// ============================================================================
// Poms.Domain/Entities/FollowUp.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;

public class FollowUp : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public DateOnly FollowUpDate { get; set; }
    public string ActionTaken { get; set; } = default!;
    public DateOnly? NextAppointmentDate { get; set; }
    public string? NextPlan { get; set; }
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
}
