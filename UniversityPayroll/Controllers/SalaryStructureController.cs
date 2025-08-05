using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // Helper methods removed – React UI handles form rendering client-side

        public async Task<IActionResult> Index() => View(await _repo.GetAllAsync());

        // Traditional MVC actions (Create/Edit/Delete) removed; React UI uses AJAX endpoints below
        
        // AJAX endpoints for React-based CRUD
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] SalaryStructure model)
        {
            var now = DateTime.UtcNow;
            model.CreatedOn = now;
            model.UpdatedOn = now;
            await _repo.CreateAsync(model);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] SalaryStructure model)
        {
            model.UpdatedOn = DateTime.UtcNow;
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
            return Json(item);
        }
    }
}