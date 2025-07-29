using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using System.Collections.Generic;

namespace UniversityPayroll.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationRepository _notificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(
            NotificationRepository notificationRepo, 
            UserManager<ApplicationUser> userManager)
        {
            _notificationRepo = notificationRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var notifications = await _notificationRepo.GetByUserIdAsync(user.Id.ToString());
            return View(notifications ?? new List<Notification>());
        }
    }
}