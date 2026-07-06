using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class LookupItemViewModel
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = default!;

    public bool IsActive { get; set; } = true;
}

public class LocationViewModel
{
    public int Id { get; set; }

    [Required]
    public int DistrictId { get; set; }

    [Required]
    public string Code { get; set; } = default!;

    [Required]
    public string Name { get; set; } = default!;

    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresPatientNumberFlag { get; set; }
    public string? PatientNumberFlagCode { get; set; }
}
