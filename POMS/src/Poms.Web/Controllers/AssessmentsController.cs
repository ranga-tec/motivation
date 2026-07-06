using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Constants;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "ClinicianOrAdmin")]
public class AssessmentsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly ILogger<AssessmentsController> _logger;

    public AssessmentsController(PomsDbContext context, ILogger<AssessmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Assessments/Create?episodeId=
    public async Task<IActionResult> Create(Guid episodeId)
    {
        var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == episodeId);
        if (episode == null) return NotFound();

        var vm = new AssessmentViewModel
        {
            EpisodeId = episodeId,
            PatientNumber = episode.Patient.PatientNumber,
            PatientName = episode.Patient.FullName,
            AssessedOn = DateOnly.FromDateTime(DateTime.Today)
        };

        await PopulateDropdowns();
        return View(vm);
    }

    // AJAX: Assessments/GetPrescriptionOptions?assessmentType=&limbCategory=
    [HttpGet]
    public IActionResult GetPrescriptionOptions(AssessmentType assessmentType, LimbCategory limbCategory)
    {
        var options = PrescriptionLists.For(assessmentType, limbCategory);
        return Json(options.Select(o => new { o.Code, o.Label, o.SubTypes }));
    }

    // POST: Assessments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssessmentViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var assessment = new Assessment
                {
                    EpisodeId = model.EpisodeId,
                    AssessmentType = model.AssessmentType,
                    LimbCategory = model.LimbCategory,
                    AssessedOn = model.AssessedOn,
                    MainProblemTypeId = model.MainProblemTypeId,
                    Side = model.Side,
                    CauseReasonTypeId = model.CauseReasonTypeId,
                    CauseReasonOther = model.CauseReasonOther,
                    AdditionalInformation = model.AdditionalInformation,
                    CreatedBy = User.Identity?.Name
                };
                _context.Assessments.Add(assessment);
                await _context.SaveChangesAsync();

                var options = PrescriptionLists.For(model.AssessmentType, model.LimbCategory);
                foreach (var (side, code, sub, other) in model.GetPrescriptionRows())
                {
                    var option = options.FirstOrDefault(o => o.Code == code);
                    _context.Prescriptions.Add(new Prescription
                    {
                        AssessmentId = assessment.Id,
                        Side = side,
                        PrescriptionCode = code,
                        PrescriptionLabel = option?.Label ?? code,
                        SubType = sub,
                        OtherText = other,
                        CreatedBy = User.Identity?.Name
                    });
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = "Assessment created successfully!";
                return RedirectToAction("Details", "Episodes", new { id = model.EpisodeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating assessment");
                ModelState.AddModelError("", "An error occurred while creating the assessment.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Assessments/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var assessment = await _context.Assessments
            .Include(a => a.Episode).ThenInclude(e => e.Patient)
            .Include(a => a.Prescriptions)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assessment == null) return NotFound();

        var model = new AssessmentViewModel
        {
            Id = assessment.Id,
            EpisodeId = assessment.EpisodeId,
            PatientNumber = assessment.Episode.Patient.PatientNumber,
            PatientName = assessment.Episode.Patient.FullName,
            AssessmentType = assessment.AssessmentType,
            LimbCategory = assessment.LimbCategory,
            AssessedOn = assessment.AssessedOn,
            MainProblemTypeId = assessment.MainProblemTypeId,
            Side = assessment.Side,
            CauseReasonTypeId = assessment.CauseReasonTypeId,
            CauseReasonOther = assessment.CauseReasonOther,
            AdditionalInformation = assessment.AdditionalInformation
        };

        if (assessment.Side == Side.Bilateral)
        {
            var left = assessment.Prescriptions.FirstOrDefault(p => p.Side == Side.Left);
            var right = assessment.Prescriptions.FirstOrDefault(p => p.Side == Side.Right);
            model.LeftPrescriptionCode = left?.PrescriptionCode;
            model.LeftSubType = left?.SubType;
            model.LeftOtherText = left?.OtherText;
            model.RightPrescriptionCode = right?.PrescriptionCode;
            model.RightSubType = right?.SubType;
            model.RightOtherText = right?.OtherText;
        }
        else
        {
            var single = assessment.Prescriptions.FirstOrDefault();
            model.PrescriptionCode = single?.PrescriptionCode;
            model.SubType = single?.SubType;
            model.OtherText = single?.OtherText;
        }

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Assessments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AssessmentViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var assessment = await _context.Assessments.Include(a => a.Prescriptions).FirstOrDefaultAsync(a => a.Id == id);
                if (assessment == null) return NotFound();

                assessment.AssessmentType = model.AssessmentType;
                assessment.LimbCategory = model.LimbCategory;
                assessment.AssessedOn = model.AssessedOn;
                assessment.MainProblemTypeId = model.MainProblemTypeId;
                assessment.Side = model.Side;
                assessment.CauseReasonTypeId = model.CauseReasonTypeId;
                assessment.CauseReasonOther = model.CauseReasonOther;
                assessment.AdditionalInformation = model.AdditionalInformation;
                assessment.UpdatedBy = User.Identity?.Name;
                assessment.UpdatedAt = DateTime.UtcNow;

                // Rebuild prescriptions (at most 2 rows, no history requirement)
                _context.Prescriptions.RemoveRange(assessment.Prescriptions);

                var options = PrescriptionLists.For(model.AssessmentType, model.LimbCategory);
                foreach (var (side, code, sub, other) in model.GetPrescriptionRows())
                {
                    var option = options.FirstOrDefault(o => o.Code == code);
                    _context.Prescriptions.Add(new Prescription
                    {
                        AssessmentId = assessment.Id,
                        Side = side,
                        PrescriptionCode = code,
                        PrescriptionLabel = option?.Label ?? code,
                        SubType = sub,
                        OtherText = other,
                        CreatedBy = User.Identity?.Name
                    });
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Assessment updated successfully!";
                return RedirectToAction("Details", "Episodes", new { id = assessment.EpisodeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating assessment {AssessmentId}", id);
                ModelState.AddModelError("", "An error occurred while updating the assessment.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.MainProblemTypes = new SelectList(await _context.MainProblemTypes.Where(m => m.IsActive).ToListAsync(), "Id", "Name");
        ViewBag.CauseReasonTypes = new SelectList(await _context.CauseReasonTypes.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
    }
}
