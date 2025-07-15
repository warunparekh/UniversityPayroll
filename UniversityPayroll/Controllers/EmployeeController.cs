using Microsoft.AspNetCore.Mvc;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;
using Microsoft.AspNetCore.Identity;
using UniversityPayroll.Models.ViewModels;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "CrudOnlyForAdmin")]
    public class EmployeeController : Controller
    {
        private readonly EmployeeRepository _employeeRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(EmployeeRepository employeeRepo, UserManager<ApplicationUser> userManager)
        {
            _employeeRepo = employeeRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var employees = _employeeRepo.GetAll();
            var result = new List<EmployeeRowViewModel>();

            foreach (var e in employees)
            {
                var user = await _userManager.FindByIdAsync(e.IdentityUserId.ToString());
                var roles = user is null
                    ? "(none)"
                    : string.Join(", ", await _userManager.GetRolesAsync(user));

                result.Add(new EmployeeRowViewModel
                {
                    Employee = e,
                    Email = user?.Email ?? "(n/a)",
                    Roles = roles
                });
            }
            return View(result);


        }

        public IActionResult Details(ObjectId id)
        {
            var employee = _employeeRepo.GetById(id);
            if (employee == null)
                return NotFound();
            return View(employee);
        }

        public IActionResult Create()
        {
            return View(new Employee());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "CrudOnlyForAdmin")]
        public async Task<IActionResult> Create(Employee employee, string email, string password)
        {
            if (!ModelState.IsValid) return View(employee);
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email
                };

                await _userManager.CreateAsync(user, password);

                await _userManager.AddToRoleAsync(user, "User");

                employee.IdentityUserId = user.Id;
                _employeeRepo.Create(employee);

                return RedirectToAction(nameof(Index));
            }
        }


        public async Task<IActionResult> Edit(ObjectId id)
        {
            var employee = _employeeRepo.GetById(id);
            if (employee == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(employee.IdentityUserId.ToString());
            ViewBag.IsAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");


            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ObjectId id, Employee employee, bool isAdmin)
        {


            var emp = _employeeRepo.GetById(id);
            emp.EmployeeCode = employee.EmployeeCode;
            emp.FirstName = employee.FirstName;
            emp.LastName = employee.LastName;
            emp.Department = employee.Department;
            emp.DateOfJoining = employee.DateOfJoining;
            emp.BasicPay = employee.BasicPay;
            emp.HraPercent = employee.HraPercent;
            emp.DaPercent = employee.DaPercent;
            emp.Status = employee.Status;
            emp.Designation = employee.Designation;

            emp.BankAccount.IFSC = employee.BankAccount.IFSC;
            emp.BankAccount.AccountNumber = employee.BankAccount.AccountNumber;

            _employeeRepo.Update(id, emp);

            var user = await _userManager.FindByIdAsync(emp.IdentityUserId.ToString());
            if (user != null)
            {
                bool currentlyAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                if (isAdmin && !currentlyAdmin)
                    await _userManager.AddToRoleAsync(user, "Admin");
                else if (!isAdmin && currentlyAdmin)
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(ObjectId id)
        {

            var emp = _employeeRepo.GetById(id);
            _employeeRepo.Remove(id);

            var user = await _userManager.FindByIdAsync(emp.IdentityUserId.ToString());
            if (user != null)
                await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}
