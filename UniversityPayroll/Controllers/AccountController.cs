using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signIn;
        public AccountController(SignInManager<ApplicationUser> signIn) =>
            _signIn = signIn;

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model: returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Login(
            string email, string password, string returnUrl = null)
        {
            var res = await _signIn.PasswordSignInAsync(
                email, password, false, false);

            if (res.Succeeded)
                return Redirect(returnUrl ?? "/");

            ModelState.AddModelError("", "Invalid login");
            return View(model: returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}
