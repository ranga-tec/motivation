using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController, Route("api/v1/dashboard")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "AnyAuthenticatedUser")]
public sealed class DashboardApiController(PomsDbContext context, IRestrictedAccessService restrictedAccess) : ControllerBase
{
    private const int ActivityTake = 12;

    [HttpGet]
    public async Task<DashboardResponse> Get(CancellationToken token)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var month = new DateOnly(today.Year, today.Month, 1);
        var access = await restrictedAccess.GetScopeAsync(User);
        var activeEpisodes = access.Filter(context.Episodes).Where(x => x.Status == RecordStatus.Active);

        // PRD 5.2.4: today's activities are assessments, fittings and deliveries only; follow-ups are report-only.
        var todayAssessments = await access.Filter(context.Assessments).CountAsync(x => x.AssessedOn == today, token);
        var todayFittings = await access.Filter(context.Fittings).CountAsync(x => x.FittingDate == today, token);
        var todayDeliveries = await access.Filter(context.Deliveries).CountAsync(x => x.DeliveryDate == today, token);
        var todayScheduled = await context.Appointments.CountAsync(x => x.AppointmentDate == today
            && x.Status == AppointmentStatus.Scheduled
            && (x.Type == AppointmentType.Assessment || x.Type == AppointmentType.Fitting), token);

