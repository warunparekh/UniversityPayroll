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
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Admin"))
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
                    Reason = leave.Reason,
                    Status = leave.Status,
                    AdminComments = leave.Comment
                }).OrderByDescending(l => l.StartDate).ToList();
                return View(adminViewModel);
            }

            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found. Please contact your administrator.";
                return RedirectToAction("Index", "Home");
            }

            var employeeLeaves = await _leaveRepo.GetByEmployeeAsync(employee.Id);
            var employeeViewModel = employeeLeaves.Select(leave => new LeaveApplicationViewModel
            {
                LeaveId = leave.Id ?? string.Empty,
                EmployeeName = employee.Name,
                EmployeeCode = employee.EmployeeCode,
                LeaveType = leave.LeaveType,
                StartDate = leave.StartDate,
                EndDate = leave.EndDate,
                TotalDays = leave.TotalDays,
                Reason = leave.Reason,
                Status = leave.Status,
                AdminComments = leave.Comment
            }).OrderByDescending(l => l.StartDate).ToList();
            return View(employeeViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admins cannot apply for leave. This feature is for employees only.";
                return RedirectToAction("Index");
            }

            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found. Please contact your administrator.";
                return RedirectToAction("Index", "Home");
            }

            var leaveTypes = await _leaveTypeRepo.GetAllAsync();
            ViewBag.LeaveTypes = new SelectList(leaveTypes, "Name", "Name");

            var model = new LeaveApplication
            {
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
                Status = "Pending",
                AppliedOn = DateTime.UtcNow
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveApplication model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Admin"))
            {
                TempData["Error"] = "Admins cannot apply for leave. This feature is for employees only.";
                return RedirectToAction("Index");
            }

            var employee = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found. Please contact your administrator.";
                return RedirectToAction("Index", "Home");
            }

            model.EmployeeId = employee.Id;
            model.Status = "Pending";
            model.AppliedOn = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                var leaveTypes = await _leaveTypeRepo.GetAllAsync();
                ViewBag.LeaveTypes = new SelectList(leaveTypes, "Name", "Name");
                return View(model);
            }

            var balance = await _balanceRepo.GetByEmployeeYearAsync(employee.Id, model.StartDate.Year);
            int availableBalance = balance?.Balance?.GetValueOrDefault(model.LeaveType) ?? 0;
            int requestedDays = (model.EndDate - model.StartDate).Days + 1;
            model.TotalDays = requestedDays;

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
                    Reason = $"Exceeded balance for {model.LeaveType}. Original reason: {model.Reason}",
                    Status = "Pending",
                    AppliedOn = DateTime.UtcNow
                };
                await _leaveRepo.CreateAsync(unpaidLeave);

                TempData["Success"] = $"Leave application submitted. {paidDays} days as {model.LeaveType} leave and {unpaidDays} days as unpaid leave.";
            }
            else
            {
                await _leaveRepo.CreateAsync(model);
                TempData["Success"] = "Leave application submitted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, string comment)
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

            // Check if leave has already started
            if (leave.StartDate <= DateTime.UtcNow.Date)
            {
                TempData["Error"] = "Cannot change status after leave has started.";
                return RedirectToAction(nameof(Index));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            var previousStatus = leave.Status;

            // Update leave status
            leave.Status = "Approved";
            leave.Comment = string.IsNullOrWhiteSpace(comment) ? "Approved by admin" : comment;
            leave.DecidedBy = adminUser?.UserName ?? "Admin";
            leave.DecidedOn = DateTime.UtcNow;

            // Handle leave balance changes
            if (leave.LeaveType != "Unpaid")
            {
                var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId ?? string.Empty, leave.StartDate.Year);
                if (balance != null && balance.Balance.ContainsKey(leave.LeaveType))
                {
                    // If previously approved, first restore the balance
                    if (previousStatus == "Approved")
                    {
                        balance.Balance[leave.LeaveType] += leave.TotalDays;
                        balance.Used[leave.LeaveType] = Math.Max(0, balance.Used[leave.LeaveType] - leave.TotalDays);
                    }

                    // Now deduct for the new approval
                    balance.Balance[leave.LeaveType] = Math.Max(0, balance.Balance[leave.LeaveType] - leave.TotalDays);
                    balance.Used[leave.LeaveType] = (balance.Used?.GetValueOrDefault(leave.LeaveType) ?? 0) + leave.TotalDays;
                    balance.UpdatedOn = DateTime.UtcNow;
                    await _balanceRepo.UpdateAsync(balance);
                }
            }

            await _leaveRepo.UpdateAsync(leave);
            TempData["Success"] = $"Leave application approved successfully for {leave.TotalDays} day(s).";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string comment)
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

            // Check if leave has already started
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

            // Update leave status
            leave.Status = "Rejected";
            leave.Comment = comment;
            leave.DecidedBy = adminUser?.UserName ?? "Admin";
            leave.DecidedOn = DateTime.UtcNow;

            // Handle leave balance changes - restore balance if previously approved
            if (previousStatus == "Approved" && leave.LeaveType != "Unpaid")
            {
                var balance = await _balanceRepo.GetByEmployeeYearAsync(leave.EmployeeId ?? string.Empty, leave.StartDate.Year);
                if (balance != null && balance.Balance.ContainsKey(leave.LeaveType))
                {
                    balance.Balance[leave.LeaveType] += leave.TotalDays;
                    balance.Used[leave.LeaveType] = Math.Max(0, balance.Used[leave.LeaveType] - leave.TotalDays);
                    balance.UpdatedOn = DateTime.UtcNow;
                    await _balanceRepo.UpdateAsync(balance);
                }
            }

            await _leaveRepo.UpdateAsync(leave);
            TempData["Success"] = "Leave application rejected successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}