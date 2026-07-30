using Poms.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class FileUploadViewModel
{
    public Guid? PatientId { get; set; }
    public Guid? EpisodeId { get; set; }

    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }

    [Required]
    [Display(Name = "Document Type")]
    public DocumentType DocumentType { get; set; }

    [Required]
    [Display(Name = "File")]
    public IFormFile File { get; set; } = default!;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Restricted document")]
    public bool IsRestricted { get; set; }
}
