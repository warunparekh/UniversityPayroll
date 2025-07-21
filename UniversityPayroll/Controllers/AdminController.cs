using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly EmployeeRepository _employeeRepo;
    private readonly SalaryStructureRepository _structureRepo;
    private readonly LeaveRepository _leaveRepo;
    private readonly SalarySlipRepository _slipRepo;
    private readonly TaxSlabRepository _taxSlabRepo;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        EmployeeRepository employeeRepo,
        SalaryStructureRepository structureRepo,
        LeaveRepository leaveRepo,
        SalarySlipRepository slipRepo,
        TaxSlabRepository taxSlabRepo,
        IWebHostEnvironment env)
    {
        _employeeRepo = employeeRepo;
        _structureRepo = structureRepo;
        _leaveRepo = leaveRepo;
        _slipRepo = slipRepo;
        _taxSlabRepo = taxSlabRepo;
        _env = env;

        // Required for QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Years = Enumerable.Range(DateTime.UtcNow.Year - 5, 10).ToList();
        ViewBag.Months = Enumerable.Range(1, 12).Select(i => new { Value = i, Name = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i) }).ToList();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> RunPayroll(int year, int month)
    {
        var existing = (await _slipRepo.GetAllAsync()).Any(s => s.Year == year && s.Month == month);
        if (existing)
        {
            TempData["Error"] = "Payroll already run for this period.";
            return RedirectToAction("Index");
        }

        var employees = await _employeeRepo.GetAllAsync();
        foreach (var emp in employees.Where(e => e.Status == "Active"))
        {
            var structure = await _structureRepo.GetByDesignationAsync(emp.Designation);
            if (structure == null) continue;
            int daysInMonth = DateTime.DaysInMonth(year, month);
            decimal perDaySalary = emp.BaseSalary / daysInMonth;
            var leaves = await _leaveRepo.GetByEmployeeAsync(emp.Id);
            int unpaidDays = leaves
                .Where(l => l.LeaveType == "Unpaid" && l.Status == "Approved" && l.StartDate.Year == year && l.StartDate.Month == month)
                .Sum(l => l.TotalDays);
            decimal unpaidDeduction = unpaidDays * perDaySalary;
            decimal da = emp.BaseSalary * (decimal)(structure.Allowances?.DaPercent ?? 0) / 100m;
            decimal hra = emp.BaseSalary * (decimal)(structure.Allowances?.HraPercent ?? 0) / 100m;
            decimal otherAllowances = 0;
            if (structure.Allowances?.OtherAllowances != null)
            {
                foreach (var o in structure.Allowances.OtherAllowances)
                    otherAllowances += emp.BaseSalary * (decimal)o.Percent / 100m;
            }
            decimal gross = emp.BaseSalary + da + hra + otherAllowances;
            decimal pfEmp = emp.BaseSalary * (decimal)(structure.Pf?.EmployeePercent ?? 0) / 100m;
            decimal pfEmpr = emp.BaseSalary * (decimal)(structure.Pf?.EmployerPercent ?? 0) / 100m;
            decimal edli = emp.BaseSalary * (decimal)(structure.Pf?.EdliPercent ?? 0) / 100m;

            decimal tax = 0;
            if (!string.IsNullOrEmpty(emp.TaxSlabId))
            {
                var taxSlab = await _taxSlabRepo.GetByIdAsync(emp.TaxSlabId);
                if (taxSlab != null && taxSlab.Slabs != null && taxSlab.Slabs.Count > 0)
                {
                    decimal annualGross = gross * 12;
                    decimal annualTax = 0;
                    foreach (var slab in taxSlab.Slabs.OrderBy(s => s.From))
                    {
                        decimal slabFrom = slab.From;
                        decimal slabTo = slab.To ?? decimal.MaxValue;
                        if (annualGross > slabFrom)
                        {
                            decimal taxable = Math.Min(annualGross, slabTo) - slabFrom;
                            annualTax += taxable * (decimal)slab.Rate / 100m;
                        }
                    }
                    if (taxSlab.CessPercent > 0)
                        annualTax += annualTax * (decimal)taxSlab.CessPercent / 100m;
                    tax = Math.Round(annualTax / 12, 2);
                }
            }

            decimal otherDeductions = 0;
            decimal totalDeductions = pfEmp + tax + otherDeductions + unpaidDeduction;
            decimal netPay = gross - totalDeductions;
            var slip = new SalarySlip
            {
                EmployeeId = emp.Id,
                Year = year,
                Month = month,
                StructureRefId = structure.Id,
                Basic = emp.BaseSalary,
                Da = da,
                Hra = hra,
                OtherAllowances = otherAllowances,
                GrossEarnings = gross,
                PfEmployee = pfEmp,
                PfEmployer = pfEmpr,
                Edli = edli,
                Tax = tax,
                OtherDeductions = otherDeductions,
                UnpaidLeaveDeduction = unpaidDeduction,
                TotalDeductions = totalDeductions,
                NetPay = netPay,
                GeneratedOn = DateTime.UtcNow
            };
            var pdfPath = await GenerateSalarySlipPdf(emp, slip, year, month);
            slip.PdfUrl = pdfPath;
            await _slipRepo.CreateAsync(slip);
        }
        TempData["Success"] = "Payroll generated.";
        return RedirectToAction("Index");
    }

    private async Task<string> GenerateSalarySlipPdf(Employee emp, SalarySlip slip, int year, int month)
    {
        var fileName = $"SalarySlip_{emp.EmployeeCode}_{year}_{month}.pdf";
        var folder = Path.Combine(_env.WebRootPath, "salaryslips");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        await Task.Run(() =>
        {
            QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Header().AlignCenter().Text($"Salary Slip - {emp.Name}").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);
                        var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
                        column.Item().Text($"Period: {monthName} {year}");
                        column.Item().Text($"Employee Code: {emp.EmployeeCode}");
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().PaddingTop(10).Text("Earnings").SemiBold().FontSize(14);
                        column.Item().Text(text =>
                        {
                            text.Span("Basic: ").SemiBold();
                            text.Span($"{slip.Basic:N2}");
                        });
                        column.Item().Text($"DA: {slip.Da:N2}");
                        column.Item().Text($"HRA: {slip.Hra:N2}");
                        column.Item().Text($"Gross Earnings: {slip.GrossEarnings:N2}").Bold();

                        column.Item().PaddingTop(10).Text("Deductions").SemiBold().FontSize(14);
                        column.Item().Text($"PF Employee: {slip.PfEmployee:N2}");
                        column.Item().Text($"Tax: {slip.Tax:N2}");
                        column.Item().Text($"Total Deductions: {slip.TotalDeductions:N2}").Bold();

                        column.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().AlignRight().Text($"Net Pay: {slip.NetPay:N2}").Bold().FontSize(16);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(filePath);
        });

        return $"/salaryslips/{fileName}";
    }

    [AllowAnonymous]
    public IActionResult DownloadSlip(string file)
    {
        var folder = Path.Combine(_env.WebRootPath, "salaryslips");
        var filePath = Path.Combine(folder, file);
        if (!System.IO.File.Exists(filePath))
            return NotFound();
        var bytes = System.IO.File.ReadAllBytes(filePath);
        return File(bytes, "application/pdf", file);
    }
}