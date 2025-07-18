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
        

        public AdminController(
            )
        {
            
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        
    }
}
