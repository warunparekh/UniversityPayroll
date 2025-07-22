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

            var model = new SalaryStructure
            {
                Allowances = new Allowances(),
                Pf = new PfRules()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalaryStructure model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedOn = DateTime.UtcNow;
                model.UpdatedOn = DateTime.UtcNow;
                await _repo.CreateAsync(model);
                return RedirectToAction(nameof(Index));
            }

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", model.Designation);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _repo.GetByIdAsync(id);
            if (model == null) return NotFound();

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", model.Designation);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SalaryStructure model)
        {
            if (ModelState.IsValid)
            {
                model.UpdatedOn = DateTime.UtcNow;
                await _repo.UpdateAsync(model);
                return RedirectToAction(nameof(Index));
            }

            var designations = await _designationRepo.GetActiveAsync();
            ViewBag.Designations = new SelectList(designations, "Name", "Name", model.Designation);
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