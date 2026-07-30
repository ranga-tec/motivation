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

// Pure Patient Record ("Episode" in code, "PatientRecord" per the PRD) CRUD.
// Clinical content (assessments/prescriptions/fittings/deliveries/follow-ups) lives in
// their own dedicated controllers, reachable from Details.
[Authorize(Policy = "ClinicianOrAdmin")]
public class EpisodesController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IRestrictedAccessService _restrictedAccess;
    private readonly ILogger<EpisodesController> _logger;

    public EpisodesController(
        PomsDbContext context,
        IRestrictedAccessService restrictedAccess,
        ILogger<EpisodesController> logger)
    {
        _context = context;
        _restrictedAccess = restrictedAccess;
        _logger = logger;
    }

    // GET: Episodes
    public async Task<IActionResult> Index(string searchString, RecordStatus? status, int? centerId, int page = 1)
    {
        var query = _context.Episodes
            .Include(e => e.Patient)
            .Include(e => e.Center)
            .AsQueryable();
        var access = await _restrictedAccess.GetScopeAsync(User);
        query = access.Filter(query);

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(e =>
                e.Patient.PatientNumber.Contains(searchString) ||
                e.Patient.FullName.Contains(searchString));
        }

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        if (centerId.HasValue)
            query = query.Where(e => e.CenterId == centerId.Value);

        var pageSize = 20;
        var totalCount = await query.CountAsync();
        var episodes = await query
            .OrderByDescending(e => e.RecordDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.Status = status;
        ViewBag.CenterId = centerId;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Centers = new SelectList(await _context.Centers.ToListAsync(), "Id", "Name");

        return View(episodes);
    }

    // GET: Episodes/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();

        var episode = await _context.Episodes
            .Include(e => e.Patient)
            .Include(e => e.Center)
            .Include(e => e.Assessments).ThenInclude(a => a.Prescriptions)
            .Include(e => e.Assessments).ThenInclude(a => a.MainProblemType)
            .Include(e => e.Assessments).ThenInclude(a => a.CauseReasonType)
            .Include(e => e.Fittings)
            .Include(e => e.Deliveries)
            .Include(e => e.FollowUps)
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (episode == null) return NotFound();

        var access = await _restrictedAccess.GetScopeAsync(User);
        var episodeAllowed = access.CanAccess(episode.IsRestricted, episode.CreatedBy);
        if (!episodeAllowed)
        {
            await _restrictedAccess.AuditAsync(
                access, "ViewDenied", nameof(Episode), episode.Id, true, false);
            return NotFound();
        }

        episode.Assessments = episode.Assessments
            .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
            .ToList();
        episode.Fittings = episode.Fittings
            .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
            .ToList();
        episode.Deliveries = episode.Deliveries
            .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
            .ToList();
        episode.FollowUps = episode.FollowUps
            .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
            .ToList();
        episode.Documents = episode.Documents
            .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
            .ToList();

        var restrictedItemsShown =
            (episode.IsRestricted ? 1 : 0) +
            episode.Assessments.Count(record => record.IsRestricted) +
            episode.Fittings.Count(record => record.IsRestricted) +
            episode.Deliveries.Count(record => record.IsRestricted) +
            episode.FollowUps.Count(record => record.IsRestricted) +
            episode.Documents.Count(record => record.IsRestricted);
        await _restrictedAccess.AuditAsync(
            access,
            "View",
            nameof(Episode),
            episode.Id,
            restrictedItemsShown > 0,
            true,
            new { RestrictedItemsShown = restrictedItemsShown });
        if (restrictedItemsShown > 0)
            Response.Headers.CacheControl = "no-store, private";

        return View(episode);
    }

    // GET: Episodes/Create
    public async Task<IActionResult> Create(Guid? patientId)
    {
        var viewModel = new EpisodeViewModel();

        if (patientId.HasValue)
        {
            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient != null)
            {
                viewModel.PatientId = patient.Id;
                viewModel.PatientNumber = patient.PatientNumber;
                viewModel.PatientName = patient.FullName;
                viewModel.CenterId = patient.CenterId;
            }
        }

        await PopulateDropdowns();
        return View(viewModel);
    }

    // POST: Episodes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EpisodeViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var episode = new Episode
                {
                    PatientId = model.PatientId,
                    CenterId = model.CenterId,
                    Status = model.Status,
                    RecordDate = model.RecordDate,
                    Remarks = model.Remarks,
                    IsRestricted = model.IsRestricted,
                    CreatedBy = User.Identity?.Name
                };

                _context.Add(episode);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Record {EpisodeId} created for patient {PatientId} by {User}",
                    episode.Id, model.PatientId, User.Identity?.Name);

                TempData["Success"] = "Record created successfully!";
                return RedirectToAction(nameof(Details), new { id = episode.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating record");
                ModelState.AddModelError("", "An error occurred while creating the record.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Episodes/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == id);
        if (episode == null) return NotFound();

        var access = await _restrictedAccess.GetScopeAsync(User);
        if (!access.CanAccess(episode.IsRestricted, episode.CreatedBy))
        {
            await _restrictedAccess.AuditAsync(
                access, "EditDenied", nameof(Episode), episode.Id, true, false);
            return NotFound();
        }
        await _restrictedAccess.AuditAsync(
            access, "EditView", nameof(Episode), episode.Id,
            episode.IsRestricted, true);
        if (episode.IsRestricted)
            Response.Headers.CacheControl = "no-store, private";

        var model = new EpisodeViewModel
        {
            Id = episode.Id,
            PatientId = episode.PatientId,
            CenterId = episode.CenterId,
            Status = episode.Status,
            RecordDate = episode.RecordDate,
            Remarks = episode.Remarks,
            IsRestricted = episode.IsRestricted,
            PatientNumber = episode.Patient.PatientNumber,
            PatientName = episode.Patient.FullName
        };

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Episodes/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EpisodeViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var episode = await _context.Episodes.FirstOrDefaultAsync(e => e.Id == id);
                if (episode == null) return NotFound();

                var access = await _restrictedAccess.GetScopeAsync(User);
                if (!access.CanAccess(episode.IsRestricted, episode.CreatedBy))
                {
                    await _restrictedAccess.AuditAsync(
                        access, "EditDenied", nameof(Episode), episode.Id, true, false);
                    return NotFound();
                }

                var wasRestricted = episode.IsRestricted;
                episode.CenterId = model.CenterId;
                episode.Status = model.Status;
                episode.RecordDate = model.RecordDate;
                episode.Remarks = model.Remarks;
                episode.IsRestricted = model.IsRestricted;
                episode.UpdatedBy = User.Identity?.Name;
                episode.UpdatedAt = DateTime.UtcNow;

                _context.Update(episode);
                await _context.SaveChangesAsync();
                await _restrictedAccess.AuditAsync(
                    access,
                    "Update",
                    nameof(Episode),
                    episode.Id,
                    wasRestricted || episode.IsRestricted,
                    true,
                    new { episode.IsRestricted });

                TempData["Success"] = "Record updated successfully!";
                return RedirectToAction(nameof(Details), new { id = episode.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating record {EpisodeId}", id);
                ModelState.AddModelError("", "An error occurred while updating the record.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Patients = new SelectList(
            await _context.Patients
                .Select(p => new { p.Id, DisplayName = p.PatientNumber + " - " + p.FullName })
                .ToListAsync(),
            "Id", "DisplayName");

        ViewBag.Centers = new SelectList(await _context.Centers.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
        ViewBag.StatusOptions = new SelectList(Enum.GetValues(typeof(RecordStatus)).Cast<RecordStatus>());
    }
}
