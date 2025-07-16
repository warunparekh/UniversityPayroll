using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "UserOrAdmin")]
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
            var employee = await _employeeRepo.GetByUserIdAsync(user.Id);

            if (User.IsInRole("Admin"))
                return View(await _leaveRepo.GetAllAsync());

            return View(await _leaveRepo.GetByEmployeeAsync(employee.Id));
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(LeaveApplication model)
        {
            var user = await _userManager.GetUserAsync(User);
            var employee = await _employeeRepo.GetByUserIdAsync(user.Id);

            model.EmployeeId = employee.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.UtcNow;

            await _leaveRepo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Approve(string id)
        {
            var leave = await _leaveRepo.GetByIdAsync(id);
            if (leave != null)
            {
                leave.Status = "Approved";
                leave.DecidedBy = User.Identity.Name;
                leave.DecidedOn = DateTime.UtcNow;
                await _leaveRepo.UpdateAsync(leave);

                // EmployeeId is a string now
                await _balanceRepo.IncrementUsedAsync(
                    leave.EmployeeId,
                    leave.LeaveType,
                    leave.TotalDays
                );
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Reject(string id, string comment)
        {
            var leave = await _leaveRepo.GetByIdAsync(id);
            if (leave != null)
            {
                leave.Status = "Rejected";
                leave.Comment = comment;
                leave.DecidedBy = User.Identity.Name;
                leave.DecidedOn = DateTime.UtcNow;
                await _leaveRepo.UpdateAsync(leave);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
