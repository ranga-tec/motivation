using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class DeliveryViewModel
{
    public Guid Id { get; set; }

    [Required]
    public Guid EpisodeId { get; set; }

    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }

    [Required]
    [Display(Name = "Date of Delivery")]
    public DateOnly DeliveryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Notes / Remarks")]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    [Display(Name = "Device")]
    public int? DeviceId { get; set; }

    [Display(Name = "Delivery Confirmation Attachment")]
    public IFormFile? Attachment { get; set; }

    [Display(Name = "Restricted clinical record")]
    public bool IsRestricted { get; set; }
}
