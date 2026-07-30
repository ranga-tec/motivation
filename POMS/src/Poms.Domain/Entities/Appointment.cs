// ============================================================================
// Poms.Domain/Entities/Appointment.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Appointment : BaseEntity
{
    public const int CancellationReasonMaxLength = 500;

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
}
