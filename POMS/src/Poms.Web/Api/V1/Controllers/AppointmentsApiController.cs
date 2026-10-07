using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/appointments")]
[Authorize(Policy = "AnyAuthenticatedUser")]
public sealed class AppointmentsApiController(
    PomsDbContext context,
    IRestrictedAccessService restrictedAccess) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<AppointmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AppointmentResponse>>> List(
        [FromQuery] AppointmentListQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom > request.DateTo)
        {
            ModelState.AddModelError(nameof(request.DateTo), "DateTo must be on or after DateFrom.");
            return BadRequest(new ValidationProblemDetails(ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            });
        }

        var access = await restrictedAccess.GetScopeAsync(User);
        var query = access.Filter(context.Appointments.AsNoTracking());
        if (request.DateFrom.HasValue)
            query = query.Where(item => item.AppointmentDate >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(item => item.AppointmentDate <= request.DateTo.Value);
        if (request.Type.HasValue)
            query = query.Where(item => item.Type == request.Type.Value);
        if (request.Status.HasValue)
            query = query.Where(item => item.Status == request.Status.Value);
        if (request.PatientId.HasValue)
            query = query.Where(item => item.PatientId == request.PatientId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.AppointmentDate)
            .ThenBy(item => item.AppointmentTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new AppointmentResponse(
                item.Id,
                item.PatientId,
                item.Patient.PatientNumber,
                item.Patient.FullName,
                item.EpisodeId,
                item.Type.ToString(),
                item.AppointmentDate,
                item.AppointmentTime,
                item.Status.ToString(),
                item.AssignedClinicianUserId,
                item.AssignedClinicianName,
                item.Notes,
                item.CancellationReason,
                item.CancelledAt,
                item.PreviousAppointmentDate,
                item.PreviousAppointmentTime,
                item.RescheduleReason,
                item.RescheduledAt))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<AppointmentResponse>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var appointment = await context.Appointments
            .AsNoTracking()
            .Include(item => item.Patient)
            .Include(item => item.Episode)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (appointment is null)
            return NotFound();

        var access = await restrictedAccess.GetScopeAsync(User);
        var allowed = appointment.Episode is null || access.CanAccess(
            appointment.Episode.IsRestricted,
            appointment.Episode.CreatedBy);
        if (appointment.Episode is not null)
        {
            await restrictedAccess.AuditAsync(
                access,
                allowed ? "ApiViewAppointment" : "ApiViewAppointmentDenied",
                nameof(Appointment),
                appointment.Id,
                appointment.Episode.IsRestricted,
                allowed);
        }

        return allowed ? Ok(Map(appointment)) : NotFound();
    }

    private static AppointmentResponse Map(Appointment item) => new(
        item.Id,
        item.PatientId,
        item.Patient.PatientNumber,
        item.Patient.FullName,
        item.EpisodeId,
        item.Type.ToString(),
        item.AppointmentDate,
        item.AppointmentTime,
        item.Status.ToString(),
        item.AssignedClinicianUserId,
        item.AssignedClinicianName,
        item.Notes,
        item.CancellationReason,
        item.CancelledAt,
        item.PreviousAppointmentDate,
        item.PreviousAppointmentTime,
        item.RescheduleReason,
        item.RescheduledAt);
}
