using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

[Authorize(Policy = "AdminOnly")]
public class LeaveTypeController : Controller
{
    private readonly LeaveTypeRepository _repo;
    private readonly LeaveEntitlementRepository _entRepo;
    private readonly LeaveBalanceRepository _balanceRepo;

    public LeaveTypeController(LeaveTypeRepository repo, LeaveEntitlementRepository entRepo, LeaveBalanceRepository balanceRepo)
    {
        _repo = repo;
        _entRepo = entRepo;
        _balanceRepo = balanceRepo;
    }

    public async Task<IActionResult> Index()
    {
        var types = await _repo.GetAllAsync();
        var entitlements = await _entRepo.GetAllAsync();
        var balances = await _balanceRepo.GetAllAsync();
        var inUse = types.ToDictionary(
            t => t.Id,
            t => entitlements.Any(e => e.Entitlements.ContainsKey(t.Name)) ||
                 balances.Any(b => b.Entitlements.ContainsKey(t.Name))
        );
        ViewBag.InUse = inUse;
        return View(types);
    }

    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(LeaveType model)
    {
        await _repo.CreateAsync(model);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(LeaveType model)
    {
        await _repo.UpdateAsync(model);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var type = await _repo.GetByIdAsync(id);
        if (type == null) return RedirectToAction(nameof(Index));
        var entitlements = await _entRepo.GetAllAsync();
        var balances = await _balanceRepo.GetAllAsync();
        bool inUse = entitlements.Any(e => e.Entitlements.ContainsKey(type.Name)) ||
                     balances.Any(b => b.Entitlements.ContainsKey(type.Name));
        if (inUse)
            return RedirectToAction(nameof(Index));
        await _repo.DeleteAsync(id);
        await _entRepo.RemoveLeaveTypeFromAll(type.Name);
        await _balanceRepo.RemoveLeaveTypeFromAll(type.Name);
        return RedirectToAction(nameof(Index));
    }
}