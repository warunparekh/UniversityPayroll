
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using UniversityPayroll.Models;
using UniversityPayroll.ViewModels;

namespace UniversityPayroll.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _um;
        private readonly SignInManager<ApplicationUser> _sm;

        public AccountController(
            UserManager<ApplicationUser> um,
            SignInManager<ApplicationUser> sm)
        {
            _um = um;
            _sm = sm;
        }


        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel m, string returnUrl = null)
        {
            if (!ModelState.IsValid) return View(m);

            var r = await _sm.PasswordSignInAsync(m.Email, m.Password, m.RememberMe, false);
            if (r.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }
            ModelState.AddModelError("", "Invalid login.");
            return View(m);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _sm.SignOutAsync();
            return RedirectToAction("Login");
        }




    }
}