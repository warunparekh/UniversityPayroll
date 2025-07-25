using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    private readonly NotificationRepository _notificationRepo;
    public AdminController(
        EmployeeRepository employeeRepo,
        SalaryStructureRepository structureRepo,
        LeaveRepository leaveRepo,
        SalarySlipRepository slipRepo,
        TaxSlabRepository taxSlabRepo,
        IWebHostEnvironment env,
        LeaveBalanceRepository balanceRepo,
        UserManager<ApplicationUser> userManager,
        NotificationRepository notificationRepo)
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
        _notificationRepo = notificationRepo;
    }

    #region Helpers

    private static int GetWorkingDaysInMonth(int year, int month) =>
        Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                  .Select(day => new DateTime(year, month, day))
                  .Count(d => d.DayOfWeek != DayOfWeek.Sunday);

    private decimal CalculateUnpaidLeaveDeduction(decimal baseSalary, List<LeaveApplication> unpaidLeaves, int year, int month)
    {
        decimal perDaySalary = baseSalary / GetWorkingDaysInMonth(year, month);
        DateTime monthStart = new(year, month, 1);
        DateTime monthEnd = new(year, month, DateTime.DaysInMonth(year, month));

        decimal total = 0;

        foreach (var leave in unpaidLeaves.Where(l => l.Status == "Approved"))
        {
            DateTime start = leave.StartDate > monthStart ? leave.StartDate : monthStart;
            DateTime end = leave.EndDate < monthEnd ? leave.EndDate : monthEnd;
            if (end < start) continue;

            if (leave.IsHalfDay)
            {
                if (start.DayOfWeek != DayOfWeek.Sunday)
                    total += 0.5m * perDaySalary;
                continue;
            }

            for (var d = start; d <= end; d = d.AddDays(1))
                if (d.DayOfWeek != DayOfWeek.Sunday)
                    total += perDaySalary;
        }

        return Math.Round(total, 2);
    }

    private async Task AdjustLeaveBalanceAsync(LeaveApplication leave, string previousStatus, bool approve)
    {
        if (leave.LeaveType == "Unpaid") return;

        var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId ?? string.Empty, leave.StartDate.Year);
        if (balance == null || !balance.Balance.ContainsKey(leave.LeaveType)) return;

        int days = (int)Math.Ceiling(leave.TotalDays);

        if (previousStatus == "Approved")
        {
            balance.Balance[leave.LeaveType] += days;
            var usedPrev = balance.Used.TryGetValue(leave.LeaveType, out var u) ? u : 0;
            balance.Used[leave.LeaveType] = Math.Max(0, usedPrev - days);
        }

        if (approve)
        {
            balance.Balance[leave.LeaveType] = Math.Max(0, balance.Balance[leave.LeaveType] - days);
            var usedPrev = balance.Used.TryGetValue(leave.LeaveType, out var u) ? u : 0;
            balance.Used[leave.LeaveType] = usedPrev + days;
        }

        balance.UpdatedOn = DateTime.UtcNow;
        await _balanceRepo.UpdateAsync(balance);
    }

    #endregion

    public async Task<IActionResult> Index()
    {
        var allLeaves = await _leaveRepo.GetAllAsync();
        var employees = (await _employeeRepo.GetAllAsync()).ToDictionary(e => e.Id);

        var model = allLeaves
            .Select(leave =>
            {
                var emp = employees.TryGetValue(leave.EmployeeId ?? string.Empty, out var e) ? e : null;
                return new LeaveApplicationViewModel
                {
                    LeaveId = leave.Id ?? string.Empty,
                    EmployeeName = emp?.Name ?? "Unknown",
                    EmployeeCode = emp?.EmployeeCode ?? "N/A",
                    LeaveType = leave.LeaveType,
                    StartDate = leave.StartDate,
                    EndDate = leave.EndDate,
                    TotalDays = leave.TotalDays,
                    IsHalfDay = leave.IsHalfDay,
                    Reason = leave.Reason,
                    Status = leave.Status,
                    AdminComments = leave.Comment
                };
            })
            .OrderByDescending(l => l.StartDate)
            .ToList();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveLeave(string id, string comment)
    {
        var leave = await _leaveRepo.GetByIdAsync(id);
        if (leave == null || leave.StartDate <= DateTime.UtcNow.Date)
            return RedirectToAction(nameof(Index));

        var adminUser = await _userManager.GetUserAsync(User);
        var previousStatus = leave.Status;

        leave.Status = "Approved";
        leave.Comment = string.IsNullOrWhiteSpace(comment) ? "Approved by admin" : comment;
        leave.DecidedBy = adminUser?.UserName ?? "Admin";
        leave.DecidedOn = DateTime.UtcNow;

        await AdjustLeaveBalanceAsync(leave, previousStatus, approve: true);
        await _leaveRepo.UpdateAsync(leave);

        var employee = await _employeeRepo.GetByIdAsync(leave.EmployeeId);
        if (employee != null && !string.IsNullOrEmpty(employee.IdentityUserId))
        {
            var notification = new Notification
            {
                UserId = employee.IdentityUserId,
                Message = $"Your leave request for {leave.StartDate:dd/MM/yy} has been approved.",
                Url = "/Employee/Profile"
            };
            await _notificationRepo.CreateAsync(notification);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectLeave(string id, string comment)
    {
        var leave = await _leaveRepo.GetByIdAsync(id);
        if (leave == null || leave.StartDate <= DateTime.UtcNow.Date || string.IsNullOrWhiteSpace(comment))
            return RedirectToAction(nameof(Index));

        var adminUser = await _userManager.GetUserAsync(User);
        var previousStatus = leave.Status;

        leave.Status = "Rejected";
        leave.Comment = comment;
        leave.DecidedBy = adminUser?.UserName ?? "Admin";
        leave.DecidedOn = DateTime.UtcNow;

        await AdjustLeaveBalanceAsync(leave, previousStatus, approve: false);
        await _leaveRepo.UpdateAsync(leave);

        var employee = await _employeeRepo.GetByIdAsync(leave.EmployeeId);
        if (employee != null && !string.IsNullOrEmpty(employee.IdentityUserId))
        {
            var notification = new Notification
            {
                UserId = employee.IdentityUserId,
                Message = $"Your leave request for {leave.StartDate:dd/MM/yy} has been rejected. Reason: {comment}",
                Url = "/Employee/Profile"
            };
            await _notificationRepo.CreateAsync(notification);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLeave(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
            await _leaveRepo.DeleteAsync(id);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RunPayroll(int year, int month)
    {
        var alreadyRun = (await _slipRepo.GetAllAsync()).Any(s => s.Year == year && s.Month == month);
        if (alreadyRun) return RedirectToAction(nameof(Index));

        var employees = (await _employeeRepo.GetAllAsync()).Where(e => e.Status == "Active");

        foreach (var emp in employees)
        {
            var structure = await _structureRepo.GetByDesignationAsync(emp.Designation);
            if (structure == null) continue;

            var leaves = await _leaveRepo.GetByEmployeeAsync(emp.Id);
            var unpaidLeaves = leaves.Where(l => l.LeaveType == "Unpaid").ToList();

            decimal unpaidDeduction = CalculateUnpaidLeaveDeduction(emp.BaseSalary, unpaidLeaves, year, month);

            decimal da = emp.BaseSalary * (decimal)(structure.Allowances?.DaPercent ?? 0) / 100m;
            decimal hra = emp.BaseSalary * (decimal)(structure.Allowances?.HraPercent ?? 0) / 100m;
            decimal otherAllowances = structure.Allowances?.OtherAllowances?.Sum(o => emp.BaseSalary * (decimal)o.Percent / 100m) ?? 0m;

            decimal gross = emp.BaseSalary + da + hra + otherAllowances;

            decimal pfEmp = emp.BaseSalary * (decimal)(structure.Pf?.EmployeePercent ?? 0) / 100m;
            decimal pfEmpr = emp.BaseSalary * (decimal)(structure.Pf?.EmployerPercent ?? 0) / 100m;
            decimal edli = emp.BaseSalary * (decimal)(structure.Pf?.EdliPercent ?? 0) / 100m;

            decimal tax = 0;
            if (!string.IsNullOrEmpty(emp.TaxSlabId))
            {
                var taxSlab = await _taxSlabRepo.GetByIdAsync(emp.TaxSlabId);
                if (taxSlab?.Slabs?.Count > 0)
                {
                    decimal annualGross = gross * 12;
                    decimal annualTax = 0;

                    foreach (var slab in taxSlab.Slabs.OrderBy(s => s.From))
                    {
                        if (annualGross <= slab.From) break;
                        decimal taxable = Math.Min(annualGross, slab.To ?? decimal.MaxValue) - slab.From;
                        annualTax += taxable * (decimal)slab.Rate / 100m;
                    }

                    if (taxSlab.CessPercent > 0)
                        annualTax += annualTax * (decimal)taxSlab.CessPercent / 100m;

                    tax = Math.Round(annualTax / 12, 2);
                }
            }

            decimal totalDeductions = pfEmp + tax + unpaidDeduction;
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
                OtherDeductions = 0,
                UnpaidLeaveDeduction = unpaidDeduction,
                TotalDeductions = totalDeductions,
                NetPay = netPay,
                GeneratedOn = DateTime.UtcNow
            };

            slip.PdfUrl = await GenerateSalarySlipPdf(emp, slip, year, month);

            await _slipRepo.CreateAsync(slip);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> GenerateSalarySlipPdf(Employee emp, SalarySlip slip, int year, int month)
    {
        var fileName = $"SalarySlip_{emp.EmployeeCode}_{year}_{month}.pdf";
        var folder = Path.Combine(_env.WebRootPath, "salaryslips");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Header().AlignCenter()
                        .Text($"Salary Slip - {emp.Name}")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text($"Period: {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month)} {year}");
                        col.Item().Text($"Employee Code: {emp.EmployeeCode}");
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(10).Text("Earnings").SemiBold().FontSize(14);
                        col.Item().Text(t =>
                        {
                            t.Span("Basic: ").SemiBold();
                            t.Span($"{slip.Basic:N2}");
                        });
                        col.Item().Text($"DA: {slip.Da:N2}");
                        col.Item().Text($"HRA: {slip.Hra:N2}");
                        col.Item().Text($"Other Allowances: {slip.OtherAllowances:N2}");
                        col.Item().Text($"Gross Earnings: {slip.GrossEarnings:N2}").Bold();

                        col.Item().PaddingTop(10).Text("Deductions").SemiBold().FontSize(14);
                        col.Item().Text($"PF Employee: {slip.PfEmployee:N2}");
                        col.Item().Text($"Tax: {slip.Tax:N2}");
                        if (slip.UnpaidLeaveDeduction > 0)
                            col.Item().Text($"Unpaid Leave Deduction: {slip.UnpaidLeaveDeduction:N2}")
                                     .FontColor(Colors.Red.Medium);
                        col.Item().Text($"Total Deductions: {slip.TotalDeductions:N2}").Bold();

                        col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().AlignRight().Text($"Net Pay: {slip.NetPay:N2}").Bold().FontSize(16);
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
        var path = Path.Combine(_env.WebRootPath, "salaryslips", file);
        return File(System.IO.File.ReadAllBytes(path), "application/pdf", file);
    }
}
