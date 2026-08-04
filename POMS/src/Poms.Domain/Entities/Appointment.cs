// ============================================================================
// Poms.Domain/Entities/Appointment.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Appointment : BaseEntity
{
    public const int CancellationReasonMaxLength = 500;
    public const int RescheduleReasonMaxLength = 500;

    public Guid PatientId { get; set; }
    public Guid? EpisodeId { get; set; }
    public AppointmentType Type { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly? AppointmentTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? AssignedClinicianUserId { get; set; }
    public string? AssignedClinicianName { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateOnly? PreviousAppointmentDate { get; private set; }
    public TimeOnly? PreviousAppointmentTime { get; private set; }
    public string? RescheduleReason { get; private set; }
    public DateTime? RescheduledAt { get; private set; }

    public Patient Patient { get; set; } = default!;
    public Episode? Episode { get; set; }

    public void Cancel(string reason, string? cancelledBy, DateTime? cancelledAt = null)
    {
        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
            throw new ArgumentException("A cancellation reason is required.", nameof(reason));
        if (normalizedReason.Length > CancellationReasonMaxLength)
            throw new ArgumentException(
                $"The cancellation reason cannot exceed {CancellationReasonMaxLength} characters.",
                nameof(reason));

        Status = AppointmentStatus.Cancelled;
        CancellationReason = normalizedReason;
        CancelledAt = cancelledAt ?? DateTime.UtcNow;
        UpdatedAt = CancelledAt;
        UpdatedBy = cancelledBy;
    }

    public void Reschedule(
        DateOnly newDate,
        TimeOnly? newTime,
        string reason,
        string? rescheduledBy,
        DateTime? rescheduledAt = null)
    {
        if (Status != AppointmentStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled appointments can be rescheduled.");
        if (newDate == default)
            throw new ArgumentException("A new appointment date is required.", nameof(newDate));

        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
            throw new ArgumentException("A rescheduling reason is required.", nameof(reason));
        if (normalizedReason.Length > RescheduleReasonMaxLength)
            throw new ArgumentException(
                $"The rescheduling reason cannot exceed {RescheduleReasonMaxLength} characters.",
                nameof(reason));
        if (newDate == AppointmentDate && newTime == AppointmentTime)
            throw new ArgumentException("Choose a new appointment date or time.", nameof(newDate));

        PreviousAppointmentDate = AppointmentDate;
        PreviousAppointmentTime = AppointmentTime;
        AppointmentDate = newDate;
        AppointmentTime = newTime;
        RescheduleReason = normalizedReason;
        RescheduledAt = rescheduledAt ?? DateTime.UtcNow;
        UpdatedAt = RescheduledAt;
        UpdatedBy = rescheduledBy;
    }
}
