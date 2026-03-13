using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class ProvincesController : Controller
{
    private readonly PomsDbContext _context;

    public ProvincesController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: Provinces
    public async Task<IActionResult> Index()
    {
        var provinces = await _context.Provinces
            .Include(p => p.Districts)
            .OrderBy(p => p.Name)
            .ToListAsync();
        return View(provinces);
    }

    // GET: Provinces/Create
    public IActionResult Create()
    {
        return View(new ProvinceViewModel());
    }

    // POST: Provinces/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProvinceViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.Provinces.AnyAsync(p => p.Code == model.Code))
            {
                ModelState.AddModelError("Code", "Province code already exists");
                return View(model);
            }

            var province = new Province
            {
                Code = model.Code,
                Name = model.Name,
                IsActive = model.IsActive
            };

            _context.Provinces.Add(province);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Province created successfully";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: Provinces/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var province = await _context.Provinces.FindAsync(id);
        if (province == null) return NotFound();

        var model = new ProvinceViewModel
        {
            Id = province.Id,
            Code = province.Code,
            Name = province.Name,
            IsActive = province.IsActive
        };

        return View(model);
    }

    // POST: Provinces/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProvinceViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var province = await _context.Provinces.FindAsync(id);
            if (province == null) return NotFound();

            if (await _context.Provinces.AnyAsync(p => p.Code == model.Code && p.Id != id))
            {
                ModelState.AddModelError("Code", "Province code already exists");
                return View(model);
            }

            province.Code = model.Code;
            province.Name = model.Name;
            province.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Province updated successfully";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // POST: Provinces/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var province = await _context.Provinces.FindAsync(id);
        if (province == null) return NotFound();

        province.IsActive = !province.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Province {(province.IsActive ? "activated" : "deactivated")}";
        return RedirectToAction(nameof(Index));
    }
}

public class ProvinceViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
