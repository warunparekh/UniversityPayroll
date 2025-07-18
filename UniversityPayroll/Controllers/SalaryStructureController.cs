using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace UniversityPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalaryStructureController : Controller
    {
        private readonly SalaryStructureService _ssvc;
        private readonly DesignationService _dsvc;

        public SalaryStructureController(
            SalaryStructureService ssvc,
            DesignationService dsvc)
        {
            _ssvc = ssvc;
            _dsvc = dsvc;
        }

        public async Task<IActionResult> Index() =>
            View(await _ssvc.GetAll());

        public async Task<IActionResult> Create()
        {
            ViewBag.Designations = new SelectList(
                await _dsvc.GetAll(), "Id", "Name");
            var m = new SalaryStructure();
            // initialize 3 blank slots each
            m.Allowances.AddRange(Enumerable.Repeat(new Allowance(), 3));
            m.Deductions.AddRange(Enumerable.Repeat(new Deduction(), 3));
            return View(m);
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalaryStructure m)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Designations = new SelectList(
                    await _dsvc.GetAll(), "Id", "Name");
                return View(m);
            }
            await _ssvc.Create(m);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var m = await _ssvc.Get(id);
            if (m == null) return NotFound();
            ViewBag.Designations = new SelectList(
                await _dsvc.GetAll(), "Id", "Name", m.DesignationId);
            // ensure at least 3 slots for editing
            while (m.Allowances.Count < 3) m.Allowances.Add(new Allowance());
            while (m.Deductions.Count < 3) m.Deductions.Add(new Deduction());
            return View(m);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, SalaryStructure m)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Designations = new SelectList(
                    await _dsvc.GetAll(), "Id", "Name", m.DesignationId);
                return View(m);
            }
            await _ssvc.Update(id, m);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _ssvc.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
