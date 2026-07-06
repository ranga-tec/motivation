// ============================================================================
// Poms.Domain/Entities/Patient.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class Patient : BaseEntity
{
    public string PatientNumber { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string NameWithInitials { get; set; } = default!;
    public DateOnly Dob { get; set; }
    public Sex Sex { get; set; }
    public string? Employment { get; set; }

    public string Address1 { get; set; } = "";
    public string? Address2 { get; set; }
    public int ProvinceId { get; set; }
    public int DistrictId { get; set; }
    public int? CityId { get; set; }
    public string? CityOther { get; set; }
    public string? Email { get; set; }

    public string? Nationality { get; set; }
    public PatientCategory Category { get; set; }
    public IdentificationType IdentificationType { get; set; }
    public string IdentificationNumber { get; set; } = default!;

    public int CenterId { get; set; }

    public int? ReferralSourceId { get; set; }
    public string? ReferralSourceOther { get; set; }
    public string? TravelTimeDistance { get; set; }

    public DateOnly RegistrationDate { get; set; }
    public string RegistrationProcessedBy { get; set; } = default!;
    public string? Remarks { get; set; }

    // Guardian / Carer fields
    public string GuardianName { get; set; } = default!;
    public string GuardianRelationship { get; set; } = default!;
    public string? GuardianAddress { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianMobile { get; set; }

    // Navigation properties
    public Province Province { get; set; } = default!;
    public District District { get; set; } = default!;
    public City? City { get; set; }
    public Center Center { get; set; } = default!;
    public ReferralSource? ReferralSource { get; set; }
    public ICollection<PatientContact> Contacts { get; set; } = new List<PatientContact>();
    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
