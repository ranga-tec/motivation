using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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

public sealed record AppointmentOptionsResponse(IReadOnlyList<AssigneeOption> Assignees);

public sealed class CreateAppointmentRequest
{
    public Guid PatientId { get; init; }
    public Guid? EpisodeId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppointmentType Type { get; init; }
    public DateOnly AppointmentDate { get; init; }
    public TimeOnly? AppointmentTime { get; init; }
    [Required, StringLength(400)] public string AssignedClinicianEntry { get; init; } = string.Empty;
    public string? AssignedClinicianUserId { get; init; }
    [StringLength(2000)] public string? Notes { get; init; }
}

public sealed class RescheduleAppointmentRequest
{
    public DateOnly AppointmentDate { get; init; }
    public TimeOnly? AppointmentTime { get; init; }
    [Required, StringLength(500)] public string Reason { get; init; } = string.Empty;
}

public sealed class CancelAppointmentRequest
{
    [Required, StringLength(500)] public string Reason { get; init; } = string.Empty;
}
