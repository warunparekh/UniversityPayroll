// Controllers/EmployeeController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniversityPayroll.Models;
using UniversityPayroll.Services;

namespace UniversityPayroll.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        public class CreateModel { public string FullName { get; set; } public string Email { get; set; } public string Password { get; set; } public string DesignationId { get; set; } }
        public class EditModel { public string Id { get; set; } public string FullName { get; set; } public string Email { get; set; } public string DesignationId { get; set; } }
        public class ListModel { public string Id { get; set; } public string FullName { get; set; } public string Email { get; set; } public string Designation { get; set; } }

        private readonly EmployeeService _esvc;
        private readonly DesignationService _dsvc;
        private readonly UserManager<ApplicationUser> _um;

        public EmployeeController(EmployeeService esvc, DesignationService dsvc, UserManager<ApplicationUser> um)
        {
            _esvc = esvc; _dsvc = dsvc; _um = um;
        }

        public async Task<IActionResult> Index()
        {
            var emps = await _esvc.GetAll();
            var list = new List<ListModel>();
            foreach (var e in emps)
            {
                var user = await _um.FindByIdAsync(e.UserId);
                var desig = await _dsvc.Get(e.DesignationId.ToString());
                list.Add(new ListModel
                {
                    Id = e.Id.ToString(),
                    FullName = user.FullName,
                    Email = user.Email,
                    Designation = desig.Name
                });
            }
            return View(list);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Designations = await _dsvc.GetAll();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateModel m)
        {
            if (!ModelState.IsValid) { ViewBag.Designations = await _dsvc.GetAll(); return View(m); }

            var user = new ApplicationUser { UserName = m.Email, Email = m.Email, FullName = m.FullName };
            var res = await _um.CreateAsync(user, m.Password);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                ViewBag.Designations = await _dsvc.GetAll();
                return View(m);
            }

            await _um.AddToRoleAsync(user, "Employee");
            await _esvc.Create(new Employee { UserId = user.Id.ToString(), DesignationId = ObjectId.Parse(m.DesignationId) });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var emp = await _esvc.Get(id);
            if (emp == null) return NotFound();
            var user = await _um.FindByIdAsync(emp.UserId);
            ViewBag.Designations = await _dsvc.GetAll();
            return View(new EditModel
            {
                Id = id,
                FullName = user.FullName,
                Email = user.Email,
                DesignationId = emp.DesignationId.ToString()
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditModel m)
        {
            if (!ModelState.IsValid) { ViewBag.Designations = await _dsvc.GetAll(); return View(m); }

            var emp = await _esvc.Get(m.Id);
            emp.DesignationId = ObjectId.Parse(m.DesignationId);
            await _esvc.Update(m.Id, emp);

            var user = await _um.FindByIdAsync(emp.UserId);
            user.FullName = m.FullName;
            user.Email = m.Email;
            user.UserName = m.Email;
            await _um.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var emp = await _esvc.Get(id);
            if (emp != null)
            {
                var user = await _um.FindByIdAsync(emp.UserId);
                if (user != null) await _um.DeleteAsync(user);
                await _esvc.Delete(id);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(string id)
        {
            var emp = await _esvc.Get(id);
            if (emp == null) return NotFound();
            var user = await _um.FindByIdAsync(emp.UserId);
            var desig = await _dsvc.Get(emp.DesignationId.ToString());
            ViewBag.Name = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.Designation = desig.Name;
            return View();
        }
    }
}