        return new(
            await context.Patients.CountAsync(token),
            await context.Appointments.CountAsync(x => x.AppointmentDate == today, token),
            await context.Appointments.CountAsync(x => x.AppointmentDate == today && x.Status == AppointmentStatus.Scheduled, token),
            await activeEpisodes.CountAsync(token),
            await access.Filter(context.Assessments).CountAsync(x => x.AssessedOn >= month, token),
            await access.Filter(context.Deliveries).CountAsync(x => x.DeliveryDate >= month, token),
            await activeEpisodes.Select(x => x.PatientId).Distinct().CountAsync(token),
            todayAssessments + todayFittings + todayDeliveries + todayScheduled,
            new TodayActivityBreakdown(todayAssessments, todayFittings, todayDeliveries, todayScheduled));
    }

    [HttpGet("activity")]
    public async Task<IReadOnlyList<DashboardActivity>> Activity(CancellationToken token)
    {
        var access = await restrictedAccess.GetScopeAsync(User);
        var items = new List<DashboardActivity>();

        items.AddRange(await context.Patients.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(ActivityTake)
            .Select(x => new DashboardActivity("PatientRegistered", "New patient registered", x.CreatedAt, x.Id, x.PatientNumber, x.FullName, x.Center.Name, x.CreatedBy, null))
            .ToListAsync(token));
        items.AddRange(await access.Filter(context.Assessments).AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(ActivityTake)
            .Select(x => new DashboardActivity("AssessmentCreated", x.AssessmentType == AssessmentType.Prosthetic ? "Prosthetic assessment created" : "Orthotic assessment created", x.CreatedAt, x.Episode.PatientId, x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, x.CreatedBy, x.EpisodeId))
            .ToListAsync(token));
        items.AddRange(await access.Filter(context.Fittings).AsNoTracking().OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(ActivityTake)
            .Select(x => new DashboardActivity("Fitting", x.UpdatedAt == null ? "Fitting date added" : "Fitting date changed", x.UpdatedAt ?? x.CreatedAt, x.Episode.PatientId, x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, x.UpdatedBy ?? x.CreatedBy, x.EpisodeId))
            .ToListAsync(token));
        items.AddRange(await access.Filter(context.Deliveries).AsNoTracking().OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt).Take(ActivityTake)
            .Select(x => new DashboardActivity("Delivery", x.UpdatedAt == null ? "Delivery recorded" : "Delivery date changed", x.UpdatedAt ?? x.CreatedAt, x.Episode.PatientId, x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, x.UpdatedBy ?? x.CreatedBy, x.EpisodeId))
            .ToListAsync(token));
        items.AddRange(await access.Filter(context.FollowUps).AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(ActivityTake)
            .Select(x => new DashboardActivity("FollowUp", "Follow-up added", x.CreatedAt, x.Episode.PatientId, x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, x.CreatedBy, x.EpisodeId))
            .ToListAsync(token));
        items.AddRange(await access.Filter(context.PatientDocuments).AsNoTracking().OrderByDescending(x => x.UploadedAt).Take(ActivityTake)
            .Select(x => new DashboardActivity("DocumentUploaded", "Document uploaded", x.UploadedAt, x.PatientId, x.Patient.PatientNumber, x.Patient.FullName, x.Patient.Center.Name, x.UploadedBy, null))
            .ToListAsync(token));
        items.AddRange(await access.Filter(context.EpisodeDocuments).AsNoTracking().OrderByDescending(x => x.UploadedAt).Take(ActivityTake)
            .Select(x => new DashboardActivity("DocumentUploaded", "Document uploaded", x.UploadedAt, x.Episode.PatientId, x.Episode.Patient.PatientNumber, x.Episode.Patient.FullName, x.Episode.Center.Name, x.UploadedBy, x.EpisodeId))
            .ToListAsync(token));

        Response.Headers.CacheControl = "no-store, private";
        return items.OrderByDescending(x => x.OccurredAt).Take(ActivityTake)
            .Select(x => x with { OccurredAt = DateTime.SpecifyKind(x.OccurredAt, DateTimeKind.Utc) })
            .ToList();
    }

    [HttpGet("records")]
    [Authorize(Policy = "PatientRead")]
    public async Task<ActionResult<PagedResponse<DashboardRecord>>> Records([FromQuery] DashboardRecordQuery request, CancellationToken token)
    {
        var access = await restrictedAccess.GetScopeAsync(User);
        var query = access.Filter(context.Episodes.AsNoTracking());
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status.Value);
        if (request.CenterId.HasValue) query = query.Where(x => x.CenterId == request.CenterId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Patient.PatientNumber.ToLower().Contains(search) || x.Patient.FullName.ToLower().Contains(search) || x.Patient.NameWithInitials.ToLower().Contains(search));
        }

        var total = await query.CountAsync(token);
        var rows = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new
            {
                x.Id, x.PatientId, x.Patient.PatientNumber, x.Patient.FullName, Center = x.Center.Name, x.Status, x.RecordDate,
                Assessment = x.Assessments.OrderByDescending(a => a.AssessedOn)
                    .Select(a => new { a.AssessmentType, a.LimbCategory, a.AssessedOn, Prescription = a.Prescriptions.Select(p => p.PrescriptionLabel).FirstOrDefault() })
                    .FirstOrDefault(),
                LastFitting = x.Fittings.Max(f => (DateOnly?)f.FittingDate),
                LastDelivery = x.Deliveries.Max(d => (DateOnly?)d.DeliveryDate),
                LastFollowUp = x.FollowUps.Max(f => (DateOnly?)f.FollowUpDate),
            })
            .ToListAsync(token);

        var items = rows.Select(x =>
        {
            var stage = x.LastFollowUp.HasValue ? "Follow-up" : x.LastDelivery.HasValue ? "Delivered" : x.LastFitting.HasValue ? "Fitting" : x.Assessment is not null ? "Assessed" : "Awaiting assessment";
            var dates = new[] { x.RecordDate, x.Assessment?.AssessedOn ?? DateOnly.MinValue, x.LastFitting ?? DateOnly.MinValue, x.LastDelivery ?? DateOnly.MinValue, x.LastFollowUp ?? DateOnly.MinValue };
            return new DashboardRecord(x.Id, x.PatientId, x.PatientNumber, x.FullName, x.Center, x.Assessment?.AssessmentType.ToString(), x.Assessment?.LimbCategory.ToString(), x.Assessment?.Prescription, dates.Max(), stage, x.Status.ToString());
        }).ToList();

        return Ok(new PagedResponse<DashboardRecord>(items, request.Page, request.PageSize, total, (int)Math.Ceiling(total / (double)request.PageSize)));
    }
}
