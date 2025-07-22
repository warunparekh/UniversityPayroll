using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "CrudOnlyForAdmin")]
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

        public async Task<IActionResult> Index()
        {
            var list = await _repo.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name");
            ViewBag.LeaveTypes = await _typeRepo.GetAllAsync();
            return View(new LeaveEntitlement { Entitlements = new System.Collections.Generic.Dictionary<string, int>() });
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveEntitlement model)
        {
            if (ModelState.IsValid)
            {
                var leaveTypes = (await _typeRepo.GetAllAsync()).Select(t => t.Name).ToHashSet();
                model.Entitlements = model.Entitlements
                    .Where(kv => leaveTypes.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                await _repo.CreateAsync(model);
                return RedirectToAction(nameof(Index));
            }

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", model.Designation);
            ViewBag.LeaveTypes = await _typeRepo.GetAllAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", item.Designation);
            ViewBag.LeaveTypes = await _typeRepo.GetAllAsync();
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(LeaveEntitlement model)
        {
            if (ModelState.IsValid)
            {
                var leaveTypes = (await _typeRepo.GetAllAsync()).Select(t => t.Name).ToHashSet();
                model.Entitlements = model.Entitlements
                    .Where(kv => leaveTypes.Contains(kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                await _repo.UpdateAsync(model);
                return RedirectToAction(nameof(Index));
            }

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", model.Designation);
            ViewBag.LeaveTypes = await _typeRepo.GetAllAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _repo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}