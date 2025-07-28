using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "CrudOnlyForAdmin")]
    public class SalaryStructureController : Controller
    {
        private readonly SalaryStructureRepository _repo;
        private readonly DesignationRepository _designationRepo;

        public SalaryStructureController(
            SalaryStructureRepository repo,
            DesignationRepository designationRepo)
        {
            _repo = repo;
            _designationRepo = designationRepo;
        }

        #region Helper Methods

        private async Task PopulateDesignations(string? selectedDesignation = null)
        {
            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", selectedDesignation);
        }

        private static SalaryStructure CreateNewSalaryStructure() => new()
        {
            Allowances = new Allowances(),
            Pf = new PfRules()
        };

        #endregion

        public async Task<IActionResult> Index() => View(await _repo.GetAllAsync());

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDesignations();
            return View(CreateNewSalaryStructure());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalaryStructure model)
        {
            model.CreatedOn = DateTime.UtcNow;
            model.UpdatedOn = DateTime.UtcNow;
            await _repo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _repo.GetByIdAsync(id);
            if (model == null) return NotFound();

            await PopulateDesignations(model.Designation);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SalaryStructure model)
        {
            model.UpdatedOn = DateTime.UtcNow;
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