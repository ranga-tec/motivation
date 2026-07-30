using Poms.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class AssessmentViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    [Required]
    public Guid EpisodeId { get; set; }

    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }

    [Required]
    [Display(Name = "Assessment Type")]
    public AssessmentType AssessmentType { get; set; }

    [Required]
    [Display(Name = "Limb Category")]
    public LimbCategory LimbCategory { get; set; }

    [Required]
    [Display(Name = "Date of Assessment")]
    public DateOnly AssessedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [Display(Name = "Main Problem Type")]
    public int MainProblemTypeId { get; set; }

    [Required]
    [Display(Name = "Side")]
    public Side Side { get; set; }

    [Required]
    [Display(Name = "Cause / Reason / Pathology Type")]
    public int CauseReasonTypeId { get; set; }

    [Display(Name = "Cause / Reason / Pathology (Other)")]
    public string? CauseReasonOther { get; set; }

    [Display(Name = "Additional Assessment Information")]
    public string? AdditionalInformation { get; set; }

    [Display(Name = "Restricted clinical record")]
    public bool IsRestricted { get; set; }

    // Non-bilateral prescription
    [Display(Name = "Prescription")]
    public string? PrescriptionCode { get; set; }
    public string? SubType { get; set; }
    public string? OtherText { get; set; }

    // Bilateral prescriptions
    [Display(Name = "Prescription for Left")]
    public string? LeftPrescriptionCode { get; set; }
    public string? LeftSubType { get; set; }
    public string? LeftOtherText { get; set; }

    [Display(Name = "Prescription for Right")]
    public string? RightPrescriptionCode { get; set; }
    public string? RightSubType { get; set; }
    public string? RightOtherText { get; set; }

    public IEnumerable<(Side Side, string Code, string? SubType, string? OtherText)> GetPrescriptionRows()
    {
        if (Side == Side.Bilateral)
        {
            if (!string.IsNullOrWhiteSpace(LeftPrescriptionCode))
                yield return (Side.Left, LeftPrescriptionCode!, LeftSubType, LeftOtherText);
            if (!string.IsNullOrWhiteSpace(RightPrescriptionCode))
                yield return (Side.Right, RightPrescriptionCode!, RightSubType, RightOtherText);
        }
        else if (!string.IsNullOrWhiteSpace(PrescriptionCode))
        {
            yield return (Side, PrescriptionCode!, SubType, OtherText);
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AssessmentType == AssessmentType.Prosthetic && LimbCategory == LimbCategory.Spinal)
        {
            yield return new ValidationResult("Spinal is only valid for Orthotic assessments.", new[] { nameof(LimbCategory) });
        }

        if (Side == Side.Bilateral)
        {
            if (string.IsNullOrWhiteSpace(LeftPrescriptionCode))
                yield return new ValidationResult("Prescription for Left is required when Side is Bilateral.", new[] { nameof(LeftPrescriptionCode) });
            if (string.IsNullOrWhiteSpace(RightPrescriptionCode))
                yield return new ValidationResult("Prescription for Right is required when Side is Bilateral.", new[] { nameof(RightPrescriptionCode) });
            if (LeftPrescriptionCode == "OTHER" && string.IsNullOrWhiteSpace(LeftOtherText))
                yield return new ValidationResult("Please specify the Left 'Other' prescription.", new[] { nameof(LeftOtherText) });
            if (RightPrescriptionCode == "OTHER" && string.IsNullOrWhiteSpace(RightOtherText))
                yield return new ValidationResult("Please specify the Right 'Other' prescription.", new[] { nameof(RightOtherText) });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(PrescriptionCode))
                yield return new ValidationResult("Prescription is required.", new[] { nameof(PrescriptionCode) });
            if (PrescriptionCode == "OTHER" && string.IsNullOrWhiteSpace(OtherText))
                yield return new ValidationResult("Please specify the 'Other' prescription.", new[] { nameof(OtherText) });
        }
    }
}
