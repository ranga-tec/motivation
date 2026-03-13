using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        PomsDbContext context,
        IDashboardService dashboardService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    // GET: Admin
    public async Task<IActionResult> Index()
    {
        var stats = new AdminDashboardViewModel
        {
            TotalUsers = await _context.Users.CountAsync(),
            ActiveUsers = await _context.Users.OfType<ApplicationUser>().CountAsync(u => u.IsActive),
            TotalPatients = await _context.Patients.CountAsync(),
            TotalEpisodes = await _context.Episodes.CountAsync(),
            TotalCenters = await _context.Centers.CountAsync(),
            RecentAuditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToListAsync()
        };

        return View(stats);
    }

    // GET: Admin/Settings
    public async Task<IActionResult> Settings()
    {
        var settings = await _context.SystemSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();

        return View(settings);
    }

    // POST: Admin/Settings/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSetting(int id, string value)
    {
        var setting = await _context.SystemSettings.FindAsync(id);
        if (setting == null) return NotFound();

        setting.Value = value;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Setting updated successfully";
        return RedirectToAction(nameof(Settings));
    }

    // GET: Admin/AuditLogs
    public async Task<IActionResult> AuditLogs(string entityType, string action, int page = 1)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        var pageSize = 50;
        var totalCount = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.EntityType = entityType;
        ViewBag.Action = action;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Populate dropdowns
        ViewBag.EntityTypes = await _context.AuditLogs
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        ViewBag.Actions = new[] { "Create", "Update", "Delete" };

        return View(logs);
    }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalPatients { get; set; }
    public int TotalEpisodes { get; set; }
    public int TotalCenters { get; set; }
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}
