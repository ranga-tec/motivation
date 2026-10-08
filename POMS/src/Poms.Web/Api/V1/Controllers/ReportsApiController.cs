using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Reporting.Services;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController, Route("api/v1/reports")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "ReportOrAdmin")]
public sealed class ReportsApiController(PomsDbContext context, IReportQueryService queries, IRestrictedAccessService restrictedAccess, IPrintFormService printForms) : ControllerBase
{
    private static readonly ReportDefinition[] Definitions = [new("patient-registration", "Patient Registration Report"), new("active-records", "Active Records Report"), new("assessment", "Assessment Report"), new("prosthetic-assessment", "Prosthetic Assessment Report"), new("orthotic-assessment", "Orthotic Assessment Report"), new("fitting", "Fitting Report"), new("delivery", "Delivery Report"), new("follow-up", "Follow-up Report"), new("location-wise", "Location-wise Report"), new("foreign-patient", "Foreign Patient Report"), new("date-wise", "Date-wise Report"), new("province-wise", "Province-wise Report"), new("year-wise", "Year-wise Report")];

    [HttpGet("options")]
    public async Task<ReportOptionsResponse> Options(CancellationToken token) => new(Definitions,
        await context.Centers.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, x.Code, x.Name, true, null, null)).ToListAsync(token),
        await context.Provinces.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, x.Code, x.Name, true, null, null)).ToListAsync(token));

    [HttpGet("{key}")]
    public async Task<ActionResult<ReportResultResponse>> Get(string key, [FromQuery] ReportFilter filter, CancellationToken token)
    {
        if (filter.DateFrom > filter.DateTo) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(filter.DateTo)] = ["End date must be on or after start date."] }));
        var definition = Definitions.SingleOrDefault(x => x.Key == key); if (definition is null) return NotFound();
        var (headers, rows) = await Build(key, filter, token); Response.Headers.CacheControl = "no-store, private";
        return Ok(new ReportResultResponse(key, definition.Title, headers, rows, rows.Count));
    }

    [HttpGet("{key}/pdf")]
    public async Task<IActionResult> Pdf(string key, [FromQuery] ReportFilter filter, CancellationToken token) { if (filter.DateFrom > filter.DateTo) return BadRequest(); var definition = Definitions.SingleOrDefault(x => x.Key == key); if (definition is null) return NotFound(); var (headers, rows) = await Build(key, filter, token); Response.Headers.CacheControl = "no-store, private"; return File(printForms.GenerateReportPdf(definition.Title, headers.ToArray(), rows.Select(x => x.ToArray()).ToList()), "application/pdf", $"{key}.pdf"); }

    private async Task<(IReadOnlyList<string>, List<IReadOnlyList<string>>)> Build(string key, ReportFilter f, CancellationToken token)
    {
        var access = await restrictedAccess.GetScopeAsync(User);
        if (key == "active-records") f.RecordStatus = RecordStatus.Active; if (key == "prosthetic-assessment") f.AssessmentType = AssessmentType.Prosthetic; if (key == "orthotic-assessment") f.AssessmentType = AssessmentType.Orthotic; if (key == "foreign-patient") f.IsForeign = true;
        if (key is "patient-registration" or "foreign-patient") { var data = await queries.FilterPatients(f).OrderByDescending(x => x.RegistrationDate).Select(x => new { x.PatientNumber, x.FullName, x.Category, Nationality = x.Nationality ?? "", Center = x.Center.Name, x.RegistrationDate }).ToListAsync(token); return (key == "foreign-patient" ? ["PNO", "Name", "Nationality", "Location", "Registration Date"] : ["PNO", "Name", "Category", "Location", "Registration Date"], data.Select(x => (IReadOnlyList<string>)[x.PatientNumber, x.FullName, key == "foreign-patient" ? x.Nationality : x.Category.ToString(), x.Center, D(x.RegistrationDate)]).ToList()); }
        if (key == "active-records") { var data = await access.Filter(queries.FilterEpisodes(f)).OrderByDescending(x => x.RecordDate).ToListAsync(token); return (["PNO", "Name", "Location", "Record Date", "Time", "Status"], data.Select(x => (IReadOnlyList<string>)[x.Patient.PatientNumber, x.Patient.FullName, x.Center.Name, D(x.RecordDate), x.RecordTime?.ToString("HH:mm") ?? "", x.Status.ToString()]).ToList()); }
        if (key is "assessment" or "prosthetic-assessment" or "orthotic-assessment") { var data = await access.Filter(queries.FilterAssessments(f)).OrderByDescending(x => x.AssessedOn).ToListAsync(token); return (["PNO", "Name", "Type", "Limb", "Main Problem", "Date", "Time"], data.Select(x => (IReadOnlyList<string>)[x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.AssessmentType.ToString(), x.LimbCategory.ToString(), x.MainProblemType.Name, D(x.AssessedOn), T(x.StartTime, x.EndTime)]).ToList()); }
        if (key == "fitting") { var data = await access.Filter(queries.FilterFittings(f)).OrderByDescending(x => x.FittingDate).ToListAsync(token); return (["PNO", "Name", "Location", "Fitting Date", "Notes"], data.Select(x => (IReadOnlyList<string>)[x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, D(x.FittingDate), x.Notes ?? ""]).ToList()); }
        if (key == "delivery") { var data = await access.Filter(queries.FilterDeliveries(f)).OrderByDescending(x => x.DeliveryDate).ToListAsync(token); return (["PNO", "Name", "Location", "Delivery Date", "Time", "Notes"], data.Select(x => (IReadOnlyList<string>)[x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, D(x.DeliveryDate), x.DeliveryTime?.ToString("HH:mm") ?? "", x.Notes ?? ""]).ToList()); }
        if (key == "follow-up") { var data = await access.Filter(queries.FilterFollowUps(f)).OrderByDescending(x => x.FollowUpDate).ToListAsync(token); return (["PNO", "Name", "Location", "Follow-up Date", "Time", "Notes"], data.Select(x => (IReadOnlyList<string>)[x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, D(x.FollowUpDate), T(x.StartTime, x.EndTime), x.Notes ?? ""]).ToList()); }
        var patients = queries.FilterPatients(f); if (key == "location-wise") { var data = await patients.GroupBy(x => x.Center.Name).Select(x => new { Label = x.Key, Count = x.Count() }).OrderByDescending(x => x.Count).ToListAsync(token); return (["Location", "Patient Count"], data.Select(x => (IReadOnlyList<string>)[x.Label, x.Count.ToString()]).ToList()); } if (key == "province-wise") { var data = await patients.GroupBy(x => x.Province.Name).Select(x => new { Label = x.Key, Count = x.Count() }).OrderByDescending(x => x.Count).ToListAsync(token); return (["Province", "Patient Count"], data.Select(x => (IReadOnlyList<string>)[x.Label, x.Count.ToString()]).ToList()); } if (key == "date-wise") { var data = await patients.GroupBy(x => x.RegistrationDate).Select(x => new { Label = x.Key, Count = x.Count() }).OrderBy(x => x.Label).ToListAsync(token); return (["Date", "Registrations"], data.Select(x => (IReadOnlyList<string>)[D(x.Label), x.Count.ToString()]).ToList()); } var years = await patients.GroupBy(x => x.RegistrationDate.Year).Select(x => new { Label = x.Key, Count = x.Count() }).OrderBy(x => x.Label).ToListAsync(token); return (["Year", "Patient Count"], years.Select(x => (IReadOnlyList<string>)[x.Label.ToString(), x.Count.ToString()]).ToList());
    }
    private static string D(DateOnly value) => value.ToString("dd-MMM-yyyy"); private static string T(TimeOnly? start, TimeOnly? end) => start.HasValue && end.HasValue ? $"{start:HH:mm} - {end:HH:mm}" : "";
}
