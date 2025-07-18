using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LeaveApprovalController : Controller
    {
        private readonly LeaveRequestService _lr;
        private readonly LeaveApprovalService _lasvc;
        private readonly LeaveEntitlementService _lesvc;
        private readonly EmployeeService _esvc;

        public LeaveApprovalController(
            LeaveRequestService lr,
            LeaveApprovalService lasvc,
            LeaveEntitlementService lesvc,
            EmployeeService esvc)
        {
            _lr = lr;
            _lasvc = lasvc;
            _lesvc = lesvc;
            _esvc = esvc;
        }

        public async Task<IActionResult> Index() =>
            View(await _lasvc.GetPending());

        [HttpPost]
        public async Task<IActionResult> Approve(string id)
        {
            var req = await _lr.Get(id);
            if (req == null) return NotFound();

            await _lasvc.Approve(id, User.Identity.Name);

            // deduct entitlement
            var emp = await _esvc.Get(req.EmployeeId.ToString());
            var delta = req.IsHalfDay ? 1 : req.Days;
            await _lesvc.AdjustEntitlement(
                emp.DesignationId.ToString(),
                req.LeaveTypeId.ToString(),
                -delta);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string id)
        {
            var req = await _lr.Get(id);
            if (req == null) return NotFound();

            await _lasvc.Reject(id, User.Identity.Name);

            // if it was approved before, restore entitlement
            if (req.Status == LeaveStatus.Approved)
            {
                var emp = await _esvc.Get(req.EmployeeId.ToString());
                var delta = req.IsHalfDay ? 1 : req.Days;
                await _lesvc.AdjustEntitlement(
                    emp.DesignationId.ToString(),
                    req.LeaveTypeId.ToString(),
                    delta);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
