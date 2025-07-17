// Controllers/AdminController.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly PayrollService _payrollService;
        private readonly SalarySlipRepository _slipRepo;

        public AdminController(
            PayrollService payrollService,
            SalarySlipRepository slipRepo)
        {
            _payrollService = payrollService;
            _slipRepo = slipRepo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult RunPayroll() => View();

        [HttpPost]
        public async Task<IActionResult> RunPayroll(int month, int year)
        {
            var slips = await _payrollService.RunAsync(month, year);
            foreach (var slip in slips)
                await _slipRepo.CreateAsync(slip);

            ViewBag.Count = slips.Count;
            return View("RunPayrollResult");
        }
    }
}
