using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Reporting.Services;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "ReportOrAdmin")]
public class ReportsController : Controller
{
    private readonly IReportQueryService _reportQueryService;
    private readonly IPrintFormService _printFormService;
    private readonly PomsDbContext _context;
    private readonly IRestrictedAccessService _restrictedAccess;

    public ReportsController(
        IReportQueryService reportQueryService,
        IPrintFormService printFormService,
        PomsDbContext context,
        IRestrictedAccessService restrictedAccess)
    {
        _reportQueryService = reportQueryService;
        _printFormService = printFormService;
        _context = context;
        _restrictedAccess = restrictedAccess;
    }

    public IActionResult Index() => View();

    // 1. Patient Registration Report
    public async Task<IActionResult> PatientRegistration(ReportFilter filter, bool export = false)
    {
        var rows = await _reportQueryService.FilterPatients(filter)
            .OrderByDescending(p => p.RegistrationDate)
            .Select(p => new[] { p.PatientNumber, p.FullName, p.Category.ToString(), p.Center.Name, p.RegistrationDate.ToString("dd-MMM-yyyy") })
            .ToListAsync();

        return await Render("Patient Registration Report", nameof(PatientRegistration),
            new[] { "PNO", "Name", "Category", "Location", "Registration Date" }, rows, filter, export);
    }

    // 2. Active Records Report
    public async Task<IActionResult> ActiveRecords(ReportFilter filter, bool export = false)
    {
        filter.RecordStatus = RecordStatus.Active;
        var access = await _restrictedAccess.GetScopeAsync(User);
        var rows = await access.Filter(_reportQueryService.FilterEpisodes(filter))
            .OrderByDescending(e => e.RecordDate)
            .Select(e => new[] { e.Patient.PatientNumber, e.Patient.FullName, e.Center.Name, e.RecordDate.ToString("dd-MMM-yyyy"), e.Status.ToString() })
            .ToListAsync();

        return await Render("Active Records Report", nameof(ActiveRecords),
            new[] { "PNO", "Name", "Location", "Record Date", "Status" }, rows, filter, export);
    }

    // 3. Assessment Report
    public async Task<IActionResult> Assessment(ReportFilter filter, bool export = false)
    {
        var access = await _restrictedAccess.GetScopeAsync(User);
        var rows = await access.Filter(_reportQueryService.FilterAssessments(filter))
            .OrderByDescending(a => a.AssessedOn)
            .Select(a => new[] { a.Episode.Patient.PatientNumber, a.Episode.Patient.FullName, a.AssessmentType.ToString(), a.LimbCategory.ToString(), a.MainProblemType.Name, a.AssessedOn.ToString("dd-MMM-yyyy") })
            .ToListAsync();

        return await Render("Assessment Report", nameof(Assessment),
            new[] { "PNO", "Name", "Type", "Limb", "Main Problem", "Date" }, rows, filter, export);
    }

    // 4. Prosthetic Assessment Report
    public async Task<IActionResult> ProstheticAssessment(ReportFilter filter, bool export = false)
    {
        filter.AssessmentType = AssessmentType.Prosthetic;
        var access = await _restrictedAccess.GetScopeAsync(User);
        var rows = await access.Filter(_reportQueryService.FilterAssessments(filter))
            .OrderByDescending(a => a.AssessedOn)
            .Select(a => new[] { a.Episode.Patient.PatientNumber, a.Episode.Patient.FullName, a.LimbCategory.ToString(), a.Side.ToString(), a.AssessedOn.ToString("dd-MMM-yyyy") })
            .ToListAsync();

        return await Render("Prosthetic Assessment Report", nameof(ProstheticAssessment),
            new[] { "PNO", "Name", "Limb", "Side", "Date" }, rows, filter, export);
    }

    // 5. Orthotic Assessment Report
    public async Task<IActionResult> OrthoticAssessment(ReportFilter filter, bool export = false)
    {
        filter.AssessmentType = AssessmentType.Orthotic;
        var access = await _restrictedAccess.GetScopeAsync(User);
        var rows = await access.Filter(_reportQueryService.FilterAssessments(filter))
            .OrderByDescending(a => a.AssessedOn)
            .Select(a => new[] { a.Episode.Patient.PatientNumber, a.Episode.Patient.FullName, a.LimbCategory.ToString(), a.Side.ToString(), a.AssessedOn.ToString("dd-MMM-yyyy") })
            .ToListAsync();

        return await Render("Orthotic Assessment Report", nameof(OrthoticAssessment),
            new[] { "PNO", "Name", "Limb", "Side", "Date" }, rows, filter, export);
    }

    // 6. Fitting Report
    public async Task<IActionResult> Fitting(ReportFilter filter, bool export = false)
    {
        var access = await _restrictedAccess.GetScopeAsync(User);
        var rows = await access.Filter(_reportQueryService.FilterFittings(filter))
            .OrderByDescending(f => f.FittingDate)
            .Select(f => new[] { f.Episode.Patient.PatientNumber, f.Episode.Patient.FullName, f.Episode.Center.Name, f.FittingDate.ToString("dd-MMM-yyyy"), f.Notes ?? "" })
            .ToListAsync();

        return await Render("Fitting Report", nameof(Fitting),
            new[] { "PNO", "Name", "Location", "Fitting Date", "Notes" }, rows, filter, export);
    }

