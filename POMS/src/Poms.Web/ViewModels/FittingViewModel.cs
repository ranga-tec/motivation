using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class FittingViewModel
{
    public Guid Id { get; set; }

    [Required]
    public Guid EpisodeId { get; set; }

    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }

    [Required]
    [Display(Name = "Fitting Date")]
    public DateOnly FittingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Notes")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [Display(Name = "Restricted clinical record")]
    public bool IsRestricted { get; set; }
}
