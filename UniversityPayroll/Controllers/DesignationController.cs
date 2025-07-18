using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Linq;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DesignationController : Controller
    {
        private readonly DesignationService _dsvc;
        private readonly LeaveTypeService _ltsvc;
        private readonly LeaveEntitlementService _lesvc;

        public DesignationController(
            DesignationService dsvc,
            LeaveTypeService ltsvc,
            LeaveEntitlementService lesvc)
        {
            _dsvc = dsvc;
            _ltsvc = ltsvc;
            _lesvc = lesvc;
        }

        // INDEX: show name + comma-list of Type:Quota
        public async Task<IActionResult> Index()
        {
            var desigs = await _dsvc.GetAll();
            var allTypes = await _ltsvc.GetAll();
            var allEnts = await _lesvc.GetAll();

            var model = desigs
              .Select(d => new DesignationIndexItem
              {
                  Id = d.Id.ToString(),
                  Name = d.Name,
                  Entitlements = allEnts
                    .Where(e => e.DesignationId == d.Id)
                    .Select(e => {
                        var t = allTypes.FirstOrDefault(x => x.Id == e.LeaveTypeId);
                        return $"{t?.Name}:{e.AnnualQuota}";
                    })
                    .ToList()
              });

            return View(model);
        }

        // GET CREATE
        public async Task<IActionResult> Create()
        {
            var types = await _ltsvc.GetAll();
            var vm = new DesignationViewModel
            {
                Entitlements = types
                  .Select(t => new EntitlementInput
                  {
                      LeaveTypeId = t.Id.ToString(),
                      LeaveTypeName = t.Name,
                      Quota = 0
                  })
                  .ToList()
            };
            return View(vm);
        }

        // POST CREATE
        [HttpPost]
        public async Task<IActionResult> Create(DesignationViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var desig = new Designation { Name = vm.Name };
            await _dsvc.Create(desig);

            foreach (var e in vm.Entitlements)
            {
                await _lesvc.Create(new LeaveEntitlement
                {
                    DesignationId = desig.Id,
                    LeaveTypeId = ObjectId.Parse(e.LeaveTypeId),
                    AnnualQuota = e.Quota
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // GET EDIT
        public async Task<IActionResult> Edit(string id)
        {
            var desig = await _dsvc.Get(id);
            if (desig == null) return NotFound();

            var types = await _ltsvc.GetAll();
            var existing = (await _lesvc.GetByDesignation(id))
                .ToDictionary(x => x.LeaveTypeId.ToString(), x => x.AnnualQuota);

            var vm = new DesignationViewModel
            {
                Name = desig.Name,
                Entitlements = types
                  .Select(t => new EntitlementInput
                  {
                      LeaveTypeId = t.Id.ToString(),
                      LeaveTypeName = t.Name,
                      Quota = existing.TryGetValue(t.Id.ToString(), out var q) ? q : 0
                  })
                  .ToList()
            };
            ViewData["DesigId"] = id;
            return View(vm);
        }

        // POST EDIT
        [HttpPost]
        public async Task<IActionResult> Edit(
            string id,
            DesignationViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewData["DesigId"] = id;
                return View(vm);
            }

            var desig = await _dsvc.Get(id);
            desig.Name = vm.Name;
            await _dsvc.Update(id, desig);

            foreach (var e in vm.Entitlements)
            {
                var ents = await _lesvc.GetByDesignation(id);
                var ent = ents.FirstOrDefault(x => x.LeaveTypeId.ToString() == e.LeaveTypeId);

                if (ent != null)
                {
                    ent.AnnualQuota = e.Quota;
                    await _lesvc.Update(ent.Id.ToString(), ent);
                }
                else
                {
                    await _lesvc.Create(new LeaveEntitlement
                    {
                        DesignationId = desig.Id,
                        LeaveTypeId = ObjectId.Parse(e.LeaveTypeId),
                        AnnualQuota = e.Quota
                    });
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // POST DELETE
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _dsvc.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
