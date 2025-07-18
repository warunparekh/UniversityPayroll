using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize(Roles = "Employee")]
    public class LeaveRequestController : Controller
    {
        private readonly LeaveRequestService _lr;
        private readonly EmployeeService _esvc;
        private readonly LeaveTypeService _ltsvc;
        private readonly UserManager<ApplicationUser> _um;

        public LeaveRequestController(
            LeaveRequestService lr,
            EmployeeService esvc,
            LeaveTypeService ltsvc,
            UserManager<ApplicationUser> um)
        {
            _lr = lr;
            _esvc = esvc;
            _ltsvc = ltsvc;
            _um = um;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _um.GetUserAsync(User);
            var emp = await _esvc.GetByUserId(user.Id.ToString());
            return View(await _lr.GetByEmployee(emp.Id.ToString()));
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.LeaveTypes = new SelectList(await _ltsvc.GetAll(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            DateTime from,
            DateTime to,
            string leaveTypeId,
            bool isHalfDay,
            string reason)
        {
            var user = await _um.GetUserAsync(User);
            var emp = await _esvc.GetByUserId(user.Id.ToString());
            var days = (to - from).Days + 1;

            await _lr.Create(new LeaveRequest
            {
                EmployeeId = emp.Id,
                LeaveTypeId = ObjectId.Parse(leaveTypeId),
                From = from,
                To = to,
                Days = days,
                IsHalfDay = isHalfDay,
                Reason = reason
            });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var req = await _lr.Get(id);
            return req == null ? NotFound() : View(req);
        }
    }
}
