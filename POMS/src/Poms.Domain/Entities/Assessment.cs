// ============================================================================
// Poms.Domain/Entities/Assessment.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Assessment : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public AssessmentType AssessmentType { get; set; }
    public LimbCategory LimbCategory { get; set; }
    public DateOnly AssessedOn { get; set; }
    public int MainProblemTypeId { get; set; }
    public Side Side { get; set; }
    public int CauseReasonTypeId { get; set; }
    public string? CauseReasonOther { get; set; }
    public string? AdditionalInformation { get; set; }
    public bool IsRestricted { get; set; }

    public Episode Episode { get; set; } = default!;
    public MainProblemType MainProblemType { get; set; } = default!;
    public CauseReasonType CauseReasonType { get; set; } = default!;
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
