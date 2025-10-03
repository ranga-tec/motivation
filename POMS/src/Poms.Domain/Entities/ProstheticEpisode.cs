// ============================================================================
// Poms.Domain/Entities/ProstheticEpisode.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Enums;

public class ProstheticEpisode
{
    public Guid EpisodeId { get; set; }
    public AmputationType AmputationType { get; set; }
    public string Level { get; set; } = default!;
    public Side Side { get; set; }
    public DateOnly? DateOfAmputation { get; set; }
    public Reason Reason { get; set; }
    public string? ReasonOther { get; set; }
    public int? DesiredDeviceId { get; set; }
    public string? SelectedComponentsJson { get; set; }
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
    public DeviceCatalog? DesiredDevice { get; set; }
}
