using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using System.Collections.Generic;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class TaxSlabController : Controller
    {
        private readonly TaxSlabRepository _repo;

        public TaxSlabController(TaxSlabRepository repo)
        {
            _repo = repo;
        }

        #region Helper Methods

        private static TaxSlab CreateNewTaxSlab() => new()
        {
            Slabs = Enumerable.Range(0, 4).Select(_ => new Slab()).ToList()
        };

        private static void EnsureMinimumSlabs(TaxSlab taxSlab)
        {
            while (taxSlab.Slabs.Count < 4)
                taxSlab.Slabs.Add(new Slab());
        }

        private static List<Slab> FilterValidSlabs(IEnumerable<Slab> slabs) =>
            slabs.Where(s => s.Rate > 0).ToList();

        #endregion

        public async Task<IActionResult> Index() => View(await _repo.GetAllAsync());

        [HttpGet]
        public IActionResult Create() => View(CreateNewTaxSlab());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaxSlab model)
        {
            var now = DateTime.UtcNow;
            model.CreatedOn = now;
            model.UpdatedOn = now;
            model.Slabs = FilterValidSlabs(model.Slabs);
            
            await _repo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            
            EnsureMinimumSlabs(item);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaxSlab model)
        {
            model.UpdatedOn = DateTime.UtcNow;
            model.Slabs = FilterValidSlabs(model.Slabs);
            
            await _repo.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _repo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] TaxSlab model)
        {
            var now = DateTime.UtcNow;
            model.CreatedOn = now;
            model.UpdatedOn = now;
            model.Slabs = FilterValidSlabs(model.Slabs);
            
            await _repo.CreateAsync(model);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] TaxSlab model)
        {
            model.UpdatedOn = DateTime.UtcNow;
            model.Slabs = FilterValidSlabs(model.Slabs);
            
            await _repo.UpdateAsync(model);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax([FromBody] string id)
        {
            await _repo.DeleteAsync(id);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            
            EnsureMinimumSlabs(item);
            return Json(item);
        }
    }
}
