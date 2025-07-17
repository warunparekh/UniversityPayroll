// Controllers/TaxSlabController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

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

        public async Task<IActionResult> Index()
        {
            var list = await _repo.GetAllAsync();
            return View(list);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new TaxSlab
            {
                Slabs = Enumerable.Range(0, 4).Select(_ => new Slab()).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaxSlab model)
        {
            model.CreatedOn = DateTime.UtcNow;
            model.UpdatedOn = DateTime.UtcNow;
            // remove empty slabs (rate==0)
            model.Slabs = model.Slabs.Where(s => s.Rate > 0).ToList();
            await _repo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            // pad to 4
            while (item.Slabs.Count < 4) item.Slabs.Add(new Slab());
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TaxSlab model)
        {
            model.UpdatedOn = DateTime.UtcNow;
            model.Slabs = model.Slabs.Where(s => s.Rate > 0).ToList();
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
