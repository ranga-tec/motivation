using System.ComponentModel.DataAnnotations;
using Poms.Infrastructure.Services;

namespace Poms.Web.ViewModels;

public sealed class PatientFormOcrUploadViewModel
{
    [Required]
    [Display(Name = "Scanned registration form")]
    public IFormFile? FormImage { get; set; }

    [Required]
    [Display(Name = "OCR method")]
    public PatientFormOcrProvider? Provider { get; set; }

    public IReadOnlyList<PatientFormOcrProviderStatus> Providers { get; set; } = [];
    public bool AnyConfigured => Providers.Any(provider => provider.IsConfigured);
}
