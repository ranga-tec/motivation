using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class DeviceTypesController : Controller
{
    private readonly PomsDbContext _context;

    public DeviceTypesController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: DeviceTypes
    public async Task<IActionResult> Index()
    {
        var deviceTypes = await _context.DeviceTypes.OrderBy(d => d.Name).ToListAsync();
        return View(deviceTypes);
    }

    // GET: DeviceTypes/Create
    public IActionResult Create()
    {
        return View(new DeviceTypeViewModel());
    }

    // POST: DeviceTypes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeviceTypeViewModel model)
    {
        if (ModelState.IsValid)
        {
            if (await _context.DeviceTypes.AnyAsync(d => d.Code == model.Code))
            {
                ModelState.AddModelError("Code", "Device type code already exists");
                return View(model);
            }

            var deviceType = new DeviceType
            {
                Code = model.Code,
                Name = model.Name
            };

            _context.DeviceTypes.Add(deviceType);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Device type created successfully";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: DeviceTypes/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var deviceType = await _context.DeviceTypes.FindAsync(id);
        if (deviceType == null) return NotFound();

        var model = new DeviceTypeViewModel
        {
            Id = deviceType.Id,
            Code = deviceType.Code,
            Name = deviceType.Name
        };

        return View(model);
    }

    // POST: DeviceTypes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DeviceTypeViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var deviceType = await _context.DeviceTypes.FindAsync(id);
            if (deviceType == null) return NotFound();

            if (await _context.DeviceTypes.AnyAsync(d => d.Code == model.Code && d.Id != id))
            {
                ModelState.AddModelError("Code", "Device type code already exists");
                return View(model);
            }

            deviceType.Code = model.Code;
            deviceType.Name = model.Name;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Device type updated successfully";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }
}

public class DeviceTypeViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";
}
