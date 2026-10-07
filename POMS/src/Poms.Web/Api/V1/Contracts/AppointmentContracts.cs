using System.ComponentModel.DataAnnotations;
using Poms.Domain.Enums;

namespace Poms.Web.Api.V1.Contracts;

public sealed class AppointmentListQuery
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public AppointmentType? Type { get; init; }
    public AppointmentStatus? Status { get; init; }
    public Guid? PatientId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    Guid? EpisodeId,
    string Type,
    DateOnly AppointmentDate,
    TimeOnly? AppointmentTime,
    string Status,
    string? AssignedClinicianUserId,
    string? AssignedClinicianName,
    string? Notes,
    string? CancellationReason,
    DateTime? CancelledAt,
    DateOnly? PreviousAppointmentDate,
    TimeOnly? PreviousAppointmentTime,
    string? RescheduleReason,
    DateTime? RescheduledAt);
