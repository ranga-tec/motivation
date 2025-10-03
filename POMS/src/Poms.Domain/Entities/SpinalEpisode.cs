// ============================================================================
// Poms.Domain/Entities/SpinalEpisode.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class SpinalEpisode
{
    public Guid EpisodeId { get; set; }
    public string PathologicalCondition { get; set; } = default!;
    public string OrthoticDesign { get; set; } = default!;
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
}
