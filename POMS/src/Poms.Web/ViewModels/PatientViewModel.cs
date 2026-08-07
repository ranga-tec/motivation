using Poms.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Poms.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class PatientViewModel : IValidatableObject
{
    public static IReadOnlyList<string> GuardianRelationshipOptions { get; } =
    [
        "Mother", "Father", "Spouse", "Brother", "Sister", "Son", "Daughter",
        "Niece", "Nephew", "Grandmother", "Grandfather", "Aunt", "Uncle",
        "Cousin", "Caregiver", "Friend", "Other"
    ];

    public Guid Id { get; set; }

    [Display(Name = "Patient Number")]
    public string? PatientNumber { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    [StringLength(200)]
    public string FullName { get; set; } = default!;

    [Required]
    [Display(Name = "Name with Initials")]
    [StringLength(100)]
    public string NameWithInitials { get; set; } = default!;

    [Required]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateOnly Dob { get; set; }

    [Required]
    [Display(Name = "Gender")]
    public Sex Sex { get; set; }

    [Display(Name = "Employment")]
    public string? Employment { get; set; }

    [Display(Name = "Patient Photo")]
    public IFormFile? ProfilePhoto { get; set; }

    [Display(Name = "Remove current photo")]
    public bool RemoveProfilePhoto { get; set; }

    [BindNever]
    public string? ExistingProfilePhotoUrl { get; set; }

    [Required]
    [Display(Name = "Patient Category")]
    public PatientCategory Category { get; set; } = PatientCategory.Local;

    [Display(Name = "Nationality")]
    public string? Nationality { get; set; }

    [Required]
    [Display(Name = "Identification Type")]
    public IdentificationType IdentificationType { get; set; }

    [Required]
    [Display(Name = "Identification Number")]
    [StringLength(50)]
    public string IdentificationNumber { get; set; } = default!;

    [Required]
    [Display(Name = "Address Line 1")]
    public string Address1 { get; set; } = "";

    [Display(Name = "Address Line 2")]
    public string? Address2 { get; set; }

    [Required]
    [Display(Name = "Province")]
    public int ProvinceId { get; set; }

    [Required]
    [Display(Name = "District")]
    public int DistrictId { get; set; }

    [Display(Name = "City")]
    public int? CityId { get; set; }

    [Display(Name = "City (if not listed)")]
    public string? CityOther { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    public List<PatientContactViewModel> Contacts { get; set; } = new();

    [Display(Name = "Referral Source")]
    public int? ReferralSourceId { get; set; }

    [Display(Name = "Referral Source (Other)")]
    public string? ReferralSourceOther { get; set; }

    [Display(Name = "Referral Person Name")]
    [StringLength(150)]
    public string? ReferralPersonName { get; set; }

    [Display(Name = "Referral Person Contact Number")]
    [Phone]
    [StringLength(30)]
    public string? ReferralPersonContactNumber { get; set; }

    [Display(Name = "Time/Distance to Travel to Centre")]
    public string? TravelTimeDistance { get; set; }

    [Required]
    [Display(Name = "Branch")]
    public int CenterId { get; set; }

    [Display(Name = "Date of Registration")]
    [DataType(DataType.Date)]
    public DateOnly? RegistrationDate { get; set; }

    [Required]
    [Display(Name = "Registration Processed By")]
    public string RegistrationProcessedBy { get; set; } = default!;

    [Display(Name = "Remarks")]
    [DataType(DataType.MultilineText)]
    public string? Remarks { get; set; }

    [Required]
    [StringLength(400)]
    [Display(Name = "Handled By (Prosthetist / Orthotist)")]
    public string AssignedClinicianEntry { get; set; } = string.Empty;

    public string? AssignedClinicianUserId { get; set; }

    [BindNever]
    public IReadOnlyList<AppointmentAssigneeOption> AssigneeOptions { get; set; } = [];

    // Guardian / Carer Information
    [Required]
    [Display(Name = "Guardian / Carer Name")]
    [StringLength(100)]
    public string GuardianName { get; set; } = default!;

    [Required]
    [Display(Name = "Relationship")]
    [StringLength(50)]
    public string GuardianRelationship { get; set; } = default!;

    [Display(Name = "Guardian Address")]
    public string? GuardianAddress { get; set; }

    [Display(Name = "Guardian Phone")]
    [Phone]
    public string? GuardianPhone { get; set; }

    [Display(Name = "Guardian Mobile")]
    [Phone]
    public string? GuardianMobile { get; set; }

    // Duplicate-check confirmation flag (Phase 2.2/2.3)
    public bool ConfirmDuplicateWarning { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var latestAllowedDob = DateOnly.FromDateTime(DateTime.Today.AddDays(-3));
        if (Dob != default && Dob > latestAllowedDob)
        {
            yield return new ValidationResult(
                $"Date of birth must be on or before {latestAllowedDob:dd-MMM-yyyy}.",
                new[] { nameof(Dob) });
        }

        if (ProfilePhoto?.Length > PatientPhotoValidator.MaxFileSizeBytes)
        {
            yield return new ValidationResult(
                "The patient photo must be 5 MB or smaller.",
                new[] { nameof(ProfilePhoto) });
        }

        if (Category == PatientCategory.Foreign && string.IsNullOrWhiteSpace(Nationality))
        {
            yield return new ValidationResult(
                "Nationality is required for foreign patients.",
                new[] { nameof(Nationality) });
        }

        if (CityId == null && string.IsNullOrWhiteSpace(CityOther))
        {
            yield return new ValidationResult(
                "Select a city or enter one if not listed.",
                new[] { nameof(CityOther) });
        }
    }
}
