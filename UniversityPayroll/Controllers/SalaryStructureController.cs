using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class SalaryStructureController : Controller
    {
        private readonly SalaryStructureRepository _repo;

        public SalaryStructureController(SalaryStructureRepository repo)
        {
            _repo = repo;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _repo.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
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
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SalaryStructure model)
        {
            model.UpdatedOn = DateTime.UtcNow;
            await _repo.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _repo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}