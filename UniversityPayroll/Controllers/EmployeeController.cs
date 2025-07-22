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
            LeaveTypeRepository leaveTypeRepo)
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
        }

        private int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name");

            var taxSlabs = await _taxRepo.GetAllAsync();
            ViewBag.TaxSlabs = new SelectList(taxSlabs, "Id", "FinancialYear");

            return View();
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create(Employee model, string email, string password)
        {

            var user = new ApplicationUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Errors.First().Description);
            }
            else
            {
                model.IdentityUserId = user.Id.ToString();
                await _employeeRepo.CreateAsync(model);
                await EnsureSalaryStructureExists(model.Designation);
                await EnsureLeaveEntitlementExists(model.Designation);
                return RedirectToAction(nameof(Index));
            }


            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", model.Designation);

            var taxSlabs = await _taxRepo.GetAllAsync();
            ViewBag.TaxSlabs = new SelectList(taxSlabs, "Id", "FinancialYear", model.TaxSlabId);

            return View(model);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var emp = await _employeeRepo.GetByIdAsync(id);
            if (emp == null) return NotFound();

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", emp.Designation);

            var taxSlabs = await _taxRepo.GetAllAsync();
            ViewBag.TaxSlabs = new SelectList(taxSlabs, "Id", "FinancialYear", emp.TaxSlabId);

            return View(emp);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Employee model)
        {
            if (ModelState.IsValid)
            {
                await _employeeRepo.UpdateAsync(model);
                await EnsureSalaryStructureExists(model.Designation);
                await EnsureLeaveEntitlementExists(model.Designation);
                return RedirectToAction(nameof(Index));
            }

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", model.Designation);

            var taxSlabs = await _taxRepo.GetAllAsync();
            ViewBag.TaxSlabs = new SelectList(taxSlabs, "Id", "FinancialYear", model.TaxSlabId);

            return View(model);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        public async Task<IActionResult> Index()
        {
            var list = await _employeeRepo.GetAllAsync();
            return View(list);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var emp = await _employeeRepo.GetByIdAsync(id);
            if (emp != null && !string.IsNullOrEmpty(emp.IdentityUserId))
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
            if (user == null)
                return RedirectToAction("Login", "Account");

            var emp = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            if (emp == null)
                return View(new EmployeeProfileViewModel());

            var structure = await EnsureSalaryStructureExists(emp.Designation);
            var taxSlab = !string.IsNullOrEmpty(emp.TaxSlabId) ? await _taxRepo.GetByIdAsync(emp.TaxSlabId) : null;
            var year = DateTime.UtcNow.Year;

            var ent = await EnsureLeaveEntitlementExists(emp.Designation);
            var balance = await _leaveBalanceRepo.GetByEmployeeYearAsync(emp.Id, year);

            if (ent != null && ent.Entitlements != null)
            {
                var entitlements = ent.Entitlements;
                if (balance == null)
                {
                    var used = entitlements.ToDictionary(x => x.Key, x => 0);
                    balance = new LeaveBalance
                    {
                        EmployeeId = emp.Id,
                        Year = year,
                        Entitlements = new Dictionary<string, int>(entitlements),
                        Used = used,
                        Balance = new Dictionary<string, int>(entitlements),
                        LastAccrualDate = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    };
                    await _leaveBalanceRepo.CreateAsync(balance);
                }
                else
                {
                    bool updated = false;
                    foreach (var kv in entitlements)
                    {
                        if (!balance.Entitlements.ContainsKey(kv.Key))
                        {
                            balance.Entitlements[kv.Key] = kv.Value;
                            balance.Used[kv.Key] = 0;
                            balance.Balance[kv.Key] = kv.Value;
                            updated = true;
                        }
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
                    {
                        await _leaveBalanceRepo.UpdateAsync(balance);
                    }
                }
            }

            var slips = await _salarySlipRepo.GetByEmployeeAsync(emp.Id);
            var allLeaves = await _leaveRepo.GetByEmployeeAsync(emp.Id);
            var unpaidLeaves = allLeaves.Where(l => l.LeaveType == "Unpaid" && l.StartDate.Year == year).ToList();

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
                AdminComments = leave.Comment
            }).OrderByDescending(l => l.StartDate).ToList();

            var leaveTypes = await _leaveTypeRepo.GetAllAsync();

            ViewData["UnpaidLeaves"] = unpaidLeaves;

            return View(new EmployeeProfileViewModel
            {
                Employee = emp,
                SalaryStructure = structure,
                TaxSlab = taxSlab,
                LeaveBalance = balance,
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
            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction("Profile");
            }

            model.EmployeeId = employee.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid data submitted. Please check your inputs.";
                return RedirectToAction("Profile");
            }

            int workingDays = CalculateWorkingDays(model.StartDate, model.EndDate);
            decimal requestedDays = model.IsHalfDay ? 0.5m : workingDays;
            model.TotalDays = requestedDays;

            var balance = await _leaveBalanceRepo.GetByEmployeeYearAsync(employee.Id, model.StartDate.Year);
            decimal availableBalance = balance?.Balance?.GetValueOrDefault(model.LeaveType) ?? 0;

            if (requestedDays > availableBalance)
            {
                decimal paidDays = availableBalance;
                decimal unpaidDays = requestedDays - availableBalance;

                if (paidDays > 0)
                {
                    DateTime paidEndDate = model.StartDate;
                    if (model.IsHalfDay)
                    {
                        paidEndDate = model.StartDate;
                    }
                    else
                    {
                        int daysToAdd = 0;
                        decimal daysLeft = paidDays;
                        while (daysLeft > 0)
                        {
                            if (paidEndDate.AddDays(daysToAdd).DayOfWeek != DayOfWeek.Sunday)
                            {
                                daysLeft--;
                            }
                            if (daysLeft > 0) daysToAdd++;
                        }
                        paidEndDate = model.StartDate.AddDays(daysToAdd);
                    }

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
                }

                if (unpaidDays > 0)
                {
                    DateTime unpaidStartDate = model.StartDate;
                    if (paidDays > 0 && !model.IsHalfDay)
                    {
                        int daysToAdd = 1;
                        decimal daysLeft = paidDays;
                        while (daysLeft > 0)
                        {
                            if (model.StartDate.AddDays(daysToAdd - 1).DayOfWeek != DayOfWeek.Sunday)
                            {
                                daysLeft--;
                            }
                            if (daysLeft > 0) daysToAdd++;
                        }
                        unpaidStartDate = model.StartDate.AddDays(daysToAdd - 1);
                    }

                    var unpaidLeave = new LeaveApplication
                    {
                        EmployeeId = employee.Id,
                        LeaveType = "Unpaid",
                        StartDate = unpaidStartDate,
                        EndDate = model.EndDate,
                        TotalDays = unpaidDays,
                        IsHalfDay = model.IsHalfDay && paidDays == 0,
                        Reason = $"Exceeded balance for {model.LeaveType}. Original reason: {model.Reason}",
                        Status = "Pending",
                        AppliedOn = DateTime.UtcNow
                    };
                    await _leaveRepo.CreateAsync(unpaidLeave);
                }

                TempData["Success"] = $"Leave application submitted. {paidDays} days as {model.LeaveType} and {unpaidDays} days as unpaid leave.";
            }
            else
            {
                await _leaveRepo.CreateAsync(model);
                TempData["Success"] = "Leave application submitted successfully.";
            }

            return RedirectToAction(nameof(Profile));
        }

        private async Task<SalaryStructure> EnsureSalaryStructureExists(string designation)
        {
            var existing = await _salaryStructRepo.GetByDesignationAsync(designation);

            return existing;
        }

        private async Task<LeaveEntitlement> EnsureLeaveEntitlementExists(string designation)
        {
            var existing = await _entitlementRepo.GetByDesignationAsync(designation);

            return existing;
        }
    }
}