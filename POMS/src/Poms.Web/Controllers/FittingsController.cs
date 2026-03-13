using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "ClinicianOrAdmin")]
public class FittingsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly ILogger<FittingsController> _logger;

    public FittingsController(PomsDbContext context, ILogger<FittingsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Fittings
    public async Task<IActionResult> Index(Guid? episodeId, FittingStatus? status, int page = 1)
    {
        var query = _context.Fittings
            .Include(f => f.Episode)
            .ThenInclude(e => e.Patient)
            .AsQueryable();

        if (episodeId.HasValue)
            query = query.Where(f => f.EpisodeId == episodeId.Value);

        if (status.HasValue)
            query = query.Where(f => f.Status == status.Value);

        var pageSize = 20;
        var totalCount = await query.CountAsync();
        var fittings = await query
            .OrderByDescending(f => f.FittingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.EpisodeId = episodeId;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.StatusOptions = new SelectList(Enum.GetValues<FittingStatus>());

        return View(fittings);
    }

    // GET: Fittings/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();

        var fitting = await _context.Fittings
            .Include(f => f.Episode)
            .ThenInclude(e => e.Patient)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fitting == null) return NotFound();

        return View(fitting);
    }

    // GET: Fittings/Create
    public async Task<IActionResult> Create(Guid episodeId)
    {
        var episode = await _context.Episodes
            .Include(e => e.Patient)
            .Include(e => e.Fittings)
            .FirstOrDefaultAsync(e => e.Id == episodeId);

        if (episode == null) return NotFound();

        var nextFittingNumber = episode.Fittings.Any()
            ? episode.Fittings.Max(f => f.FittingNumber) + 1
            : 1;

        var model = new FittingViewModel
        {
            EpisodeId = episodeId,
            FittingNumber = nextFittingNumber,
            FittingDate = DateOnly.FromDateTime(DateTime.Today),
            Status = FittingStatus.Scheduled,
            PatientName = $"{episode.Patient.FirstName} {episode.Patient.LastName}",
            PatientNumber = episode.Patient.PatientNumber
        };

        ViewBag.StatusOptions = new SelectList(Enum.GetValues<FittingStatus>());
        return View(model);
    }

    // POST: Fittings/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FittingViewModel model)
    {
        if (ModelState.IsValid)
        {
            var fitting = new Fitting
            {
                EpisodeId = model.EpisodeId,
                FittingNumber = model.FittingNumber,
                FittingDate = model.FittingDate,
                Status = model.Status,
                Notes = model.Notes,
                Adjustments = model.Adjustments,
                PatientFeedback = model.PatientFeedback,
                NextSteps = model.NextSteps,
                NextFittingDate = model.NextFittingDate,
                PerformedBy = model.PerformedBy ?? User.Identity?.Name,
                Remarks = model.Remarks,
                CreatedBy = User.Identity?.Name
            };

            _context.Add(fitting);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Fitting #{Number} created for Episode {EpisodeId}",
                fitting.FittingNumber, fitting.EpisodeId);

            TempData["Success"] = $"Fitting #{fitting.FittingNumber} created successfully";
            return RedirectToAction("Details", "Episodes", new { id = model.EpisodeId });
        }

        ViewBag.StatusOptions = new SelectList(Enum.GetValues<FittingStatus>());
        return View(model);
    }

    // GET: Fittings/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var fitting = await _context.Fittings
            .Include(f => f.Episode)
            .ThenInclude(e => e.Patient)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fitting == null) return NotFound();

        var model = new FittingViewModel
        {
            Id = fitting.Id,
            EpisodeId = fitting.EpisodeId,
            FittingNumber = fitting.FittingNumber,
            FittingDate = fitting.FittingDate,
            Status = fitting.Status,
            Notes = fitting.Notes,
            Adjustments = fitting.Adjustments,
            PatientFeedback = fitting.PatientFeedback,
            NextSteps = fitting.NextSteps,
            NextFittingDate = fitting.NextFittingDate,
            PerformedBy = fitting.PerformedBy,
            Remarks = fitting.Remarks,
            PatientName = $"{fitting.Episode.Patient.FirstName} {fitting.Episode.Patient.LastName}",
            PatientNumber = fitting.Episode.Patient.PatientNumber
        };

        ViewBag.StatusOptions = new SelectList(Enum.GetValues<FittingStatus>());
        return View(model);
    }

    // POST: Fittings/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FittingViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var fitting = await _context.Fittings.FindAsync(id);
            if (fitting == null) return NotFound();

            fitting.FittingDate = model.FittingDate;
            fitting.Status = model.Status;
            fitting.Notes = model.Notes;
            fitting.Adjustments = model.Adjustments;
            fitting.PatientFeedback = model.PatientFeedback;
            fitting.NextSteps = model.NextSteps;
            fitting.NextFittingDate = model.NextFittingDate;
            fitting.PerformedBy = model.PerformedBy;
            fitting.Remarks = model.Remarks;
            fitting.UpdatedBy = User.Identity?.Name;
            fitting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Fitting updated successfully";
            return RedirectToAction(nameof(Details), new { id = fitting.Id });
        }

        ViewBag.StatusOptions = new SelectList(Enum.GetValues<FittingStatus>());
        return View(model);
    }

    // POST: Fittings/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, FittingStatus status)
    {
        var fitting = await _context.Fittings.FindAsync(id);
        if (fitting == null) return NotFound();

        fitting.Status = status;
        fitting.UpdatedBy = User.Identity?.Name;
        fitting.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Fitting status updated to {status}";
        return RedirectToAction(nameof(Details), new { id });
    }

    // AJAX: Get fitting details (for modal)
    [HttpGet]
    public async Task<IActionResult> GetFittingDetails(Guid id)
    {
        var fitting = await _context.Fittings
            .Include(f => f.Episode)
            .ThenInclude(e => e.Patient)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fitting == null) return NotFound();

        return PartialView("_FittingDetails", fitting);
    }
}

public class FittingViewModel
{
    public Guid Id { get; set; }

    [Required]
    public Guid EpisodeId { get; set; }

    [Display(Name = "Fitting #")]
    public int FittingNumber { get; set; }

    [Required]
    [Display(Name = "Fitting Date")]
    [DataType(DataType.Date)]
    public DateOnly FittingDate { get; set; }

    [Required]
    [Display(Name = "Status")]
    public FittingStatus Status { get; set; }

    [Display(Name = "Notes")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [Display(Name = "Adjustments Made")]
    [DataType(DataType.MultilineText)]
    public string? Adjustments { get; set; }

    [Display(Name = "Patient Feedback")]
    [DataType(DataType.MultilineText)]
    public string? PatientFeedback { get; set; }

    [Display(Name = "Next Steps")]
    [DataType(DataType.MultilineText)]
    public string? NextSteps { get; set; }

    [Display(Name = "Next Fitting Date")]
    [DataType(DataType.Date)]
    public DateOnly? NextFittingDate { get; set; }

    [Display(Name = "Performed By")]
    public string? PerformedBy { get; set; }

    [Display(Name = "Remarks")]
    [DataType(DataType.MultilineText)]
    public string? Remarks { get; set; }

    // Display only
    public string? PatientName { get; set; }
    public string? PatientNumber { get; set; }
}
