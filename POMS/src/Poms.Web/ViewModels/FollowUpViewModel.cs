using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class FollowUpViewModel
{
    public Guid Id { get; set; }

    [Required]
    public Guid EpisodeId { get; set; }

    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }

    [Required]
    [Display(Name = "Follow-up Date")]
    public DateOnly FollowUpDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Remarks / Notes")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [Display(Name = "Restricted clinical record")]
    public bool IsRestricted { get; set; }
}
