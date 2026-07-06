// ============================================================================
// Poms.Domain/Entities/PatientContact.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;

public class PatientContact : BaseEntity
{
    public Guid PatientId { get; set; }
    public string TelephoneNo { get; set; } = default!;
    public DateOnly? DateConfirmed { get; set; }
    public string? PersonChecked { get; set; }

    public Patient Patient { get; set; } = default!;
}
