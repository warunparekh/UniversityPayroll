using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

namespace UniversityPayroll.Controllers
{
    [Authorize]
    public class LeaveController : Controller
    {
        private readonly LeaveRepository _leaveRepo;
        private readonly EmployeeRepository _employeeRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveController(LeaveRepository leaveRepo, EmployeeRepository employeeRepo, UserManager<ApplicationUser> userManager)
        {
            _leaveRepo = leaveRepo;
            _employeeRepo = employeeRepo;
            _userManager = userManager;

        }

        public  IActionResult Index()
        {
            if (User.IsInRole("Admin"))
                return View(_leaveRepo.GetAll());


            var id = ObjectId.Parse(_userManager.GetUserId(User));

            var employee = _employeeRepo.FindByIdentityUserId(id);

            if (employee is null)
                return View(Enumerable.Empty<LeaveApplication>());    

            var myLeaves = _leaveRepo.GetByEmployeeId(employee.EmployeeCode.ToString());
            return View(myLeaves);
        }


        public IActionResult Details(ObjectId id)
        {
            var leave = _leaveRepo.GetById(id);
            if (leave == null)
                return NotFound();
            return View(leave);
        }

        public IActionResult Create()
        {
            return View(new LeaveApplication());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(LeaveApplication leave)
        {
            if (ModelState.IsValid)
            {
                leave.AppliedOn = DateTime.Now;
                _leaveRepo.Create(leave);
                return RedirectToAction(nameof(Index));
            }
            return View(leave);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        public IActionResult Edit(ObjectId id)
        {
            var leave = _leaveRepo.GetById(id);
            if (leave == null)
                return NotFound();
            return View(leave);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CrudOnlyForAdmin")]
        public IActionResult Edit(ObjectId id, LeaveApplication leave)
        {
            if (ModelState.IsValid)
            {
                leave.Id = id;
                _leaveRepo.Update(id, leave);
                return RedirectToAction(nameof(Index));
            }
            return View(leave);
        }
        [Authorize(Policy = "CrudOnlyForAdmin")]
        public IActionResult Delete(ObjectId id)
        {
            var leave = _leaveRepo.GetById(id);
            if (leave != null)
            {
                _leaveRepo.Remove(id);
            }
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Accept(ObjectId id)
        {
            _leaveRepo.SetStatus(id, "Accepted");
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(ObjectId id)
        {
            _leaveRepo.SetStatus(id, "Rejected");
            return RedirectToAction(nameof(Index));
        }
    }
}
