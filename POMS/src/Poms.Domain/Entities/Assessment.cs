// ============================================================================
// Poms.Domain/Entities/Assessment.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;

public class Assessment : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public DateOnly AssessedOn { get; set; }
    public string? ClinicianId { get; set; }
    public string Findings { get; set; } = default!;
    public string? AttachmentsJson { get; set; }
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
}
