using System.ComponentModel.DataAnnotations;
using Poms.Domain.Enums;

namespace Poms.Web.ViewModels;

public class FittingViewModel
{
    public Guid Id { get; set; }

    [Required]
    public Guid EpisodeId { get; set; }

    public int FittingNumber { get; set; }

    [Required]
    [Display(Name = "Fitting Date")]
    [DataType(DataType.Date)]
    public DateOnly FittingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Status")]
    public FittingStatus Status { get; set; } = FittingStatus.Scheduled;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Adjustments Made")]
    public string? Adjustments { get; set; }

    [Display(Name = "Patient Feedback")]
    public string? PatientFeedback { get; set; }

    [Display(Name = "Next Steps")]
    public string? NextSteps { get; set; }

    [Display(Name = "Next Fitting Date")]
    [DataType(DataType.Date)]
    public DateOnly? NextFittingDate { get; set; }

    [Display(Name = "Performed By")]
    public string? PerformedBy { get; set; }

    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }

    // For display
    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }
    public string? EpisodeType { get; set; }
}
