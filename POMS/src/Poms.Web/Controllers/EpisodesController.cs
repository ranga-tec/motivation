using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

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
    public async Task<IActionResult> Index(string searchString, EpisodeType? type, int page = 1)
    {
        var query = _context.Episodes
            .Include(e => e.Patient)
            .Include(e => e.Prosthetic)
            .Include(e => e.Orthotic)
            .Include(e => e.Spinal)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(e =>
                e.Patient.PatientNumber.Contains(searchString) ||
                e.Patient.FirstName.Contains(searchString) ||
                (e.Patient.LastName != null && e.Patient.LastName.Contains(searchString)));
        }

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        var pageSize = 20;
        var totalCount = await query.CountAsync();
        var episodes = await query
            .OrderByDescending(e => e.OpenedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.Type = type;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(episodes);
    }

    // GET: Episodes/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();

        var episode = await _context.Episodes
            .Include(e => e.Patient)
            .Include(e => e.Prosthetic)
            .Include(e => e.Orthotic)
            .Include(e => e.Spinal)
            .Include(e => e.Assessments)
            .Include(e => e.Fittings)
            .Include(e => e.Delivery)
            .Include(e => e.FollowUps)
            .Include(e => e.Repairs)
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
                viewModel.PatientName = $"{patient.FirstName} {patient.LastName}".Trim();
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
                    Type = model.Type,
                    OpenedOn = model.OpenedOn,
                    ClosedOn = model.ClosedOn,
                    Remarks = model.Remarks,
                    CreatedBy = User.Identity?.Name
                };

                _context.Add(episode);
                await _context.SaveChangesAsync();

                // Add type-specific details
                await CreateTypeSpecificDetails(episode, model);

                _logger.LogInformation("Episode {EpisodeId} created for patient {PatientId} by {User}",
                    episode.Id, model.PatientId, User.Identity?.Name);

                TempData["Success"] = "Episode created successfully!";
                return RedirectToAction(nameof(Details), new { id = episode.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating episode");
                ModelState.AddModelError("", "An error occurred while creating the episode.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Episodes/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var episode = await _context.Episodes
            .Include(e => e.Patient)
            .Include(e => e.Prosthetic)
            .Include(e => e.Orthotic)
            .Include(e => e.Spinal)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (episode == null) return NotFound();

        var model = new EpisodeViewModel
        {
            Id = episode.Id,
            PatientId = episode.PatientId,
            Type = episode.Type,
            OpenedOn = episode.OpenedOn,
            ClosedOn = episode.ClosedOn,
            Remarks = episode.Remarks,
            PatientNumber = episode.Patient.PatientNumber,
            PatientName = $"{episode.Patient.FirstName} {episode.Patient.LastName}".Trim()
        };

        // Load type-specific details
        if (episode.Type == EpisodeType.Prosthetic && episode.Prosthetic != null)
        {
            model.AmputationType = episode.Prosthetic.AmputationType;
            model.Level = episode.Prosthetic.Level;
            model.ProstheticSide = episode.Prosthetic.Side;
            model.Reason = episode.Prosthetic.Reason;
        }
        else if (episode.Type == EpisodeType.Orthotic && episode.Orthotic != null)
        {
            model.BodyRegion = episode.Orthotic.BodyRegion;
            model.OrthoticSide = episode.Orthotic.Side;
        }

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
                var episode = await _context.Episodes
                    .Include(e => e.Prosthetic)
                    .Include(e => e.Orthotic)
                    .Include(e => e.Spinal)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (episode == null) return NotFound();

                episode.OpenedOn = model.OpenedOn;
                episode.ClosedOn = model.ClosedOn;
                episode.Remarks = model.Remarks;
                episode.UpdatedBy = User.Identity?.Name;
                episode.UpdatedAt = DateTime.UtcNow;

                // Update type-specific details
                await UpdateTypeSpecificDetails(episode, model);

                _context.Update(episode);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Episode updated successfully!";
                return RedirectToAction(nameof(Details), new { id = episode.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating episode {EpisodeId}", id);
                ModelState.AddModelError("", "An error occurred while updating the episode.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    private async Task CreateTypeSpecificDetails(Episode episode, EpisodeViewModel model)
    {
        switch (model.Type)
        {
            case EpisodeType.Prosthetic:
                var prosthetic = new ProstheticEpisode
                {
                    EpisodeId = episode.Id,
                    AmputationType = model.AmputationType ?? AmputationType.BelowKnee,
                    Level = model.Level ?? "Not Specified",
                    Side = model.ProstheticSide ?? Side.Left,
                    Reason = model.Reason ?? Reason.Disease
                };
                _context.ProstheticEpisodes.Add(prosthetic);
                break;

            case EpisodeType.Orthotic:
                var orthotic = new OrthoticEpisode
                {
                    EpisodeId = episode.Id,
                    MainProblem = "General Orthotic Need", // Default value
                    BodyRegion = model.BodyRegion ?? BodyRegion.LowerLimb,
                    Side = model.OrthoticSide ?? Side.Left,
                    ReasonForProblem = "Assessment Required" // Default value
                };
                _context.OrthoticEpisodes.Add(orthotic);
                break;

            case EpisodeType.SpinalOrthosis:
                var spinal = new SpinalEpisode
                {
                    EpisodeId = episode.Id
                };
                _context.SpinalEpisodes.Add(spinal);
                break;
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpdateTypeSpecificDetails(Episode episode, EpisodeViewModel model)
    {
        switch (episode.Type)
        {
            case EpisodeType.Prosthetic when episode.Prosthetic != null:
                episode.Prosthetic.AmputationType = model.AmputationType ?? AmputationType.BelowKnee;
                episode.Prosthetic.Level = model.Level ?? "Not Specified";
                episode.Prosthetic.Side = model.ProstheticSide ?? Side.Left;
                episode.Prosthetic.Reason = model.Reason ?? Reason.Disease;
                break;

            case EpisodeType.Orthotic when episode.Orthotic != null:
                episode.Orthotic.BodyRegion = model.BodyRegion ?? BodyRegion.LowerLimb;
                episode.Orthotic.Side = model.OrthoticSide ?? Side.Left;
                if (string.IsNullOrEmpty(episode.Orthotic.MainProblem))
                    episode.Orthotic.MainProblem = "General Orthotic Need";
                if (string.IsNullOrEmpty(episode.Orthotic.ReasonForProblem))
                    episode.Orthotic.ReasonForProblem = "Assessment Required";
                break;
        }

        await _context.SaveChangesAsync();
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Patients = new SelectList(
            await _context.Patients
                .Select(p => new { p.Id, DisplayName = p.PatientNumber + " - " + p.FirstName + " " + p.LastName })
                .ToListAsync(),
            "Id", "DisplayName");
    }
}