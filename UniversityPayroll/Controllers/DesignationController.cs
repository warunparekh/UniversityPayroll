using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "CrudOnlyForAdmin")]
    public class DesignationController : Controller
    {
        private readonly DesignationRepository _designationRepo;
        private readonly SalaryStructureRepository _salaryStructRepo;
        private readonly LeaveEntitlementRepository _entitlementRepo;

        public DesignationController(
            DesignationRepository designationRepo,
            SalaryStructureRepository salaryStructRepo,
            LeaveEntitlementRepository entitlementRepo)
        {
            _designationRepo = designationRepo;
            _salaryStructRepo = salaryStructRepo;
            _entitlementRepo = entitlementRepo;
        }

        public async Task<IActionResult> Index()
        {
            var designations = await _designationRepo.GetAllAsync();
            return View(designations);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Designation model)
        {
           
            var existing = await _designationRepo.GetByNameAsync(model.Name);
            if (existing != null)
            {
                ModelState.AddModelError("Name", "Designation already exists");
                return View(model);
            }

            await _designationRepo.CreateAsync(model);

            

            return RedirectToAction(nameof(Index));
            
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var designation = await _designationRepo.GetByIdAsync(id);
            if (designation == null) return NotFound();
            return View(designation);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Designation model)
        {
            
            await _designationRepo.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _designationRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

       

        
    }
}