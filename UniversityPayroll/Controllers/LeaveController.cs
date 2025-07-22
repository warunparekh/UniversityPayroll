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
using MongoDB.Driver;

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
                    IsHalfDay = leave.IsHalfDay,
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
                IsHalfDay = leave.IsHalfDay,
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

            int workingDays = CalculateWorkingDays(model.StartDate, model.EndDate);
            decimal requestedDays = model.IsHalfDay ? 0.5m : workingDays;
            model.TotalDays = requestedDays;

            var balance = await _balanceRepo.GetByEmployeeYearAsync(employee.Id, model.StartDate.Year);
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
        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _leaveRepo.DeleteAsync(id);

            return RedirectToAction(nameof(Index));
        }
            
    }
}