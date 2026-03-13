using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class DistrictsController : Controller
{
    private readonly PomsDbContext _context;

    public DistrictsController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: Districts
    public async Task<IActionResult> Index(int? provinceId)
    {
        var query = _context.Districts
            .Include(d => d.Province)
            .Include(d => d.Centers)
            .AsQueryable();

        if (provinceId.HasValue)
            query = query.Where(d => d.ProvinceId == provinceId.Value);

        var districts = await query.OrderBy(d => d.Province.Name).ThenBy(d => d.Name).ToListAsync();

        ViewBag.ProvinceId = provinceId;
        ViewBag.Provinces = new SelectList(await _context.Provinces.ToListAsync(), "Id", "Name");
        return View(districts);
    }

    // GET: Districts/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Provinces = new SelectList(await _context.Provinces.Where(p => p.IsActive).ToListAsync(), "Id", "Name");
        return View(new DistrictViewModel());
    }

    // POST: Districts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DistrictViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Districts.AnyAsync(d => d.Code == model.Code))
            {
                ModelState.AddModelError("Code", "District code already exists");
                ViewBag.Provinces = new SelectList(await _context.Provinces.Where(p => p.IsActive).ToListAsync(), "Id", "Name");
                return View(model);
            }

            var district = new District
            {
                ProvinceId = model.ProvinceId,
                Code = model.Code,
                Name = model.Name,
                IsActive = model.IsActive
            };

            _context.Districts.Add(district);
            await _context.SaveChangesAsync();

            TempData["Success"] = "District created successfully";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Provinces = new SelectList(await _context.Provinces.Where(p => p.IsActive).ToListAsync(), "Id", "Name");
        return View(model);
    }

    // GET: Districts/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var district = await _context.Districts.FindAsync(id);
        if (district == null) return NotFound();

        var model = new DistrictViewModel
        {
            Id = district.Id,
            ProvinceId = district.ProvinceId,
            Code = district.Code,
            Name = district.Name,
            IsActive = district.IsActive
        };

        ViewBag.Provinces = new SelectList(await _context.Provinces.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // POST: Districts/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DistrictViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var district = await _context.Districts.FindAsync(id);
            if (district == null) return NotFound();

            if (await _context.Districts.AnyAsync(d => d.Code == model.Code && d.Id != id))
            {
                ModelState.AddModelError("Code", "District code already exists");
                ViewBag.Provinces = new SelectList(await _context.Provinces.ToListAsync(), "Id", "Name");
                return View(model);
            }

            district.ProvinceId = model.ProvinceId;
            district.Code = model.Code;
            district.Name = model.Name;
            district.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "District updated successfully";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Provinces = new SelectList(await _context.Provinces.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // POST: Districts/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var district = await _context.Districts.FindAsync(id);
        if (district == null) return NotFound();

        district.IsActive = !district.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"District {(district.IsActive ? "activated" : "deactivated")}";
        return RedirectToAction(nameof(Index));
    }
}

public class DistrictViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Province")]
    public int ProvinceId { get; set; }

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
