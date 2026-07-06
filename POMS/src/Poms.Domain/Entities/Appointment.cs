// ============================================================================
// Poms.Domain/Entities/Appointment.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Appointment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? EpisodeId { get; set; }
    public AppointmentType Type { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public TimeOnly? AppointmentTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }

    public Patient Patient { get; set; } = default!;
    public Episode? Episode { get; set; }
}
