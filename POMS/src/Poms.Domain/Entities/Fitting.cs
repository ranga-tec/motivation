// ============================================================================
// Poms.Domain/Entities/Fitting.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Fitting : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public int FittingNumber { get; set; }
    public DateOnly FittingDate { get; set; }
    public FittingStatus Status { get; set; } = FittingStatus.Scheduled;
    public string? Notes { get; set; }
    public string? Adjustments { get; set; }
    public string? PatientFeedback { get; set; }
    public string? NextSteps { get; set; }
    public DateOnly? NextFittingDate { get; set; }
    public string? PerformedBy { get; set; }
    public string? Remarks { get; set; }

    public Episode Episode { get; set; } = default!;
}
