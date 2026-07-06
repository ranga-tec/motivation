using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "ClinicianOrAdmin")]
public class FollowUpsController : Controller
{
    private readonly PomsDbContext _context;

    public FollowUpsController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: FollowUps/QuickAdd?patientId= - Dashboard Quick Action entry point (PRD 5.4)
    public async Task<IActionResult> QuickAdd(Guid patientId)
    {
        var episodeId = await ResolveTargetEpisodeAsync(patientId);
        if (episodeId == null) return RedirectToAction("Folder", "Patients", new { id = patientId });
        return RedirectToAction(nameof(Create), new { episodeId = episodeId.Value });
    }

    // GET: FollowUps/Create?episodeId=
    public async Task<IActionResult> Create(Guid episodeId)
    {
        var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == episodeId);
        if (episode == null) return NotFound();

        return View(new FollowUpViewModel
        {
            EpisodeId = episodeId,
            PatientNumber = episode.Patient.PatientNumber,
            PatientName = episode.Patient.FullName
        });
    }

    // POST: FollowUps/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpViewModel model)
    {
        if (ModelState.IsValid)
        {
            var followUp = new FollowUp
            {
                EpisodeId = model.EpisodeId,
                FollowUpDate = model.FollowUpDate,
                Notes = model.Notes,
                CreatedBy = User.Identity?.Name
            };
            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Follow-up added.";
            return RedirectToAction("Details", "Episodes", new { id = model.EpisodeId });
        }

        return View(model);
    }

    // GET: FollowUps/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var followUp = await _context.FollowUps.Include(f => f.Episode).ThenInclude(e => e.Patient).FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null) return NotFound();

        return View(new FollowUpViewModel
        {
            Id = followUp.Id,
            EpisodeId = followUp.EpisodeId,
            PatientNumber = followUp.Episode.Patient.PatientNumber,
            PatientName = followUp.Episode.Patient.FullName,
            FollowUpDate = followUp.FollowUpDate,
            Notes = followUp.Notes
        });
    }

    // POST: FollowUps/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FollowUpViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null) return NotFound();

            followUp.FollowUpDate = model.FollowUpDate;
            followUp.Notes = model.Notes;
            followUp.UpdatedBy = User.Identity?.Name;
            followUp.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Follow-up updated.";
            return RedirectToAction("Details", "Episodes", new { id = followUp.EpisodeId });
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
