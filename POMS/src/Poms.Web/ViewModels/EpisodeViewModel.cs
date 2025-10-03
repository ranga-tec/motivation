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
    [Display(Name = "Body Region")]
    public BodyRegion? BodyRegion { get; set; }

    [Display(Name = "Side")]
    public Side? OrthoticSide { get; set; }

    // Spinal Episode Details (currently no specific fields beyond base Episode)
}