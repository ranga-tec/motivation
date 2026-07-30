using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "ClinicianOrAdmin")]
public class DeliveriesController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IRestrictedAccessService _restrictedAccess;

    public DeliveriesController(
        PomsDbContext context,
        IFileStorageService fileStorageService,
        IRestrictedAccessService restrictedAccess)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _restrictedAccess = restrictedAccess;
    }

    // GET: Deliveries/QuickAdd?patientId= - Dashboard Quick Action entry point (PRD 5.4)
    public async Task<IActionResult> QuickAdd(Guid patientId)
    {
        var episodeId = await ResolveTargetEpisodeAsync(patientId);
        if (episodeId == null) return RedirectToAction("Folder", "Patients", new { id = patientId });
        return RedirectToAction(nameof(Create), new { episodeId = episodeId.Value });
    }

    // GET: Deliveries/Create?episodeId=
    public async Task<IActionResult> Create(Guid episodeId)
    {
        var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == episodeId);
        if (episode == null) return NotFound();
        var access = await _restrictedAccess.GetScopeAsync(User);
        if (!access.CanAccess(episode.IsRestricted, episode.CreatedBy))
            return NotFound();

        await PopulateDropdowns();
        return View(new DeliveryViewModel
        {
            EpisodeId = episodeId,
            PatientNumber = episode.Patient.PatientNumber,
            PatientName = episode.Patient.FullName
        });
    }

    // POST: Deliveries/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeliveryViewModel model)
    {
        if (ModelState.IsValid)
        {
            var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == model.EpisodeId);
            if (episode == null) return NotFound();
            var access = await _restrictedAccess.GetScopeAsync(User);
            if (!access.CanAccess(episode.IsRestricted, episode.CreatedBy))
                return NotFound();

            var delivery = new Delivery
            {
                EpisodeId = model.EpisodeId,
                DeliveryDate = model.DeliveryDate,
                DeliveryTime = model.DeliveryTime,
                Notes = model.Notes,
                DeviceId = model.DeviceId,
                IsRestricted = model.IsRestricted,
                CreatedBy = User.Identity?.Name
            };
            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var (storagePath, fileName) = await _fileStorageService.SaveFileAsync(model.Attachment, episode.Patient.PatientNumber);
                _context.EpisodeDocuments.Add(new EpisodeDocument
                {
                    EpisodeId = model.EpisodeId,
                    DocumentType = DocumentType.DeliveryConfirmation,
                    FileName = fileName,
                    StoragePath = storagePath,
                    ContentType = model.Attachment.ContentType,
                    FileSize = model.Attachment.Length,
                    UploadedBy = User.Identity?.Name ?? "",
                    UploadedAt = DateTime.UtcNow,
                    IsRestricted = model.IsRestricted,
                    CreatedBy = User.Identity?.Name
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Delivery recorded.";
            return RedirectToAction("Details", "Episodes", new { id = model.EpisodeId });
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Deliveries/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var delivery = await _context.Deliveries.Include(d => d.Episode).ThenInclude(e => e.Patient).FirstOrDefaultAsync(d => d.Id == id);
        if (delivery == null) return NotFound();
        var access = await _restrictedAccess.GetScopeAsync(User);
        var canAccess = access.CanAccess(delivery.Episode.IsRestricted, delivery.Episode.CreatedBy) &&
            access.CanAccess(delivery.IsRestricted, delivery.CreatedBy);
        if (!canAccess)
        {
            await _restrictedAccess.AuditAsync(
                access, "EditDenied", nameof(Delivery), delivery.Id, true, false);
            return NotFound();
        }
        await _restrictedAccess.AuditAsync(
            access, "EditView", nameof(Delivery), delivery.Id,
            delivery.IsRestricted || delivery.Episode.IsRestricted, true);
        if (delivery.IsRestricted || delivery.Episode.IsRestricted)
            Response.Headers.CacheControl = "no-store, private";

        await PopulateDropdowns();
        return View(new DeliveryViewModel
        {
            Id = delivery.Id,
            EpisodeId = delivery.EpisodeId,
            PatientNumber = delivery.Episode.Patient.PatientNumber,
            PatientName = delivery.Episode.Patient.FullName,
            DeliveryDate = delivery.DeliveryDate,
            DeliveryTime = delivery.DeliveryTime,
            Notes = delivery.Notes,
            DeviceId = delivery.DeviceId,
            IsRestricted = delivery.IsRestricted
        });
    }

    // POST: Deliveries/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DeliveryViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var delivery = await _context.Deliveries
                .Include(record => record.Episode)
                .FirstOrDefaultAsync(record => record.Id == id);
            if (delivery == null) return NotFound();

            var access = await _restrictedAccess.GetScopeAsync(User);
            var canAccess = access.CanAccess(delivery.Episode.IsRestricted, delivery.Episode.CreatedBy) &&
                access.CanAccess(delivery.IsRestricted, delivery.CreatedBy);
            if (!canAccess)
            {
                await _restrictedAccess.AuditAsync(
                    access, "EditDenied", nameof(Delivery), delivery.Id, true, false);
                return NotFound();
            }

            var wasRestricted = delivery.IsRestricted;
            delivery.DeliveryDate = model.DeliveryDate;
            delivery.DeliveryTime = model.DeliveryTime;
            delivery.Notes = model.Notes;
            delivery.DeviceId = model.DeviceId;
            delivery.IsRestricted = model.IsRestricted;
            delivery.UpdatedBy = User.Identity?.Name;
            delivery.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _restrictedAccess.AuditAsync(
                access,
                "Update",
                nameof(Delivery),
                delivery.Id,
                wasRestricted || delivery.IsRestricted || delivery.Episode.IsRestricted,
                true,
                new { delivery.IsRestricted });

            TempData["Success"] = "Delivery updated.";
            return RedirectToAction("Details", "Episodes", new { id = delivery.EpisodeId });
        }

        await PopulateDropdowns();
        return View(model);
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Devices = new SelectList(await _context.DeviceCatalogs.Where(d => d.IsActive).ToListAsync(), "Id", "Name");
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
