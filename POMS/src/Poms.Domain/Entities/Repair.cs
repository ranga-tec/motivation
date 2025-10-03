// ============================================================================
// Poms.Domain/Entities/Repair.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Repair : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public DateOnly RepairDate { get; set; }
    public RepairCategory Category { get; set; }
    public string Details { get; set; } = default!;
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
}
