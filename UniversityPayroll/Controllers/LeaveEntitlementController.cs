using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using System.Collections.Generic;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class LeaveEntitlementController : Controller
    {
        private readonly LeaveEntitlementRepository _repo;
        private readonly LeaveTypeRepository _typeRepo;
        private readonly DesignationRepository _designationRepo;

        public LeaveEntitlementController(
            LeaveEntitlementRepository repo,
            LeaveTypeRepository typeRepo,
            DesignationRepository designationRepo)
        {
            _repo = repo;
            _typeRepo = typeRepo;
            _designationRepo = designationRepo;
        }

        #region Helper Methods

        private async Task PopulateViewBags(string? selectedDesignation = null)
        {
            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", selectedDesignation);
            ViewBag.LeaveTypes = await _typeRepo.GetAllAsync();
        }

        private async Task<Dictionary<string, int>> FilterValidEntitlements(Dictionary<string, int> entitlements)
        {
            var validLeaveTypes = (await _typeRepo.GetAllAsync()).Select(t => t.Name).ToHashSet();
            return entitlements
                .Where(kv => validLeaveTypes.Contains(kv.Key) && kv.Value > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private static LeaveEntitlement CreateNewLeaveEntitlement() => new()
        {
            Entitlements = new Dictionary<string, int>()
        };

        #endregion

        public async Task<IActionResult> Index() => View(await _repo.GetAllAsync());

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateViewBags();
            return View(CreateNewLeaveEntitlement());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveEntitlement model)
        {
            model.Entitlements = await FilterValidEntitlements(model.Entitlements);
            await _repo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();

            await PopulateViewBags(item.Designation);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeaveEntitlement model)
        {
            model.Entitlements = await FilterValidEntitlements(model.Entitlements);
            await _repo.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _repo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}