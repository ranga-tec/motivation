using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class DeviceCatalogController : Controller
{
    private readonly PomsDbContext _context;

    public DeviceCatalogController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: DeviceCatalog
    public async Task<IActionResult> Index(int? deviceTypeId)
    {
        var query = _context.DeviceCatalogs
            .Include(d => d.DeviceType)
            .AsQueryable();

        if (deviceTypeId.HasValue)
            query = query.Where(d => d.DeviceTypeId == deviceTypeId.Value);

        var devices = await query.OrderBy(d => d.DeviceType.Name).ThenBy(d => d.Name).ToListAsync();

        ViewBag.DeviceTypeId = deviceTypeId;
        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(devices);
    }

    // GET: DeviceCatalog/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(new DeviceCatalogViewModel());
    }

    // POST: DeviceCatalog/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeviceCatalogViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.DeviceCatalogs.AnyAsync(d => d.Code == model.Code))
            {
                ModelState.AddModelError("Code", "Device code already exists");
                ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
                return View(model);
            }

            var device = new DeviceCatalog
            {
                DeviceTypeId = model.DeviceTypeId,
                Code = model.Code,
                Name = model.Name,
                DefaultComponentsJson = model.DefaultComponentsJson,
                IsActive = model.IsActive
            };

            _context.DeviceCatalogs.Add(device);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Device created successfully";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // GET: DeviceCatalog/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var device = await _context.DeviceCatalogs.FindAsync(id);
        if (device == null) return NotFound();

        var model = new DeviceCatalogViewModel
        {
            Id = device.Id,
            DeviceTypeId = device.DeviceTypeId,
            Code = device.Code,
            Name = device.Name,
            DefaultComponentsJson = device.DefaultComponentsJson,
            IsActive = device.IsActive
        };

        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // POST: DeviceCatalog/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DeviceCatalogViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var device = await _context.DeviceCatalogs.FindAsync(id);
            if (device == null) return NotFound();

            if (await _context.DeviceCatalogs.AnyAsync(d => d.Code == model.Code && d.Id != id))
            {
                ModelState.AddModelError("Code", "Device code already exists");
                ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
                return View(model);
            }

            device.DeviceTypeId = model.DeviceTypeId;
            device.Code = model.Code;
            device.Name = model.Name;
            device.DefaultComponentsJson = model.DefaultComponentsJson;
            device.IsActive = model.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Device updated successfully";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.DeviceTypes = new SelectList(await _context.DeviceTypes.ToListAsync(), "Id", "Name");
        return View(model);
    }

    // POST: DeviceCatalog/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var device = await _context.DeviceCatalogs.FindAsync(id);
        if (device == null) return NotFound();

        device.IsActive = !device.IsActive;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Device {(device.IsActive ? "activated" : "deactivated")}";
        return RedirectToAction(nameof(Index));
    }
}

public class DeviceCatalogViewModel
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

    [Display(Name = "Default Components (JSON)")]
    [DataType(DataType.MultilineText)]
    public string? DefaultComponentsJson { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
