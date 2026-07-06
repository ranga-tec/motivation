using Poms.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class EpisodeViewModel
{
    public Guid Id { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    [Required]
    [Display(Name = "Centre / Location")]
    public int CenterId { get; set; }

    [Display(Name = "Record Status")]
    public RecordStatus Status { get; set; } = RecordStatus.Active;

    [Display(Name = "Record Date")]
    public DateOnly RecordDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public string? Remarks { get; set; }

    // Patient info for display
    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }
}
