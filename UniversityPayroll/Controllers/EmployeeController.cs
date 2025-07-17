using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using UniversityPayroll.ViewModels;

namespace UniversityPayroll.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly EmployeeRepository _employeeRepo;
        private readonly SalaryStructureRepository _salaryStructRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TaxSlabRepository _taxRepo;
        private readonly LeaveBalanceRepository _leaveBalanceRepo;
        private readonly SalarySlipRepository _salarySlipRepo;

        public EmployeeController(
            EmployeeRepository employeeRepo,
            SalaryStructureRepository salaryStructRepo,
            UserManager<ApplicationUser> userManager,
            TaxSlabRepository taxRepo,
            LeaveBalanceRepository leaveBalanceRepo,
            SalarySlipRepository salarySlipRepo)
        {
            _employeeRepo = employeeRepo;
            _salaryStructRepo = salaryStructRepo;
            _userManager = userManager;
            _taxRepo = taxRepo;
            _leaveBalanceRepo = leaveBalanceRepo;
            _salarySlipRepo = salarySlipRepo;
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        public async Task<IActionResult> Index()
        {
            var list = await _employeeRepo.GetAllAsync();
            return View(list);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var designations = (await _salaryStructRepo.GetAllAsync())
                .Select(s => s.Designation)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            ViewBag.Designations = new SelectList(designations);
            return View();
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create(Employee model, string email, string password)
        {
            var user = new ApplicationUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Errors.First().Description);
                var designations = (await _salaryStructRepo.GetAllAsync())
                    .Select(s => s.Designation)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();
                ViewBag.Designations = new SelectList(designations, model.Designation);
                return View(model);
            }

            model.IdentityUserId = user.Id.ToString();
            await _employeeRepo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var emp = await _employeeRepo.GetByIdAsync(id);
            if (emp == null) return NotFound();

            var designations = (await _salaryStructRepo.GetAllAsync())
                .Select(s => s.Designation)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            ViewBag.Designations = new SelectList(designations, emp.Designation);

            return View(emp);
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Employee model)
        {
            await _employeeRepo.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CrudOnlyForAdmin")]
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var emp = await _employeeRepo.GetByIdAsync(id);
            if (emp != null && !string.IsNullOrEmpty(emp.IdentityUserId))
            {
                var user = await _userManager.FindByIdAsync(emp.IdentityUserId);
                if (user != null)
                    await _userManager.DeleteAsync(user);
            }
            await _employeeRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }


        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            var emp = await _employeeRepo.GetByUserIdAsync(user.Id.ToString());
            var structure = await _salaryStructRepo.GetByDesignationAsync(emp.Designation);
            var taxSlab = emp.TaxSlabId != null ? await _taxRepo.GetByIdAsync(emp.TaxSlabId) : null;
            var year = DateTime.UtcNow.Year;

            var entRepo = new LeaveEntitlementRepository(new MongoDbContext(
                HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<MongoDbSettings>)) as Microsoft.Extensions.Options.IOptions<MongoDbSettings>
            ));
            var ent = await entRepo.GetByDesignationAsync(emp.Designation);

            var balance = await _leaveBalanceRepo.GetByEmployeeYearAsync(emp.Id, year);
            if (balance == null)
            {
                var entitlements = ent?.Entitlements ?? new System.Collections.Generic.Dictionary<string, int>
                {
                    ["CL"] = 12,
                    ["EL"] = 10,
                    ["HPL"] = 20
                };
                var used = entitlements.ToDictionary(x => x.Key, x => 0);
                balance = new LeaveBalance
                {
                    EmployeeId = emp.Id,
                    Year = year,
                    Entitlements = entitlements,
                    Used = used,
                    Balance = new System.Collections.Generic.Dictionary<string, int>(entitlements),
                    LastAccrualDate = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                };
                await _leaveBalanceRepo.CreateAsync(balance);
            }

            var slips = await _salarySlipRepo.GetByEmployeeAsync(emp.Id);

            return View(new EmployeeProfileViewModel
            {
                Employee = emp,
                SalaryStructure = structure,
                TaxSlab = taxSlab,
                LeaveBalance = balance,
                SalarySlips = slips
            });
        }

    }
}
