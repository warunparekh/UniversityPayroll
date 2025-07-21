using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using UniversityPayroll.Data;
using UniversityPayroll.Models;

[Authorize(Policy = "AdminOnly")]
public class LeaveEntitlementController : Controller
{
    private readonly LeaveEntitlementRepository _repo;
    private readonly LeaveTypeRepository _typeRepo;

    public LeaveEntitlementController(LeaveEntitlementRepository repo, LeaveTypeRepository typeRepo)
    {
        _repo = repo;
        _typeRepo = typeRepo;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _repo.GetAllAsync();
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Designations = Designations.All.Select(d => new { Value = d, Text = d }).ToList();
        ViewBag.LeaveTypes = await _typeRepo.GetAllAsync();
        return View(new LeaveEntitlement { Entitlements = new System.Collections.Generic.Dictionary<string, int>() });
    }

    [HttpPost]
    public async Task<IActionResult> Create(LeaveEntitlement model)
    {
        var leaveTypes = (await _typeRepo.GetAllAsync()).Select(t => t.Name).ToHashSet();
        model.Entitlements = model.Entitlements
            .Where(kv => leaveTypes.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        await _repo.CreateAsync(model);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var item = await _repo.GetByIdAsync(id);
        if (item == null) return NotFound();
        ViewBag.Designations = Designations.All.Select(d => new { Value = d, Text = d }).ToList();
        ViewBag.LeaveTypes = await _typeRepo.GetAllAsync();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(LeaveEntitlement model)
    {
        var leaveTypes = (await _typeRepo.GetAllAsync()).Select(t => t.Name).ToHashSet();
        model.Entitlements = model.Entitlements
            .Where(kv => leaveTypes.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        await _repo.UpdateAsync(model);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        await _repo.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}