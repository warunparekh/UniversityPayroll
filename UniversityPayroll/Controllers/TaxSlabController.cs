// Controllers/TaxSlabController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TaxSlabController : Controller
    {
        private readonly TaxSlabService _svc;
        public TaxSlabController(TaxSlabService svc) => _svc = svc;

        public async Task<IActionResult> Index() =>
            View(await _svc.GetAll());

        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(TaxSlab m)
        {
            if (!ModelState.IsValid) return View(m);
            await _svc.Create(m);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var m = await _svc.Get(id);
            if (m == null) return NotFound();
            return View(m);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, TaxSlab m)
        {
            if (!ModelState.IsValid) return View(m);
            await _svc.Update(id, m);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _svc.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
