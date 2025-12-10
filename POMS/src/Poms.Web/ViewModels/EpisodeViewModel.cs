using Poms.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class EpisodeViewModel
{
    public Guid Id { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    [Required]
    [Display(Name = "Episode Type")]
    public EpisodeType Type { get; set; }

    [Required]
    [Display(Name = "Opened Date")]
    public DateOnly OpenedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Closed Date")]
    public DateOnly? ClosedOn { get; set; }

    public string? Remarks { get; set; }

    // Patient info for display
    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }

    // Prosthetic Episode Details
    [Display(Name = "Amputation Type")]
    public AmputationType? AmputationType { get; set; }

    [Display(Name = "Level")]
    public string? Level { get; set; }

    [Display(Name = "Side")]
    public Side? ProstheticSide { get; set; }

    [Display(Name = "Reason")]
    public Reason? Reason { get; set; }

    // Orthotic Episode Details
    [Display(Name = "Main Problem")]
    public string? MainProblem { get; set; }

    [Display(Name = "Body Region")]
    public BodyRegion? BodyRegion { get; set; }

    [Display(Name = "Side")]
    public Side? OrthoticSide { get; set; }

    [Display(Name = "Orthosis Type")]
    public int? OrthosisTypeId { get; set; }

    [Display(Name = "Reason for Problem")]
    public string? ReasonForProblem { get; set; }

    // Spinal Episode Details
    [Display(Name = "Pathological Condition")]
    public string? PathologicalCondition { get; set; }

    [Display(Name = "Orthotic Design")]
    public string? OrthoticDesign { get; set; }

    // Assessment Details (for initial assessment during episode creation)
    [Display(Name = "Date of Assessment")]
    public DateOnly? AssessmentDate { get; set; }

    [Display(Name = "Assessment Findings")]
    public string? AssessmentFindings { get; set; }

    // Fitting Details
    [Display(Name = "Fitting Date")]
    public DateOnly? FittingDate { get; set; }

    [Display(Name = "Fitting Notes")]
    public string? FittingNotes { get; set; }

    // Delivery Details
    [Display(Name = "Delivery Date")]
    public DateOnly? DeliveryDate { get; set; }
}