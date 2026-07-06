// ============================================================================
// Poms.Domain/Entities/Prescription.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Prescription : BaseEntity
{
    public Guid AssessmentId { get; set; }
    public Side Side { get; set; }
    public string PrescriptionCode { get; set; } = default!;
    public string PrescriptionLabel { get; set; } = default!;
    public string? SubType { get; set; }
    public string? OtherText { get; set; }

    public Assessment Assessment { get; set; } = default!;
}
