using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
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
using System.Threading.Tasks;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using UniversityPayroll.ViewModels;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly EmployeeRepository _employeeRepo;
    private readonly SalaryStructureRepository _structureRepo;
    private readonly LeaveRepository _leaveRepo;
    private readonly SalarySlipRepository _slipRepo;
    private readonly TaxSlabRepository _taxSlabRepo;
    private readonly IWebHostEnvironment _env;
    private readonly LeaveBalanceRepository _balanceRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(
        EmployeeRepository employeeRepo,
        SalaryStructureRepository structureRepo,
        LeaveRepository leaveRepo,
        SalarySlipRepository slipRepo,
        TaxSlabRepository taxSlabRepo,
        IWebHostEnvironment env,
        LeaveBalanceRepository balanceRepo,
        UserManager<ApplicationUser> userManager)
    {
        _employeeRepo = employeeRepo;
        _structureRepo = structureRepo;
        _leaveRepo = leaveRepo;
        _slipRepo = slipRepo;
        _taxSlabRepo = taxSlabRepo;
        _env = env;
        _balanceRepo = balanceRepo;
        _userManager = userManager;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    private decimal CalculateUnpaidLeaveDeduction(decimal baseSalary, List<LeaveApplication> unpaidLeaves, int year, int month)
    {
        decimal totalDeduction = 0;
        int workingDaysInMonth = GetWorkingDaysInMonth(year, month);
        decimal perDaySalary = baseSalary / workingDaysInMonth;

        foreach (var leave in unpaidLeaves.Where(l => l.Status == "Approved"))
        {
            decimal leaveDaysInMonth = 0;

            if (leave.StartDate.Year == year && leave.StartDate.Month == month)
            {
                DateTime monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                DateTime leaveEndInMonth = leave.EndDate > monthEnd ? monthEnd : leave.EndDate;

                for (DateTime date = leave.StartDate; date <= leaveEndInMonth; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        leaveDaysInMonth += leave.IsHalfDay ? 0.5m : 1m;
                        if (leave.IsHalfDay) break;
                    }
                }
            }
            else if (leave.StartDate < new DateTime(year, month, 1) && leave.EndDate >= new DateTime(year, month, 1))
            {
                DateTime monthStart = new DateTime(year, month, 1);
                DateTime monthEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                DateTime leaveEndInMonth = leave.EndDate > monthEnd ? monthEnd : leave.EndDate;

                for (DateTime date = monthStart; date <= leaveEndInMonth; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        leaveDaysInMonth += 1m;
                    }
                }
            }

            totalDeduction += leaveDaysInMonth * perDaySalary;
        }

        return Math.Round(totalDeduction, 2);
    }

    private int GetWorkingDaysInMonth(int year, int month)
    {
        int workingDays = 0;
        int daysInMonth = DateTime.DaysInMonth(year, month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            DateTime date = new DateTime(year, month, day);
            if (date.DayOfWeek != DayOfWeek.Sunday)
            {
                workingDays++;
            }
        }

        return workingDays;
    }

    public async Task<IActionResult> Index()
    {
        var allLeaves = await _leaveRepo.GetAllAsync();
        var allEmployees = await _employeeRepo.GetAllAsync();
        var employeeDict = allEmployees.ToDictionary(e => e.Id);

        var adminViewModel = allLeaves.Select(leave => new LeaveApplicationViewModel
        {
            LeaveId = leave.Id ?? string.Empty,
            EmployeeName = employeeDict.GetValueOrDefault(leave.EmployeeId ?? string.Empty)?.Name ?? "Unknown",
            EmployeeCode = employeeDict.GetValueOrDefault(leave.EmployeeId ?? string.Empty)?.EmployeeCode ?? "N/A",
            LeaveType = leave.LeaveType,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            TotalDays = leave.TotalDays,
            IsHalfDay = leave.IsHalfDay,
            Reason = leave.Reason,
            Status = leave.Status,
            AdminComments = leave.Comment
        }).OrderByDescending(l => l.StartDate).ToList();

        return View(adminViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveLeave(string id, string comment)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "Invalid leave application ID.";
            return RedirectToAction(nameof(Index));
        }

        var leave = await _leaveRepo.GetByIdAsync(id);
        if (leave == null)
        {
            TempData["Error"] = "Leave application not found.";
            return RedirectToAction(nameof(Index));
        }

        if (leave.StartDate <= DateTime.UtcNow.Date)
        {
            TempData["Error"] = "Cannot change status after leave has started.";
            return RedirectToAction(nameof(Index));
        }

        var adminUser = await _userManager.GetUserAsync(User);
        var previousStatus = leave.Status;

        leave.Status = "Approved";
        leave.Comment = string.IsNullOrWhiteSpace(comment) ? "Approved by admin" : comment;
        leave.DecidedBy = adminUser?.UserName ?? "Admin";
        leave.DecidedOn = DateTime.UtcNow;

        if (leave.LeaveType != "Unpaid")
        {
            var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId ?? string.Empty, leave.StartDate.Year);
            if (balance != null && balance.Balance.ContainsKey(leave.LeaveType))
            {
                if (previousStatus == "Approved")
                {
                    balance.Balance[leave.LeaveType] += (int)leave.TotalDays;
                    balance.Used[leave.LeaveType] = Math.Max(0, balance.Used[leave.LeaveType] - (int)leave.TotalDays);
                }

                balance.Balance[leave.LeaveType] = Math.Max(0, balance.Balance[leave.LeaveType] - (int)Math.Ceiling(leave.TotalDays));
                balance.Used[leave.LeaveType] = (balance.Used?.GetValueOrDefault(leave.LeaveType) ?? 0) + (int)Math.Ceiling(leave.TotalDays);
                balance.UpdatedOn = DateTime.UtcNow;
                await _balanceRepo.UpdateAsync(balance);
            }
        }

        await _leaveRepo.UpdateAsync(leave);
        TempData["Success"] = $"Leave application approved successfully for {leave.TotalDays} day(s).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectLeave(string id, string comment)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Error"] = "Invalid leave application ID.";
            return RedirectToAction(nameof(Index));
        }

        var leave = await _leaveRepo.GetByIdAsync(id);
        if (leave == null)
        {
            TempData["Error"] = "Leave application not found.";
            return RedirectToAction(nameof(Index));
        }

        if (leave.StartDate <= DateTime.UtcNow.Date)
        {
            TempData["Error"] = "Cannot change status after leave has started.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["Error"] = "A comment is required when rejecting a leave application.";
            return RedirectToAction(nameof(Index));
        }

        var adminUser = await _userManager.GetUserAsync(User);
        var previousStatus = leave.Status;

        leave.Status = "Rejected";
        leave.Comment = comment;
        leave.DecidedBy = adminUser?.UserName ?? "Admin";
        leave.DecidedOn = DateTime.UtcNow;

        if (previousStatus == "Approved" && leave.LeaveType != "Unpaid")
        {
            var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId ?? string.Empty, leave.StartDate.Year);
            if (balance != null && balance.Balance.ContainsKey(leave.LeaveType))
            {
                balance.Balance[leave.LeaveType] += (int)Math.Ceiling(leave.TotalDays);
                balance.Used[leave.LeaveType] = Math.Max(0, balance.Used[leave.LeaveType] - (int)Math.Ceiling(leave.TotalDays));
                balance.UpdatedOn = DateTime.UtcNow;
                await _balanceRepo.UpdateAsync(balance);
            }
        }

        await _leaveRepo.UpdateAsync(leave);
        TempData["Success"] = "Leave application rejected successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLeave(string id)
    {
        await _leaveRepo.DeleteAsync(id);
        TempData["Success"] = "Leave application deleted successfully.";
        return RedirectToAction(nameof(Index));
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

            var leaves = await _leaveRepo.GetByEmployeeAsync(emp.Id);
            var unpaidLeaves = leaves.Where(l => l.LeaveType == "Unpaid").ToList();

            decimal unpaidDeduction = CalculateUnpaidLeaveDeduction(emp.BaseSalary, unpaidLeaves, year, month);

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
        TempData["Success"] = "Payroll generated successfully.";
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
                        column.Item().Text($"Other Allowances: {slip.OtherAllowances:N2}");
                        column.Item().Text($"Gross Earnings: {slip.GrossEarnings:N2}").Bold();

                        column.Item().PaddingTop(10).Text("Deductions").SemiBold().FontSize(14);
                        column.Item().Text($"PF Employee: {slip.PfEmployee:N2}");
                        column.Item().Text($"Tax: {slip.Tax:N2}");
                        if (slip.UnpaidLeaveDeduction > 0)
                        {
                            column.Item().Text($"Unpaid Leave Deduction: {slip.UnpaidLeaveDeduction:N2}").FontColor(Colors.Red.Medium);
                        }
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