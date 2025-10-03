// ============================================================================
// Poms.Domain/Entities/Fitting.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;

public class Fitting : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public DateOnly FittingDate { get; set; }
    public string? Notes { get; set; }
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
}
