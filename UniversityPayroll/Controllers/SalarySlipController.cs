// Controllers/SalarySlipController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Linq;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SalarySlipController : Controller
    {
        private readonly SalarySlipService _ss;
        private readonly EmployeeService _esvc;
        private readonly SalaryStructureService _strsvc;
        private readonly TaxSlabService _tsvc;

        public SalarySlipController(
            SalarySlipService ss,
            EmployeeService esvc,
            SalaryStructureService strsvc,
            TaxSlabService tsvc)
        {
            _ss = ss; _esvc = esvc; _strsvc = strsvc; _tsvc = tsvc;
        }

        public async Task<IActionResult> Index() =>
            View(await _ss.GetAll());

        public IActionResult Generate() => View();

        [HttpPost]
        public async Task<IActionResult> Generate(int month, int year)
        {
            var emps = await _esvc.GetAll();
            var slabs = await _tsvc.GetAll();
            foreach (var emp in emps)
            {
                var str = (await _strsvc.GetAll())
                    .FirstOrDefault(s => s.DesignationId == emp.DesignationId);
                if (str == null) continue;
                var gross = str.Basic;
                var slab = slabs.FirstOrDefault(t => gross >= t.MinAnnualIncome && gross <= t.MaxAnnualIncome);
                var rate = slab?.Rate ?? 0;
                var tax = gross * (decimal)rate / 100;
                await _ss.Create(new SalarySlip
                {
                    EmployeeId = emp.Id,
                    Month = month,
                    Year = year,
                    Gross = gross,
                    Tax = tax,
                    Net = gross - tax,
                    GeneratedOn = DateTime.UtcNow
                });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
