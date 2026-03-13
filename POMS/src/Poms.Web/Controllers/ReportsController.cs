using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AnyAuthenticatedUser")]
public class ReportsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        PomsDbContext context,
        IReportService reportService,
        ILogger<ReportsController> logger)
    {
        _context = context;
        _reportService = reportService;
        _logger = logger;
    }

    // GET: Reports
    public async Task<IActionResult> Index()
    {
        await PopulateDropdowns();
        return View();
    }

    // GET: Reports/PatientRegistration
    public async Task<IActionResult> PatientRegistration()
    {
        await PopulateDropdowns();
        var model = new ReportFilterViewModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(model);
    }

    // POST: Reports/PatientRegistration
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PatientRegistration(ReportFilterViewModel model, string format)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        try
        {
            byte[] fileBytes;
            string contentType;
            string fileName;

            if (format == "excel")
            {
                fileBytes = await _reportService.GeneratePatientRegistrationReportExcelAsync(
                    model.StartDate, model.EndDate, model.CenterId);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"PatientRegistration_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.xlsx";
            }
            else
            {
                fileBytes = await _reportService.GeneratePatientRegistrationReportPdfAsync(
                    model.StartDate, model.EndDate, model.CenterId);
                contentType = "application/pdf";
                fileName = $"PatientRegistration_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.pdf";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating patient registration report");
            TempData["Error"] = "Error generating report. Please try again.";
            await PopulateDropdowns();
            return View(model);
        }
    }

    // GET: Reports/Episodes
    public async Task<IActionResult> Episodes()
    {
        await PopulateDropdowns();
        var model = new EpisodeReportFilterViewModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(model);
    }

    // POST: Reports/Episodes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Episodes(EpisodeReportFilterViewModel model, string format)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        try
        {
            byte[] fileBytes;
            string contentType;
            string fileName;

            if (format == "excel")
            {
                fileBytes = await _reportService.GenerateEpisodeSummaryReportExcelAsync(
                    model.StartDate, model.EndDate, model.EpisodeType);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Episodes_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.xlsx";
            }
            else
            {
                fileBytes = await _reportService.GenerateEpisodeSummaryReportPdfAsync(
                    model.StartDate, model.EndDate, model.EpisodeType);
                contentType = "application/pdf";
                fileName = $"Episodes_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.pdf";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating episode report");
            TempData["Error"] = "Error generating report. Please try again.";
            await PopulateDropdowns();
            return View(model);
        }
    }

    // GET: Reports/Deliveries
    public async Task<IActionResult> Deliveries()
    {
        await PopulateDropdowns();
        var model = new ReportFilterViewModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(model);
    }

    // POST: Reports/Deliveries
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deliveries(ReportFilterViewModel model, string format)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        try
        {
            byte[] fileBytes;
            string contentType;
            string fileName;

            if (format == "excel")
            {
                fileBytes = await _reportService.GenerateDeliveryReportExcelAsync(
                    model.StartDate, model.EndDate, model.CenterId);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"Deliveries_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.xlsx";
            }
            else
            {
                fileBytes = await _reportService.GenerateDeliveryReportPdfAsync(
                    model.StartDate, model.EndDate, model.CenterId);
                contentType = "application/pdf";
                fileName = $"Deliveries_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.pdf";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating delivery report");
            TempData["Error"] = "Error generating report. Please try again.";
            await PopulateDropdowns();
            return View(model);
        }
    }

    // GET: Reports/FollowUps
    public async Task<IActionResult> FollowUps()
    {
        var model = new ReportFilterViewModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(model);
    }

    // POST: Reports/FollowUps
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FollowUps(ReportFilterViewModel model, string format)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            byte[] fileBytes;
            string contentType;
            string fileName;

            if (format == "excel")
            {
                fileBytes = await _reportService.GenerateFollowUpReportExcelAsync(model.StartDate, model.EndDate);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"FollowUps_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.xlsx";
            }
            else
            {
                fileBytes = await _reportService.GenerateFollowUpReportPdfAsync(model.StartDate, model.EndDate);
                contentType = "application/pdf";
                fileName = $"FollowUps_{model.StartDate:yyyyMMdd}_{model.EndDate:yyyyMMdd}.pdf";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating follow-up report");
            TempData["Error"] = "Error generating report. Please try again.";
            return View(model);
        }
    }

    // GET: Reports/PatientList
    public async Task<IActionResult> PatientList()
    {
        await PopulateDropdowns();
        return View(new PatientListFilterViewModel());
    }

    // POST: Reports/PatientList
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PatientList(PatientListFilterViewModel model, string format)
    {
        try
        {
            byte[] fileBytes;
            string contentType;
            string fileName;

            if (format == "excel")
            {
                fileBytes = await _reportService.GeneratePatientListExcelAsync(model.CenterId, model.IsActive);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                fileName = $"PatientList_{DateTime.Today:yyyyMMdd}.xlsx";
            }
            else
            {
                fileBytes = await _reportService.GeneratePatientListPdfAsync(model.CenterId, model.IsActive);
                contentType = "application/pdf";
                fileName = $"PatientList_{DateTime.Today:yyyyMMdd}.pdf";
            }

            return File(fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating patient list report");
            TempData["Error"] = "Error generating report. Please try again.";
            await PopulateDropdowns();
            return View(model);
        }
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Centers = new SelectList(
            await _context.Centers.Where(c => c.IsActive).ToListAsync(),
            "Id", "Name");

        ViewBag.EpisodeTypes = new SelectList(
            Enum.GetValues(typeof(EpisodeType)).Cast<EpisodeType>()
                .Select(e => new { Value = (int)e, Text = e.ToString() }),
            "Value", "Text");
    }
}

#region ViewModels

public class ReportFilterViewModel
{
    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Display(Name = "Center")]
    public int? CenterId { get; set; }
}

public class EpisodeReportFilterViewModel
{
    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Display(Name = "Episode Type")]
    public EpisodeType? EpisodeType { get; set; }
}

public class PatientListFilterViewModel
{
    [Display(Name = "Center")]
    public int? CenterId { get; set; }

    [Display(Name = "Status")]
    public bool? IsActive { get; set; }
}

#endregion
