using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly EmployeeService _esvc;
        private readonly DesignationService _dsvc;
        private readonly LeaveEntitlementService _lesvc;
        private readonly LeaveTypeService _ltsvc;
        private readonly LeaveRequestService _lr;
        private readonly SalarySlipService _ss;
        private readonly UserManager<ApplicationUser> _um;

        public ProfileController(
            EmployeeService esvc,
            DesignationService dsvc,
            LeaveEntitlementService lesvc,
            LeaveTypeService ltsvc,
            LeaveRequestService lr,
            SalarySlipService ss,
            UserManager<ApplicationUser> um)
        {
            _esvc = esvc;
            _dsvc = dsvc;
            _lesvc = lesvc;
            _ltsvc = ltsvc;
            _lr = lr;
            _ss = ss;
            _um = um;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _um.GetUserAsync(User);
            var emp = await _esvc.GetByUserId(user.Id.ToString());
            var desig = await _dsvc.Get(emp.DesignationId.ToString());

            // fetch entitlements & leave types
            var ents = await _lesvc.GetByDesignation(emp.DesignationId.ToString());
            var types = await _ltsvc.GetAll();

            // fetch all approved leaves for this employee
            var leaves = (await _lr.GetByEmployee(emp.Id.ToString()))
                .Where(r => r.Status == LeaveStatus.Approved)
                .ToList();

            // build per-type summary
            var entSummaries = ents.Select(e =>
            {
                var lt = types.FirstOrDefault(t => t.Id == e.LeaveTypeId);
                var usedDays = leaves
                    .Where(r => r.LeaveTypeId == e.LeaveTypeId)
                    .Sum(r => r.IsHalfDay ? 0.5m : r.Days);

                return new LeaveSummary
                {
                    TypeName = lt?.Name ?? "Unknown",
                    Quota = e.AnnualQuota,
                    Used = usedDays,
                    Available = e.AnnualQuota - usedDays
                };
            }).ToList();

            ViewBag.Name = user.FullName;
            ViewBag.Designation = desig.Name;
            ViewBag.Leaves = entSummaries;
            ViewBag.SalarySlips = await _ss.GetByEmployee(emp.Id.ToString());

            return View();
        }
    }

    // helper class for view
    public class LeaveSummary
    {
        public string TypeName { get; set; }
        public decimal Quota { get; set; }
        public decimal Used { get; set; }
        public decimal Available { get; set; }
    }
}
