using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using UniversityPayroll.Data;
using UniversityPayroll.Models;
using System.Collections.Generic;

namespace UniversityPayroll.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class LeaveTypeController : Controller
    {
        private readonly LeaveTypeRepository _repo;
        private readonly LeaveEntitlementRepository _entRepo;
        private readonly LeaveBalanceRepository _balanceRepo;

        public LeaveTypeController(
            LeaveTypeRepository repo, 
            LeaveEntitlementRepository entRepo, 
            LeaveBalanceRepository balanceRepo)
        {
            _repo = repo;
            _entRepo = entRepo;
            _balanceRepo = balanceRepo;
        }

        #region Helper Methods

        private async Task<Dictionary<string, bool>> GetLeaveTypeUsageStatus()
        {
            var types = await _repo.GetAllAsync();
            var entitlements = await _entRepo.GetAllAsync();
            var balances = await _balanceRepo.GetAllAsync();

            return types.ToDictionary(
                t => t.Id ?? string.Empty,
                t => entitlements.Any(e => e.Entitlements?.ContainsKey(t.Name) == true) ||
                     balances.Any(b => b.Entitlements?.ContainsKey(t.Name) == true)
            );
        }

        private async Task<bool> IsLeaveTypeInUse(string leaveTypeName)
        {
            var entitlements = await _entRepo.GetAllAsync();
            var balances = await _balanceRepo.GetAllAsync();
            
            return entitlements.Any(e => e.Entitlements?.ContainsKey(leaveTypeName) == true) ||
                   balances.Any(b => b.Entitlements?.ContainsKey(leaveTypeName) == true);
        }

        #endregion

        public async Task<IActionResult> Index()
        {
            var types = await _repo.GetAllAsync();
            ViewBag.InUse = await GetLeaveTypeUsageStatus();
            return View(types);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveType model)
        {
            await _repo.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var item = await _repo.GetByIdAsync(id);
            return item == null ? NotFound() : View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeaveType model)
        {
            await _repo.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var type = await _repo.GetByIdAsync(id);
            if (type == null) return RedirectToAction(nameof(Index));

            if (await IsLeaveTypeInUse(type.Name))
                return RedirectToAction(nameof(Index));

            await _repo.DeleteAsync(id);
            await _entRepo.RemoveLeaveTypeFromAll(type.Name);
            await _balanceRepo.RemoveLeaveTypeFromAll(type.Name);
            
            return RedirectToAction(nameof(Index));
        }
    }
}