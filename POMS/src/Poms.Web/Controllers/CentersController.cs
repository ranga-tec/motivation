using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class CentersController : Controller
{
    private readonly PomsDbContext _context;

    public CentersController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: Centers
    public async Task<IActionResult> Index(int? districtId)
    {
        var query = _context.Centers
            .Include(c => c.District)
            .ThenInclude(d => d.Province)
            .AsQueryable();

        if (districtId.HasValue)
            query = query.Where(c => c.DistrictId == districtId.Value);

        var centers = await query.OrderBy(c => c.District.Name).ThenBy(c => c.Name).ToListAsync();

        ViewBag.DistrictId = districtId;
        ViewBag.Districts = new SelectList(
            await _context.Districts.Include(d => d.Province).ToListAsync(),
            "Id", "Name", null, "Province.Name");
        return View(centers);
    }

    // GET: Centers/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new CenterViewModel());
    }

    // POST: Centers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CenterViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Centers.AnyAsync(c => c.Code == model.Code))
            {
                ModelState.AddModelError("Code", "Center code already exists");
                await PopulateDropdowns();
                return View(model);
            }

            var center = new Center
            {
                DistrictId = model.DistrictId,
                Code = model.Code,
                Name = model.Name,
                Address = model.Address,
                Phone = model.Phone,
                IsActive = model.IsActive
            };

            _context.Centers.Add(center);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Center created successfully";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Centers/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var center = await _context.Centers.FindAsync(id);
        if (center == null) return NotFound();

        var model = new CenterViewModel
        {
            Id = center.Id,
            DistrictId = center.DistrictId,
            Code = center.Code,
            Name = center.Name,
            Address = center.Address,
            Phone = center.Phone,
            IsActive = center.IsActive
        };

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Centers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CenterViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var center = await _context.Centers.FindAsync(id);
            if (center == null) return NotFound();

            if (await _context.Centers.AnyAsync(c => c.Code == model.Code && c.Id != id))
            {
                ModelState.AddModelError("Code", "Center code already exists");
                await PopulateDropdowns();
                return View(model);
            }

            center.DistrictId = model.DistrictId;
            center.Code = model.Code;
            center.Name = model.Name;
            center.Address = model.Address;
            center.Phone = model.Phone;
            center.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Center updated successfully";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Centers/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var center = await _context.Centers.FindAsync(id);
        if (center == null) return NotFound();

        center.IsActive = !center.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Center {(center.IsActive ? "activated" : "deactivated")}";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Districts = new SelectList(
            await _context.Districts.Where(d => d.IsActive).Include(d => d.Province).ToListAsync(),
            "Id", "Name", null, "Province.Name");
    }
}

public class CenterViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "District")]
    public int DistrictId { get; set; }

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string? Address { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
