using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class ComponentCatalogController : Controller
{
    private readonly PomsDbContext _context;

    public ComponentCatalogController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: ComponentCatalog
    public async Task<IActionResult> Index(int? deviceTypeId)
    {
        var query = _context.ComponentCatalogs
            .Include(c => c.DeviceType)
            .AsQueryable();

        if (deviceTypeId.HasValue)
            query = query.Where(c => c.DeviceTypeId == deviceTypeId.Value);

        var components = await query.OrderBy(c => c.DeviceType.Name).ThenBy(c => c.Name).ToListAsync();

        ViewBag.DeviceTypeId = deviceTypeId;
        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(components);
    }

    // GET: ComponentCatalog/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(new ComponentCatalogViewModel());
    }

    // POST: ComponentCatalog/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComponentCatalogViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.ComponentCatalogs.AnyAsync(c => c.Code == model.Code))
            {
                ModelState.AddModelError("Code", "Component code already exists");
                ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
                return View(model);
            }

            var component = new ComponentCatalog
            {
                DeviceTypeId = model.DeviceTypeId,
                Code = model.Code,
                Name = model.Name,
                Category = model.Category,
                IsActive = model.IsActive
            };

            _context.ComponentCatalogs.Add(component);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Component created successfully";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // GET: ComponentCatalog/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var component = await _context.ComponentCatalogs.FindAsync(id);
        if (component == null) return NotFound();

        var model = new ComponentCatalogViewModel
        {
            Id = component.Id,
            DeviceTypeId = component.DeviceTypeId,
            Code = component.Code,
            Name = component.Name,
            Category = component.Category,
            IsActive = component.IsActive
        };

        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // POST: ComponentCatalog/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ComponentCatalogViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var component = await _context.ComponentCatalogs.FindAsync(id);
            if (component == null) return NotFound();

            if (await _context.ComponentCatalogs.AnyAsync(c => c.Code == model.Code && c.Id != id))
            {
                ModelState.AddModelError("Code", "Component code already exists");
                ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
                return View(model);
            }

            component.DeviceTypeId = model.DeviceTypeId;
            component.Code = model.Code;
            component.Name = model.Name;
            component.Category = model.Category;
            component.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Component updated successfully";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // POST: ComponentCatalog/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var component = await _context.ComponentCatalogs.FindAsync(id);
        if (component == null) return NotFound();

        component.IsActive = !component.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Component {(component.IsActive ? "activated" : "deactivated")}";
        return RedirectToAction(nameof(Index));
    }
}

public class ComponentCatalogViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Device Type")]
    public int DeviceTypeId { get; set; }

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [StringLength(100)]
    public string? Category { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
