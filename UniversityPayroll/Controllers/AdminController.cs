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
        _notificationRepo = notificationRepo;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    #region Optimized Helper Methods

    private static int GetWorkingDaysInMonth(int year, int month) =>
        Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                  .Select(day => new DateTime(year, month, day))
                  .Count(d => d.DayOfWeek != DayOfWeek.Sunday);

    private decimal CalculateUnpaidLeaveDeduction(decimal baseSalary, List<LeaveApplication> unpaidLeaves, int year, int month)
    {
        decimal perDaySalary = baseSalary / GetWorkingDaysInMonth(year, month);
        DateTime monthStart = new(year, month, 1);
        DateTime monthEnd = new(year, month, DateTime.DaysInMonth(year, month));

        return unpaidLeaves
            .Where(l => l.Status == "Approved")
            .Sum(leave => CalculateLeaveDeduction(leave, monthStart, monthEnd, perDaySalary));
    }

    private static decimal CalculateLeaveDeduction(LeaveApplication leave, DateTime monthStart, DateTime monthEnd, decimal perDaySalary)
    {
        DateTime start = leave.StartDate > monthStart ? leave.StartDate : monthStart;
        DateTime end = leave.EndDate < monthEnd ? leave.EndDate : monthEnd;
        if (end < start) return 0;

        if (leave.IsHalfDay)
            return start.DayOfWeek != DayOfWeek.Sunday ? 0.5m * perDaySalary : 0;

        return Enumerable.Range(0, (int)(end - start).TotalDays + 1)
            .Select(i => start.AddDays(i))
            .Where(d => d.DayOfWeek != DayOfWeek.Sunday)
            .Count() * perDaySalary;
    }

    private async Task AdjustLeaveBalanceAsync(LeaveApplication leave, string previousStatus, bool approve)
    {
        if (leave.LeaveType == "Unpaid") return;

        var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId ?? string.Empty, leave.StartDate.Year);
        if (balance?.Balance?.ContainsKey(leave.LeaveType) != true) return;

        decimal days = leave.TotalDays;

        if (previousStatus == "Approved")
        {
            balance.Balance[leave.LeaveType] += days;
            balance.Used[leave.LeaveType] = Math.Max(0, (balance.Used.TryGetValue(leave.LeaveType, out var u) ? u : 0) - days);
        }

        if (approve)
        {
            balance.Balance[leave.LeaveType] = Math.Max(0, balance.Balance[leave.LeaveType] - days);
            balance.Used[leave.LeaveType] = (balance.Used.TryGetValue(leave.LeaveType, out var u) ? u : 0) + days;
        }

        balance.UpdatedOn = DateTime.UtcNow;
        await _balanceRepo.UpdateAsync(balance);
    }

    private async Task<bool> ProcessLeaveDecision(string id, string comment, bool approve)
    {
        var leave = await _leaveRepo.GetByIdAsync(id);
        if (leave == null || leave.StartDate.Date.AddDays(1) < DateTime.UtcNow.Date || (!approve && string.IsNullOrWhiteSpace(comment)))
            return false;

        var adminUser = await _userManager.GetUserAsync(User);
        var previousStatus = leave.Status;

        leave.Status = approve ? "Approved" : "Rejected";
        leave.Comment = approve && string.IsNullOrWhiteSpace(comment) ? "Approved by HR" : comment;
        leave.DecidedBy = adminUser?.UserName ?? "Admin";
        leave.DecidedOn = DateTime.UtcNow;

        await AdjustLeaveBalanceAsync(leave, previousStatus, approve);
        await _leaveRepo.UpdateAsync(leave);
        await SendLeaveNotification(leave, approve, comment);
        return true;
    }

    private async Task SendLeaveNotification(LeaveApplication leave, bool approve, string comment)
    {
        if (string.IsNullOrEmpty(leave.EmployeeId)) return;
        
        var employee = await _employeeRepo.GetByIdAsync(leave.EmployeeId);
        if (employee?.IdentityUserId == null) return;

        var message = approve 
            ? $"Your leave request for {leave.StartDate:dd/MM/yy} has been approved."
            : $"Your leave request for {leave.StartDate:dd/MM/yy} has been rejected. Reason: {comment}";

        await _notificationRepo.CreateAsync(new Notification
        {
            UserId = employee.IdentityUserId,
            Message = message,
            Url = "/Employee/Profile"
        });
    }

    private async Task<decimal> CalculateTax(string taxSlabId, decimal gross)
    {
        if (string.IsNullOrEmpty(taxSlabId)) return 0;

        var taxSlab = await _taxSlabRepo.GetByIdAsync(taxSlabId);
        if (taxSlab?.Slabs == null || taxSlab.Slabs.Count == 0) return 0;

        decimal annualGross = gross * 12;
        decimal annualTax = taxSlab.Slabs
            .OrderBy(s => s.From)
            .TakeWhile(slab => annualGross > slab.From)
            .Sum(slab => {
                decimal taxable = Math.Min(annualGross, slab.To ?? decimal.MaxValue) - slab.From;
                return taxable * (decimal)slab.Rate / 100m;
            });

        if (taxSlab.CessPercent > 0)
            annualTax += annualTax * (decimal)taxSlab.CessPercent / 100m;

        return Math.Round(annualTax / 12, 2);
    }

    private async Task<SalarySlip?> CalculateSalarySlip(Employee emp, int year, int month)
    {
        var structure = await _structureRepo.GetByDesignationAsync(emp.Designation);
        if (structure == null) return null;

        var leaves = await _leaveRepo.GetByEmployeeAsync(emp.Id);
        var unpaidLeaves = leaves.Where(l => l.LeaveType == "Unpaid").ToList();
        decimal unpaidDeduction = CalculateUnpaidLeaveDeduction(emp.BaseSalary, unpaidLeaves, year, month);

        var allowances = structure.Allowances;
        decimal da = emp.BaseSalary * (decimal)(allowances?.DaPercent ?? 0) / 100m;
        decimal hra = emp.BaseSalary * (decimal)(allowances?.HraPercent ?? 0) / 100m;
        decimal otherAllowances = allowances?.OtherAllowances?.Sum(o => emp.BaseSalary * (decimal)o.Percent / 100m) ?? 0m;
        decimal gross = emp.BaseSalary + da + hra + otherAllowances;

        var pf = structure.Pf;
        decimal pfEmp = emp.BaseSalary * (decimal)(pf?.EmployeePercent ?? 0) / 100m;
        decimal pfEmpr = emp.BaseSalary * (decimal)(pf?.EmployerPercent ?? 0) / 100m;
        decimal edli = emp.BaseSalary * (decimal)(pf?.EdliPercent ?? 0) / 100m;

        decimal tax = await CalculateTax(emp.TaxSlabId ?? string.Empty, gross);
        decimal totalDeductions = pfEmp + tax + unpaidDeduction;

        return new SalarySlip
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
            NetPay = gross - totalDeductions,
            GeneratedOn = DateTime.UtcNow
        };
    }

    private async Task<string> GenerateSalarySlipPdf(Employee emp, SalarySlip slip, int year, int month)
    {
        if (emp == null || slip == null) 
            throw new ArgumentNullException("Employee and SalarySlip cannot be null");

        var fileName = $"SalarySlip_{emp.EmployeeCode}_{year:D4}_{month:D2}.pdf";
        var folder = Path.Combine(_env.WebRootPath, "salaryslips");
        
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);
            
        var filePath = Path.Combine(folder, fileName);

        await Task.Run(() => Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePdfPageLayout(page, emp);
                AddPdfPageContent(page, emp, slip, year, month);
                AddPdfPageFooter(page);
            });
        }).GeneratePdf(filePath));

        return $"/salaryslips/{fileName}";
    }

    private static void ConfigurePdfPageLayout(PageDescriptor page, Employee emp)
    {
        page.Margin(40);
        page.Header().AlignCenter()
            .Text($"Salary Slip - {emp.Name}")
            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
    }

    private static void AddPdfPageContent(PageDescriptor page, Employee emp, SalarySlip slip, int year, int month)
    {
        page.Content().Column(col =>
        {
            col.Spacing(10);
            
            AddPdfHeaderInfo(col, emp, year, month);
            AddPdfEarningsSection(col, slip);
            AddPdfDeductionsSection(col, slip);
            AddPdfNetPaySection(col, slip);
        });
    }

    private static void AddPdfHeaderInfo(ColumnDescriptor col, Employee emp, int year, int month)
    {
        var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
        col.Item().Text($"Period: {monthName} {year}");
        col.Item().Text($"Employee Code: {emp.EmployeeCode}");
        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
    }

    private static void AddPdfEarningsSection(ColumnDescriptor col, SalarySlip slip)
    {
        col.Item().PaddingTop(10).Text("Earnings").SemiBold().FontSize(14);
        col.Item().Text($"Basic: {slip.Basic:N2}").SemiBold();
        col.Item().Text($"DA: {slip.Da:N2}");
        col.Item().Text($"HRA: {slip.Hra:N2}");
        col.Item().Text($"Other Allowances: {slip.OtherAllowances:N2}");
        col.Item().Text($"Gross Earnings: {slip.GrossEarnings:N2}").Bold();
    }

    private static void AddPdfDeductionsSection(ColumnDescriptor col, SalarySlip slip)
    {
        col.Item().PaddingTop(10).Text("Deductions").SemiBold().FontSize(14);
        col.Item().Text($"PF Employee: {slip.PfEmployee:N2}");
        col.Item().Text($"Tax: {slip.Tax:N2}");
        
        if (slip.UnpaidLeaveDeduction > 0)
        {
            col.Item().Text($"Unpaid Leave Deduction: {slip.UnpaidLeaveDeduction:N2}")
                     .FontColor(Colors.Red.Medium);
        }
        
        col.Item().Text($"Total Deductions: {slip.TotalDeductions:N2}").Bold();
    }

    private static void AddPdfNetPaySection(ColumnDescriptor col, SalarySlip slip)
    {
        col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        col.Item().AlignRight().Text($"Net Pay: {slip.NetPay:N2}").Bold().FontSize(16);
    }

    private static void AddPdfPageFooter(PageDescriptor page)
    {
        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
        });
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
                    AdminComments = leave.Comment ?? string.Empty
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
        await ProcessLeaveDecision(id, comment, approve: true);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectLeave(string id, string comment)
    {
        await ProcessLeaveDecision(id, comment, approve: false);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteLeave(string id)
    {
        var leave = await _leaveRepo.GetByIdAsync(id);
        if (leave != null)
        {
            if (leave.Status == "Approved")
            {
                await AdjustLeaveBalanceAsync(leave, "Approved", false);
            }
            await _leaveRepo.DeleteAsync(id);
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> RunPayroll(int year, int month)
    {
        var alreadyRun = (await _slipRepo.GetAllAsync()).Any(s => s.Year == year && s.Month == month);
        if (alreadyRun) return RedirectToAction(nameof(Index));

        var employees = (await _employeeRepo.GetAllAsync()).Where(e => e.Status == "Active");

        foreach (var emp in employees)
        {
            var slip = await CalculateSalarySlip(emp, year, month);
            if (slip == null) continue;

            slip.PdfUrl = await GenerateSalarySlipPdf(emp, slip, year, month);
            await _slipRepo.CreateAsync(slip);
        }

        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult DownloadSlip(string file)
    {
        var path = Path.Combine(_env.WebRootPath, "salaryslips", file);
        return File(System.IO.File.ReadAllBytes(path), "application/pdf", file);
    }
}
