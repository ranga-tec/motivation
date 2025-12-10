// ============================================================================
// Poms.Domain/Entities/Episode.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Episode : BaseEntity
{
    public Guid PatientId { get; set; }
    public EpisodeType Type { get; set; }
    public DateOnly OpenedOn { get; set; }
    public DateOnly? ClosedOn { get; set; }
    public string? Remarks { get; set; }
    
    // Navigation
    public Patient Patient { get; set; } = default!;
    public ProstheticEpisode? Prosthetic { get; set; }
    public OrthoticEpisode? Orthotic { get; set; }
    public SpinalEpisode? Spinal { get; set; }
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<Fitting> Fittings { get; set; } = new List<Fitting>();
    public Delivery? Delivery { get; set; }
    public ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public ICollection<Repair> Repairs { get; set; } = new List<Repair>();
    public ICollection<EpisodeDocument> Documents { get; set; } = new List<EpisodeDocument>();
}
