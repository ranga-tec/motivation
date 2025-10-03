// ============================================================================
// Poms.Domain/Entities/Patient.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Patient : BaseEntity
{
    public string PatientNumber { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string? LastName { get; set; }
    public DateOnly? Dob { get; set; }
    public Sex Sex { get; set; }
    public string? NationalId { get; set; }
    public string Address1 { get; set; } = "";
    public string? Address2 { get; set; }
    public int ProvinceId { get; set; }
    public int DistrictId { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public int CenterId { get; set; }
    public DateOnly RegistrationDate { get; set; }
    public string? ReferredBy { get; set; }
    public string? Remarks { get; set; }
    
    // Guardian fields
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianAddress { get; set; }
    public string? GuardianPhone1 { get; set; }
    public string? GuardianPhone2 { get; set; }
    
    // Navigation properties
    public Province Province { get; set; } = default!;
    public District District { get; set; } = default!;
    public Center Center { get; set; } = default!;
    public ICollection<PatientCondition> Conditions { get; set; } = new List<PatientCondition>();
    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
}
