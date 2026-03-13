using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class ProvinceViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "Code")]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class DistrictViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "Code")]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required]
    [Display(Name = "Province")]
    public int ProvinceId { get; set; }

    public string? ProvinceName { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class CenterViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "Code")]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required]
    [Display(Name = "District")]
    public int DistrictId { get; set; }

    public string? DistrictName { get; set; }

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Phone]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class DeviceTypeViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    [Display(Name = "Code")]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(100)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class DeviceCatalogViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Code")]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Required]
    [Display(Name = "Device Type")]
    public int DeviceTypeId { get; set; }

    public string? DeviceTypeName { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class ComponentCatalogViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Code")]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = "";

    [Display(Name = "Category")]
    public string? Category { get; set; }

    [Required]
    [Display(Name = "Device Type")]
    public int DeviceTypeId { get; set; }

    public string? DeviceTypeName { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
