using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniversityPayroll.ViewModels;

namespace UniversityPayroll.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly LeaveRepository _leaveRepo;
        private readonly LeaveBalanceRepository _balanceRepo;
        private readonly EmployeeRepository _employeeRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LeaveTypeRepository _leaveTypeRepo;

        public LeaveController(
            LeaveRepository leaveRepo,
            LeaveBalanceRepository balanceRepo,
            EmployeeRepository employeeRepo,
            UserManager<ApplicationUser> userManager,
            LeaveTypeRepository leaveTypeRepo)
        {
            _leaveRepo = leaveRepo;
            _balanceRepo = balanceRepo;
            _employeeRepo = employeeRepo;
            _userManager = userManager;
            _leaveTypeRepo = leaveTypeRepo;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());

            if (User.IsInRole("Admin"))
            {
                var allLeaves = await _leaveRepo.GetAllAsync();
                var allEmployees = await _employeeRepo.GetAllAsync();
                var employeeDict = allEmployees.ToDictionary(e => e.Id);

                var adminViewModel = allLeaves.Select(leave => new LeaveApplicationViewModel
                {
                    LeaveId = leave.Id,
                    EmployeeName = employeeDict.GetValueOrDefault(leave.EmployeeId)?.Name ?? "Unknown",
                    EmployeeCode = employeeDict.GetValueOrDefault(leave.EmployeeId)?.EmployeeCode ?? "N/A",
                    LeaveType = leave.LeaveType,
                    StartDate = leave.StartDate,
                    EndDate = leave.EndDate,
                    TotalDays = leave.TotalDays,
                    Reason = leave.Reason,
                    Status = leave.Status,
                    AdminComments = leave.Comment
                }).ToList();
                return View(adminViewModel);
            }

            var employeeLeaves = await _leaveRepo.GetByEmployeeAsync(employee.Id);
            var employeeViewModel = employeeLeaves.Select(leave => new LeaveApplicationViewModel
            {
                LeaveId = leave.Id,
                EmployeeName = employee.Name,
                EmployeeCode = employee.EmployeeCode,
                LeaveType = leave.LeaveType,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                TotalDays = leave.TotalDays,
                Reason = leave.Reason,
                Status = leave.Status,
                AdminComments = leave.Comment
            }).ToList();
            return View(employeeViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var leaveTypes = await _leaveTypeRepo.GetAllAsync();
            ViewBag.LeaveTypes = new SelectList(leaveTypes, "Name", "Name");
            return View(new LeaveApplication());
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveApplication model)
        {
            if (!ModelState.IsValid)
            {
                var leaveTypes = await _leaveTypeRepo.GetAllAsync();
                ViewBag.LeaveTypes = new SelectList(leaveTypes, "Name", "Name");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            var balance = await _balanceRepo.GetByEmployeeYearAsync(employee.Id, model.StartDate.Year);

            int availableBalance = balance?.Balance?.GetValueOrDefault(model.LeaveType) ?? 0;
            int requestedDays = (model.EndDate - model.StartDate).Days + 1;
            model.TotalDays = requestedDays;
            model.EmployeeId = employee.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.UtcNow;

            if (requestedDays > availableBalance)
            {
                int paidDays = availableBalance;
                int unpaidDays = requestedDays - availableBalance;

                if (paidDays > 0)
                {
                    var paidLeave = new LeaveApplication
                    {
                        EmployeeId = employee.Id,
                        LeaveType = model.LeaveType,
                        StartDate = model.StartDate,
                        EndDate = model.StartDate.AddDays(paidDays - 1),
                        TotalDays = paidDays,
                        Reason = model.Reason,
                        Status = "Pending",
                        AppliedOn = DateTime.UtcNow
                    };
                    await _leaveRepo.CreateAsync(paidLeave);
                }

                var unpaidLeave = new LeaveApplication
                {
                    EmployeeId = employee.Id,
                    LeaveType = "Unpaid",
                    StartDate = model.StartDate.AddDays(paidDays),
                    EndDate = model.StartDate.AddDays(paidDays + unpaidDays - 1),
                    TotalDays = unpaidDays,
                    Reason = $"Exceeded balance for {model.LeaveType}.",
                    Status = "Pending",
                    AppliedOn = DateTime.UtcNow
                };
                await _leaveRepo.CreateAsync(unpaidLeave);
            }
            else
            {
                await _leaveRepo.CreateAsync(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Approve(string id, string comment)
        {
            var leave = await _leaveRepo.GetByIdAsync(id);
            if (leave == null) return NotFound();

            var adminUser = await _userManager.GetUserAsync(User);
            leave.Status = "Approved";
            leave.Comment = comment;
            leave.DecidedBy = adminUser.UserName;
            leave.DecidedOn = DateTime.UtcNow;
            await _leaveRepo.UpdateAsync(leave);

            if (leave.LeaveType != "Unpaid")
            {
                var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId, leave.StartDate.Year);
                if (balance != null && balance.Balance.ContainsKey(leave.LeaveType))
                {
                    balance.Balance[leave.LeaveType] -= leave.TotalDays;
                    balance.Used[leave.LeaveType] += leave.TotalDays;
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
            if (leave == null) return NotFound();

            var adminUser = await _userManager.GetUserAsync(User);
            leave.Status = "Rejected";
            leave.Comment = comment;
            leave.DecidedBy = adminUser.UserName;
            leave.DecidedOn = DateTime.UtcNow;
            await _leaveRepo.UpdateAsync(leave);
            return RedirectToAction(nameof(Index));
        }
    }
}