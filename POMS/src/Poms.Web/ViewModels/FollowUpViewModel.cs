using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class FollowUpViewModel : IValidatableObject
{
    public Guid Id { get; set; }

    [Required]
    public Guid EpisodeId { get; set; }

    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }

    [Required]
    [Display(Name = "Follow-up Date")]
    public DateOnly FollowUpDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [Display(Name = "From")]
    public TimeOnly? StartTime { get; set; } = new(9, 0);

    [Required]
    [Display(Name = "To")]
    public TimeOnly? EndTime { get; set; } = new(10, 0);

    [Display(Name = "Remarks / Notes")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [Display(Name = "Restricted clinical record")]
    public bool IsRestricted { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime.HasValue && EndTime.HasValue && EndTime <= StartTime)
        {
            yield return new ValidationResult(
                "The follow-up end time must be later than the start time.",
                new[] { nameof(EndTime) });
        }
    }
}
