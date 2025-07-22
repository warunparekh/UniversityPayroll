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

            await CreateDefaultSalaryStructure(model.Name);
            await CreateDefaultLeaveEntitlement(model.Name);

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

        private async Task CreateDefaultSalaryStructure(string designation)
        {
            var existing = await _salaryStructRepo.GetByDesignationAsync(designation);
            if (existing == null)
            {
                var allowances = designation.Contains("Professor") ?
                    new Allowances { DaPercent = 12, HraPercent = 20 } :
                    new Allowances { DaPercent = 8, HraPercent = 15 };

                var incrementPercent = designation.Contains("Professor") ? 5 : 3;

                await _salaryStructRepo.CreateAsync(new SalaryStructure
                {
                    Designation = designation,
                    Allowances = allowances,
                    Pf = new PfRules { EmployeePercent = 12, EmployerPercent = 12, EdliPercent = 0.5 },
                    AnnualIncrementPercent = incrementPercent
                });
            }
        }

        private async Task CreateDefaultLeaveEntitlement(string designation)
        {
            var existing = await _entitlementRepo.GetByDesignationAsync(designation);
            if (existing == null)
            {
                var entitlements = designation.Contains("Professor") ?
                    new Dictionary<string, int> { { "Sick", 15 }, { "Casual", 8 } } :
                    new Dictionary<string, int> { { "Sick", 10 }, { "Casual", 6 } };

                await _entitlementRepo.CreateAsync(new LeaveEntitlement
                {
                    Designation = designation,
                    Entitlements = entitlements
                });
            }
        }
    }
}