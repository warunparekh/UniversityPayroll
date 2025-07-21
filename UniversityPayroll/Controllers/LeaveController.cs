using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly LeaveRepository _leaveRepo;
        private readonly LeaveBalanceRepository _balanceRepo;
        private readonly EmployeeRepository _employeeRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveController(
            LeaveRepository leaveRepo,
            LeaveBalanceRepository balanceRepo,
            EmployeeRepository employeeRepo,
            UserManager<ApplicationUser> userManager)
        {
            _leaveRepo = leaveRepo;
            _balanceRepo = balanceRepo;
            _employeeRepo = employeeRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            var leaves = User.IsInRole("Admin")
                ? await _leaveRepo.GetAllAsync()
                : await _leaveRepo.GetByEmployeeAsync(employee.Id);
            return View(leaves);
        }

        

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var emp = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            var entRepo = new LeaveEntitlementRepository(new MongoDbContext(
                HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<MongoDbSettings>)) as Microsoft.Extensions.Options.IOptions<MongoDbSettings>
            ));
            var ent = await entRepo.GetByDesignationAsync(emp.Designation);
            ViewBag.LeaveTypes = ent?.Entitlements?.Keys?.ToList() ?? new List<string>();
            ViewBag.EmployeeCode = emp.EmployeeCode;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveApplication model)
        {
            var user = await _userManager.GetUserAsync(User);
            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            model.EmployeeId = employee.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.UtcNow;
            await _leaveRepo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Approve(string id)
        {
            var leave = await _leaveRepo.GetByIdAsync(id);
            if (leave != null && leave.Status != "Approved")
            {
                leave.Status = "Approved";
                leave.DecidedBy = User.Identity.Name;
                leave.DecidedOn = DateTime.UtcNow;
                await _leaveRepo.UpdateAsync(leave);

                var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId, leave.StartDate.Year);
                if (balance != null)
                {
                    if (!balance.Used.ContainsKey(leave.LeaveType))
                        balance.Used[leave.LeaveType] = 0;
                    if (!balance.Balance.ContainsKey(leave.LeaveType))
                        balance.Balance[leave.LeaveType] = balance.Entitlements[leave.LeaveType];

                    balance.Used[leave.LeaveType] += leave.TotalDays;
                    balance.Balance[leave.LeaveType] = balance.Entitlements[leave.LeaveType] - balance.Used[leave.LeaveType];
                    balance.UpdatedOn = DateTime.UtcNow;
                    await _balanceRepo.UpdateAsync(balance);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Reject(string id, string comment)
        {
            var leave = await _leaveRepo.GetByIdAsync(id);
            if (leave != null)
            {
                var wasApproved = leave.Status == "Approved";
                leave.Status = "Rejected";
                leave.Comment = comment;
                leave.DecidedBy = User.Identity.Name;
                leave.DecidedOn = DateTime.UtcNow;
                await _leaveRepo.UpdateAsync(leave);

                if (wasApproved)
                {
                    var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId, leave.StartDate.Year);
                    if (balance != null)
                    {
                        if (!balance.Used.ContainsKey(leave.LeaveType))
                            balance.Used[leave.LeaveType] = 0;
                        if (!balance.Balance.ContainsKey(leave.LeaveType))
                            balance.Balance[leave.LeaveType] = balance.Entitlements[leave.LeaveType];

                        balance.Used[leave.LeaveType] -= leave.TotalDays;
                        if (balance.Used[leave.LeaveType] < 0)
                            balance.Used[leave.LeaveType] = 0;
                        balance.Balance[leave.LeaveType] = balance.Entitlements[leave.LeaveType] - balance.Used[leave.LeaveType];
                        balance.UpdatedOn = DateTime.UtcNow;
                        await _balanceRepo.UpdateAsync(balance);
                    }
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}