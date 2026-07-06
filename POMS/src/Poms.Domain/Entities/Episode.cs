// ============================================================================
// Poms.Domain/Entities/Episode.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Episode : BaseEntity
{
    public Guid PatientId { get; set; }
    public int CenterId { get; set; }
    public RecordStatus Status { get; set; } = RecordStatus.Active;
    public DateOnly RecordDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? Remarks { get; set; }

    // Navigation
    public Patient Patient { get; set; } = default!;
    public Center Center { get; set; } = default!;
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<Fitting> Fittings { get; set; } = new List<Fitting>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<EpisodeDocument> Documents { get; set; } = new List<EpisodeDocument>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