    // 7. Delivery Report
    public async Task<IActionResult> Delivery(ReportFilter filter, bool export = false)
    {
        var access = await _restrictedAccess.GetScopeAsync(User);
        var rows = await access.Filter(_reportQueryService.FilterDeliveries(filter))
            .OrderByDescending(d => d.DeliveryDate)
            .Select(d => new[] { d.Episode.Patient.PatientNumber, d.Episode.Patient.FullName, d.Episode.Center.Name, d.DeliveryDate.ToString("dd-MMM-yyyy"), d.Notes ?? "" })
            .ToListAsync();

        return await Render("Delivery Report", nameof(Delivery),
            new[] { "PNO", "Name", "Location", "Delivery Date", "Notes" }, rows, filter, export);
    }

    // 8. Follow-up Report
    public async Task<IActionResult> FollowUp(ReportFilter filter, bool export = false)
    {
        var access = await _restrictedAccess.GetScopeAsync(User);
        var rows = await access.Filter(_reportQueryService.FilterFollowUps(filter))
            .OrderByDescending(f => f.FollowUpDate)
            .Select(f => new[] { f.Episode.Patient.PatientNumber, f.Episode.Patient.FullName, f.Episode.Center.Name, f.FollowUpDate.ToString("dd-MMM-yyyy"), f.Notes ?? "" })
            .ToListAsync();

        return await Render("Follow-up Report", nameof(FollowUp),
            new[] { "PNO", "Name", "Location", "Follow-up Date", "Notes" }, rows, filter, export);
    }

    // 9. Location-wise Report
    public async Task<IActionResult> LocationWise(ReportFilter filter, bool export = false)
    {
        var grouped = await _reportQueryService.FilterPatients(filter)
            .GroupBy(p => p.Center.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();
        var rows = grouped.OrderByDescending(r => r.Count)
            .Select(r => new[] { r.Name, r.Count.ToString() })
            .ToList();

        return await Render("Location-wise Report", nameof(LocationWise), new[] { "Location", "Patient Count" }, rows, filter, export);
    }

    // 10. Foreign Patient Report
    public async Task<IActionResult> ForeignPatient(ReportFilter filter, bool export = false)
    {
        filter.IsForeign = true;
        var rows = await _reportQueryService.FilterPatients(filter)
            .OrderByDescending(p => p.RegistrationDate)
            .Select(p => new[] { p.PatientNumber, p.FullName, p.Nationality ?? "", p.Center.Name, p.RegistrationDate.ToString("dd-MMM-yyyy") })
            .ToListAsync();

        return await Render("Foreign Patient Report", nameof(ForeignPatient),
            new[] { "PNO", "Name", "Nationality", "Location", "Registration Date" }, rows, filter, export);
    }

    // 11. Date-wise Report (registration date based)
    public async Task<IActionResult> DateWise(ReportFilter filter, bool export = false)
    {
        var grouped = await _reportQueryService.FilterPatients(filter)
            .GroupBy(p => p.RegistrationDate)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();
        var rows = grouped.OrderBy(r => r.Date)
            .Select(r => new[] { r.Date.ToString("dd-MMM-yyyy"), r.Count.ToString() })
            .ToList();

        return await Render("Date-wise Report", nameof(DateWise), new[] { "Date", "Registrations" }, rows, filter, export);
    }

    // 12. Province-wise Report
    public async Task<IActionResult> ProvinceWise(ReportFilter filter, bool export = false)
    {
        var grouped = await _reportQueryService.FilterPatients(filter)
            .GroupBy(p => p.Province.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync();
        var rows = grouped.OrderByDescending(r => r.Count)
            .Select(r => new[] { r.Name, r.Count.ToString() })
            .ToList();

        return await Render("Province-wise Report", nameof(ProvinceWise), new[] { "Province", "Patient Count" }, rows, filter, export);
    }

    // 13. Year-wise Report
    public async Task<IActionResult> YearWise(ReportFilter filter, bool export = false)
    {
        var grouped = await _reportQueryService.FilterPatients(filter)
            .GroupBy(p => p.RegistrationDate.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .ToListAsync();
        var rows = grouped.OrderBy(r => r.Year)
            .Select(r => new[] { r.Year.ToString(), r.Count.ToString() })
            .ToList();

        return await Render("Year-wise Report", nameof(YearWise), new[] { "Year", "Patient Count" }, rows, filter, export);
    }

    private async Task<IActionResult> Render(string title, string actionName, string[] headers, List<string[]> rows, ReportFilter filter, bool export)
    {
        Response.Headers.CacheControl = "no-store, private";
        if (export)
        {
            var pdf = _printFormService.GenerateReportPdf(title, headers, rows);
            return File(pdf, "application/pdf", $"{actionName}.pdf");
        }

        await PopulateFilterDropdowns();

        return View("Result", new ReportResultViewModel
        {
            Title = title,
            ActionName = actionName,
            Headers = headers,
            Rows = rows,
            Filter = filter
        });
    }

    private async Task PopulateFilterDropdowns()
    {
        ViewBag.Centers = new SelectList(await _context.Centers.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
        ViewBag.Provinces = new SelectList(await _context.Provinces.ToListAsync(), "Id", "Name");
        ViewBag.AssessmentTypes = new SelectList(Enum.GetValues(typeof(AssessmentType)).Cast<AssessmentType>());
        ViewBag.LimbCategories = new SelectList(Enum.GetValues(typeof(LimbCategory)).Cast<LimbCategory>());
        ViewBag.RecordStatuses = new SelectList(Enum.GetValues(typeof(RecordStatus)).Cast<RecordStatus>());
    }
}
