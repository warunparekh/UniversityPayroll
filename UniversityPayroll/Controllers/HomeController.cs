using Microsoft.AspNetCore.Mvc;

namespace UniversityPayroll.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
