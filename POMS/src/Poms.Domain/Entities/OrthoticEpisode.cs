// ============================================================================
// Poms.Domain/Entities/OrthoticEpisode.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Enums;

public class OrthoticEpisode
{
    public Guid EpisodeId { get; set; }
    public string MainProblem { get; set; } = default!;
    public BodyRegion BodyRegion { get; set; }
    public Side Side { get; set; }
    public int? OrthosisTypeId { get; set; }
    public string ReasonForProblem { get; set; } = default!;
    public string? ReasonOther { get; set; }
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
    public DeviceCatalog? OrthosisType { get; set; }
}
