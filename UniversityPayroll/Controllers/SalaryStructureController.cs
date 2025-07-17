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

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create(SalaryStructure model)
        {
            await _repo.CreateAsync(model);

            var leaveEntRepo = new LeaveEntitlementRepository(new MongoDbContext(
                HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<MongoDbSettings>)) as Microsoft.Extensions.Options.IOptions<MongoDbSettings>
            ));
            var ent = await leaveEntRepo.GetByDesignationAsync(model.Designation);
            if (ent == null)
            {
                await leaveEntRepo.CreateAsync(new LeaveEntitlement
                {
                    Designation = model.Designation,
                    Entitlements = new System.Collections.Generic.Dictionary<string, int>
                    {
                        ["CL"] = 12,
                        ["EL"] = 10,
                        ["HPL"] = 20
                    }
                });
            }

            return RedirectToAction(nameof(Index));
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
