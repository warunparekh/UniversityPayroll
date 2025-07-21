// Controllers/AdminController.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly SalarySlipRepository _slipRepo;

        public AdminController(SalarySlipRepository slipRepo)
        {
            _slipRepo = slipRepo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult RunPayroll() => View();

      
    }
}
