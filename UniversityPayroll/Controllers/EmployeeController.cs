using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using UniversityPayroll.ViewModels;
using System.Collections.Generic;

namespace UniversityPayroll.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly EmployeeRepository _employeeRepo;
        private readonly SalaryStructureRepository _salaryStructRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaxSlabRepository _taxRepo;
        private readonly LeaveBalanceRepository _leaveBalanceRepo;
        private readonly SalarySlipRepository _salarySlipRepo;
        private readonly LeaveRepository _leaveRepo;
        private readonly LeaveEntitlementRepository _entitlementRepo;
        private readonly DesignationRepository _designationRepo;
        private readonly LeaveTypeRepository _leaveTypeRepo;
        private readonly NotificationRepository _notificationRepo;

        public EmployeeController(
            EmployeeRepository employeeRepo,
            SalaryStructureRepository salaryStructRepo,
            UserManager<ApplicationUser> userManager,
            TaxSlabRepository taxRepo,
            LeaveBalanceRepository leaveBalanceRepo,
            SalarySlipRepository salarySlipRepo,
            LeaveRepository leaveRepo,
            LeaveEntitlementRepository entitlementRepo,
            DesignationRepository designationRepo,
            LeaveTypeRepository leaveTypeRepo,
            NotificationRepository notificationRepo)
        {
            _employeeRepo = employeeRepo;
            _salaryStructRepo = salaryStructRepo;
            _userManager = userManager;
            _taxRepo = taxRepo;
            _leaveBalanceRepo = leaveBalanceRepo;
            _salarySlipRepo = salarySlipRepo;
            _leaveRepo = leaveRepo;
            _entitlementRepo = entitlementRepo;
            _designationRepo = designationRepo;
            _leaveTypeRepo = leaveTypeRepo;
            _notificationRepo = notificationRepo;
        }

        #region Optimized Helper Methods

        private static int CalculateWorkingDays(DateTime startDate, DateTime endDate) =>
            Enumerable.Range(0, (endDate - startDate).Days + 1)
                      .Select(offset => startDate.AddDays(offset))
                      .Count(date => date.DayOfWeek != DayOfWeek.Sunday);

        private static IEnumerable<DateTime> GetDateRange(DateTime startDate, DateTime endDate) =>
            Enumerable.Range(0, (endDate - startDate).Days + 1)
                      .Select(offset => startDate.AddDays(offset));

        private async Task PopulateViewBags(string? selectedDesignation = null, string? selectedTaxSlab = null)
        {
            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", selectedDesignation);

            var taxSlabs = await _taxRepo.GetAllAsync();
            ViewBag.TaxSlabs = new SelectList(taxSlabs, "Id", "FinancialYear", selectedTaxSlab);
        }

        private async Task<bool> CreateUserForEmployee(Employee model, string email, string password)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                model.IdentityUserId = user.Id.ToString();
                return true;
            }
            
            ModelState.AddModelError("", result.Errors.First().Description);
            return false;
        }

        private async Task SendLeaveApplicationNotificationToAdmin(Employee employee, LeaveApplication leave)
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            
            foreach (var admin in adminUsers)
            {
                var message = $"New leave application from {employee.Name} ({employee.EmployeeCode}) " +
                            $"for {leave.LeaveType} from {leave.StartDate:dd/MM/yyyy} to {leave.EndDate:dd/MM/yyyy} " +
                            $"({leave.TotalDays} day{(leave.TotalDays != 1 ? "s" : "")})";

                await _notificationRepo.CreateAsync(new Notification
                {
                    UserId = admin.Id.ToString(),
                    Message = message,
                    Url = "/Admin/Index",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task<LeaveBalance> EnsureLeaveBalance(Employee emp, int year)
        {
            var ent = await _entitlementRepo.GetByDesignationAsync(emp.Designation);
            var balance = await _leaveBalanceRepo.GetByEmployeeYearAsync(emp.Id, year);

            if (ent?.Entitlements == null) return balance ?? new LeaveBalance();

            if (balance == null)
            {
                balance = new LeaveBalance
                {
                    EmployeeId = emp.Id,
                    Year = year,
                    Entitlements = new Dictionary<string, int>(ent.Entitlements),
                    Used = ent.Entitlements.ToDictionary(x => x.Key, x => 0),
                    Balance = new Dictionary<string, int>(ent.Entitlements),
                    LastAccrualDate = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                await _leaveBalanceRepo.CreateAsync(balance);
            }
            else
            {
                await SyncLeaveBalance(balance, ent.Entitlements);
            }

            return balance;
        }

        private async Task SyncLeaveBalance(LeaveBalance balance, Dictionary<string, int> entitlements)
        {
            bool updated = false;

            foreach (var kv in entitlements.Where(kv => !balance.Entitlements.ContainsKey(kv.Key)))
            {
                balance.Entitlements[kv.Key] = kv.Value;
                balance.Used[kv.Key] = 0;
                balance.Balance[kv.Key] = kv.Value;
                updated = true;
            }

            var toRemove = balance.Entitlements.Keys.Except(entitlements.Keys).ToList();
            foreach (var key in toRemove)
            {
                balance.Entitlements.Remove(key);
                balance.Used.Remove(key);
                balance.Balance.Remove(key);
                updated = true;
            }

            if (updated)
                await _leaveBalanceRepo.UpdateAsync(balance);
        }

        private async Task<(bool success, DateTime paidEndDate)> ProcessPaidLeave(Employee employee, LeaveApplication model, decimal paidDays)
        {
            if (paidDays <= 0) return (false, model.StartDate);

            DateTime paidEndDate = model.IsHalfDay ? model.StartDate : CalculateEndDate(model.StartDate, paidDays);

            var paidLeave = new LeaveApplication
            {
                EmployeeId = employee.Id,
                LeaveType = model.LeaveType,
                StartDate = model.StartDate,
                EndDate = paidEndDate,
                TotalDays = paidDays,
                IsHalfDay = model.IsHalfDay && paidDays == 0.5m,
                Reason = model.Reason,
                Status = "Pending",
                AppliedOn = DateTime.UtcNow
            };

            await _leaveRepo.CreateAsync(paidLeave);
            await SendLeaveApplicationNotificationToAdmin(employee, paidLeave);
            return (true, paidEndDate);
        }

        private DateTime CalculateEndDate(DateTime startDate, decimal totalDays)
        {
            int daysToAdd = 0;
            decimal daysLeft = totalDays;
            
            while (daysLeft > 0)
            {
                if (startDate.AddDays(daysToAdd).DayOfWeek != DayOfWeek.Sunday)
                    daysLeft--;
                if (daysLeft > 0) daysToAdd++;
            }
            
            return startDate.AddDays(daysToAdd - 1);
        }

        #endregion

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateViewBags();
            return View();
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create(Employee model, string email, string password)
        {
            if (!await CreateUserForEmployee(model, email, password))
            {
                await PopulateViewBags(model.Designation, model.TaxSlabId);
                return View(model);
            }

            await _employeeRepo.CreateAsync(model);
            await _salaryStructRepo.GetByDesignationAsync(model.Designation);
            await _entitlementRepo.GetByDesignationAsync(model.Designation);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var emp = await _employeeRepo.GetByIdAsync(id);
            if (emp == null) return NotFound();

            await PopulateViewBags(emp.Designation, emp.TaxSlabId);
            return View(emp);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Employee model)
        {
            await _employeeRepo.UpdateAsync(model);
            await _salaryStructRepo.GetByDesignationAsync(model.Designation);
            await _entitlementRepo.GetByDesignationAsync(model.Designation);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        public async Task<IActionResult> Index() => View(await _employeeRepo.GetAllAsync());

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var emp = await _employeeRepo.GetByIdAsync(id);
            if (emp?.IdentityUserId != null)
            {
                var user = await _userManager.FindByIdAsync(emp.IdentityUserId);
                if (user != null)
                    await _userManager.DeleteAsync(user);
            }
            await _employeeRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var emp = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            if (emp == null) return View(new EmployeeProfileViewModel());

            var year = DateTime.UtcNow.Year;
            var structure = await _salaryStructRepo.GetByDesignationAsync(emp.Designation);
            var taxSlab = !string.IsNullOrEmpty(emp.TaxSlabId) ? await _taxRepo.GetByIdAsync(emp.TaxSlabId) : null;
            var balance = await EnsureLeaveBalance(emp, year);
            var slips = await _salarySlipRepo.GetByEmployeeAsync(emp.Id);
            var allLeaves = await _leaveRepo.GetByEmployeeAsync(emp.Id);
            var leaveTypes = await _leaveTypeRepo.GetAllAsync();

            var leaveApplications = allLeaves.Select(leave => new LeaveApplicationViewModel
            {
                LeaveId = leave.Id ?? string.Empty,
                EmployeeName = emp.Name,
                EmployeeCode = emp.EmployeeCode,
                LeaveType = leave.LeaveType,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                TotalDays = leave.TotalDays,
                IsHalfDay = leave.IsHalfDay,
                Reason = leave.Reason,
                Status = leave.Status,
                AdminComments = leave.Comment ?? string.Empty
            }).OrderByDescending(l => l.StartDate).ToList();

            var unpaidLeaves = allLeaves.Where(l => l.LeaveType == "Unpaid" && l.StartDate.Year == year).ToList();
            ViewData["UnpaidLeaves"] = unpaidLeaves;

            var blockedDates = allLeaves
                .Where(l => l.Status != "Rejected")
                .SelectMany(l => GetDateRange(l.StartDate, l.EndDate))
                .Select(d => d.ToString("yyyy-MM-dd"))
                .ToList();
            ViewData["BlockedDates"] = blockedDates;

            return View(new EmployeeProfileViewModel
            {
                Employee = emp,
                SalaryStructure = structure ?? new SalaryStructure(),
                TaxSlab = taxSlab,
                LeaveBalance = balance!,
                SalarySlips = slips,
                LeaveApplications = leaveApplications,
                LeaveTypes = leaveTypes
            });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ApplyLeave(LeaveApplication model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            if (employee == null) return RedirectToAction(nameof(Profile));

            bool hasOverlap = await _leaveRepo.HasOverlappingLeaveAsync(employee.Id, model.StartDate, model.EndDate);
            if (hasOverlap)
            {
                return RedirectToAction(nameof(Profile));
            }

            model.EmployeeId = employee.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.UtcNow;

            int workingDays = CalculateWorkingDays(model.StartDate, model.EndDate);
            decimal requestedDays = model.IsHalfDay ? 0.5m : workingDays;
            model.TotalDays = requestedDays;

            var balance = await _leaveBalanceRepo.GetByEmployeeYearAsync(employee.Id, model.StartDate.Year);
            decimal availableBalance = balance?.Balance?.GetValueOrDefault(model.LeaveType) ?? 0;

            if (requestedDays > availableBalance)
            {
                await ProcessExceedingLeave(employee, model, availableBalance, requestedDays);
            }
            else
            {
                await _leaveRepo.CreateAsync(model);
                await SendLeaveApplicationNotificationToAdmin(employee, model);
            }

            return RedirectToAction(nameof(Profile));
        }

        private async Task ProcessExceedingLeave(Employee employee, LeaveApplication model, decimal availableBalance, decimal requestedDays)
        {
            decimal unpaidDays = requestedDays - availableBalance;

            if (availableBalance > 0)
            {
                var (success, paidEndDate) = await ProcessPaidLeave(employee, model, availableBalance);
                
                if (success && unpaidDays > 0)
                {
                    DateTime unpaidStartDate = model.IsHalfDay && availableBalance == 0 
                        ? model.StartDate 
                        : paidEndDate.AddDays(1);

                    var unpaidLeave = new LeaveApplication
                    {
                        EmployeeId = employee.Id,
                        LeaveType = "Unpaid",
                        StartDate = unpaidStartDate,
                        EndDate = model.EndDate,
                        TotalDays = unpaidDays,
                        IsHalfDay = model.IsHalfDay && availableBalance == 0,
                        Reason = $"Exceeded balance for {model.LeaveType}. Original reason: {model.Reason}",
                        Status = "Pending",
                        AppliedOn = DateTime.UtcNow
                    };
                    await _leaveRepo.CreateAsync(unpaidLeave);
                    await SendLeaveApplicationNotificationToAdmin(employee, unpaidLeave);
                }
            }
            else
            {
                var unpaidLeave = new LeaveApplication
                {
                    EmployeeId = employee.Id,
                    LeaveType = "Unpaid",
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    TotalDays = requestedDays,
                    IsHalfDay = model.IsHalfDay,
                    Reason = $"No balance available for {model.LeaveType}. Original reason: {model.Reason}",
                    Status = "Pending",
                    AppliedOn = DateTime.UtcNow
                };
                await _leaveRepo.CreateAsync(unpaidLeave);
                await SendLeaveApplicationNotificationToAdmin(employee, unpaidLeave);
            }
        }

       

        private async Task<SalaryStructure?> EnsureSalaryStructureExists(string designation)
        {
            var existing = await _salaryStructRepo.GetByDesignationAsync(designation);
            return existing;
        }

        private async Task<LeaveEntitlement?> EnsureLeaveEntitlementExists(string designation)
        {
            var existing = await _entitlementRepo.GetByDesignationAsync(designation);
            return existing;
        }
    }
}