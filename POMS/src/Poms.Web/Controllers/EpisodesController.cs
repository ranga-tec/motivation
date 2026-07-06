using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

// Pure Patient Record ("Episode" in code, "PatientRecord" per the PRD) CRUD.
// Clinical content (assessments/prescriptions/fittings/deliveries/follow-ups) lives in
// their own dedicated controllers, reachable from Details.
[Authorize(Policy = "ClinicianOrAdmin")]
public class EpisodesController : Controller
{
    private readonly PomsDbContext _context;
    private readonly ILogger<EpisodesController> _logger;

    public EpisodesController(PomsDbContext context, ILogger<EpisodesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Episodes
    public async Task<IActionResult> Index(string searchString, RecordStatus? status, int? centerId, int page = 1)
    {
        var query = _context.Episodes
            .Include(e => e.Patient)
            .Include(e => e.Center)
            .AsQueryable();

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

        var model = new EpisodeViewModel
        {
            Id = episode.Id,
            PatientId = episode.PatientId,
            CenterId = episode.CenterId,
            Status = episode.Status,
            RecordDate = episode.RecordDate,
            Remarks = episode.Remarks,
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

                episode.CenterId = model.CenterId;
                episode.Status = model.Status;
                episode.RecordDate = model.RecordDate;
                episode.Remarks = model.Remarks;
                episode.UpdatedBy = User.Identity?.Name;
                episode.UpdatedAt = DateTime.UtcNow;

                _context.Update(episode);
                await _context.SaveChangesAsync();

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
