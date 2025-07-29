using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using System.Threading.Tasks;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class DesignationController : Controller
    {
        private readonly DesignationRepository _designationRepo;

        public DesignationController(DesignationRepository designationRepo) => _designationRepo = designationRepo;

        public async Task<IActionResult> Index() => View(await _designationRepo.GetAllAsync());

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Designation model)
        {
            if (await _designationRepo.GetByNameAsync(model.Name) != null)
            {
                ModelState.AddModelError("Name", "Designation already exists");
                return View(model);
            }
            await _designationRepo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var designation = await _designationRepo.GetByIdAsync(id);
            return designation == null ? NotFound() : View(designation);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Designation model)
        {
            await _designationRepo.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            await _designationRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}