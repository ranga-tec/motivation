using Poms.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class PatientViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Patient Number")]
    public string? PatientNumber { get; set; }

    [Required]
    [Display(Name = "First Name")]
    [StringLength(100)]
    public string FirstName { get; set; } = default!;

    [Display(Name = "Last Name")]
    [StringLength(100)]
    public string? LastName { get; set; }

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateOnly? Dob { get; set; }

    [Required]
    public Sex Sex { get; set; }

    [Display(Name = "National ID / NIC")]
    [StringLength(50)]
    public string? NationalId { get; set; }

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

    [Display(Name = "Phone 1")]
    [Phone]
    public string? Phone1 { get; set; }

    [Display(Name = "Phone 2")]
    [Phone]
    public string? Phone2 { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "Registration Center")]
    public int CenterId { get; set; }

    [Display(Name = "Registration Date")]
    [DataType(DataType.Date)]
    public DateOnly? RegistrationDate { get; set; }

    [Display(Name = "Referred By")]
    public string? ReferredBy { get; set; }

    [Required]
    [Display(Name = "Remarks")]
    [DataType(DataType.MultilineText)]
    public string? Remarks { get; set; } = "";

    // Guardian Information
    [Display(Name = "Guardian Name")]
    public string? GuardianName { get; set; }

    [Display(Name = "Guardian Relationship")]
    public string? GuardianRelationship { get; set; }

    [Display(Name = "Guardian Address")]
    public string? GuardianAddress { get; set; }

    [Display(Name = "Guardian Phone 1")]
    public string? GuardianPhone1 { get; set; }

    [Display(Name = "Guardian Phone 2")]
    public string? GuardianPhone2 { get; set; }
}
