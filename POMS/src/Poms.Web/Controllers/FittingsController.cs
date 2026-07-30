using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "ClinicianOrAdmin")]
public class FittingsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IRestrictedAccessService _restrictedAccess;

    public FittingsController(PomsDbContext context, IRestrictedAccessService restrictedAccess)
    {
        _context = context;
        _restrictedAccess = restrictedAccess;
    }

    // GET: Fittings/QuickAdd?patientId= - Dashboard Quick Action entry point (PRD 5.4)
    public async Task<IActionResult> QuickAdd(Guid patientId)
    {
        var episodeId = await ResolveTargetEpisodeAsync(patientId);
        if (episodeId == null) return RedirectToAction("Folder", "Patients", new { id = patientId });
        return RedirectToAction(nameof(Create), new { episodeId = episodeId.Value });
    }

    // GET: Fittings/Create?episodeId=
    public async Task<IActionResult> Create(Guid episodeId)
    {
        var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == episodeId);
        if (episode == null) return NotFound();
        var access = await _restrictedAccess.GetScopeAsync(User);
        if (!access.CanAccess(episode.IsRestricted, episode.CreatedBy))
            return NotFound();

        return View(new FittingViewModel
        {
            EpisodeId = episodeId,
            PatientNumber = episode.Patient.PatientNumber,
            PatientName = episode.Patient.FullName
        });
    }

    // POST: Fittings/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FittingViewModel model)
    {
        var episode = await _context.Episodes
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.Id == model.EpisodeId);
        if (episode is null) return NotFound();
        var access = await _restrictedAccess.GetScopeAsync(User);
        if (!access.CanAccess(episode.IsRestricted, episode.CreatedBy))
            return NotFound();

        if (ModelState.IsValid)
        {
            var fitting = new Fitting
            {
                EpisodeId = model.EpisodeId,
                FittingDate = model.FittingDate,
                Notes = model.Notes,
                IsRestricted = model.IsRestricted,
                CreatedBy = User.Identity?.Name
            };
            _context.Fittings.Add(fitting);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Fitting added.";
            return RedirectToAction("Details", "Episodes", new { id = model.EpisodeId });
        }

        return View(model);
    }

    // GET: Fittings/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var fitting = await _context.Fittings.Include(f => f.Episode).ThenInclude(e => e.Patient).FirstOrDefaultAsync(f => f.Id == id);
        if (fitting == null) return NotFound();
        var access = await _restrictedAccess.GetScopeAsync(User);
        var canAccess = access.CanAccess(fitting.Episode.IsRestricted, fitting.Episode.CreatedBy) &&
            access.CanAccess(fitting.IsRestricted, fitting.CreatedBy);
        if (!canAccess)
        {
            await _restrictedAccess.AuditAsync(
                access, "EditDenied", nameof(Fitting), fitting.Id, true, false);
            return NotFound();
        }
        await _restrictedAccess.AuditAsync(
            access, "EditView", nameof(Fitting), fitting.Id,
            fitting.IsRestricted || fitting.Episode.IsRestricted, true);
        if (fitting.IsRestricted || fitting.Episode.IsRestricted)
            Response.Headers.CacheControl = "no-store, private";

        return View(new FittingViewModel
        {
            Id = fitting.Id,
            EpisodeId = fitting.EpisodeId,
            PatientNumber = fitting.Episode.Patient.PatientNumber,
            PatientName = fitting.Episode.Patient.FullName,
            FittingDate = fitting.FittingDate,
            Notes = fitting.Notes,
            IsRestricted = fitting.IsRestricted
        });
    }

    // POST: Fittings/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FittingViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var fitting = await _context.Fittings
                .Include(record => record.Episode)
                .FirstOrDefaultAsync(record => record.Id == id);
            if (fitting == null) return NotFound();

            var access = await _restrictedAccess.GetScopeAsync(User);
            var canAccess = access.CanAccess(fitting.Episode.IsRestricted, fitting.Episode.CreatedBy) &&
                access.CanAccess(fitting.IsRestricted, fitting.CreatedBy);
            if (!canAccess)
            {
                await _restrictedAccess.AuditAsync(
                    access, "EditDenied", nameof(Fitting), fitting.Id, true, false);
                return NotFound();
            }

            var wasRestricted = fitting.IsRestricted;
            fitting.FittingDate = model.FittingDate;
            fitting.Notes = model.Notes;
            fitting.IsRestricted = model.IsRestricted;
            fitting.UpdatedBy = User.Identity?.Name;
            fitting.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _restrictedAccess.AuditAsync(
                access,
                "Update",
                nameof(Fitting),
                fitting.Id,
                wasRestricted || fitting.IsRestricted || fitting.Episode.IsRestricted,
                true,
                new { fitting.IsRestricted });

            TempData["Success"] = "Fitting updated.";
            return RedirectToAction("Details", "Episodes", new { id = fitting.EpisodeId });
        }

        return View(model);
    }

    // Resolves which episode a dashboard Quick Action should land on: the sole active episode if
    // there's exactly one, the most recent episode as a fallback, or none (patient needs an assessment first).
    private async Task<Guid?> ResolveTargetEpisodeAsync(Guid patientId)
    {
        var episodes = await _context.Episodes
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.RecordDate)
            .ToListAsync();
        var access = await _restrictedAccess.GetScopeAsync(User);
        episodes = episodes
            .Where(episode => access.CanAccess(episode.IsRestricted, episode.CreatedBy))
            .ToList();

        var active = episodes.Where(e => e.Status == RecordStatus.Active).ToList();

        if (active.Count == 1) return active[0].Id;

        if (active.Count > 1)
        {
            TempData["Info"] = "This patient has more than one active record — open the correct one below.";
            return null;
        }

        if (episodes.Count > 0) return episodes[0].Id;

        TempData["Error"] = "This patient has no record yet — create an assessment first, then add it from there.";
        return null;
    }
}
