using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public sealed class UserManagementViewModel
{
    public CreateUserViewModel NewUser { get; set; } = new();
    public IReadOnlyList<UserAccessRowViewModel> Users { get; set; } = [];
    public IReadOnlyList<string> AvailableRoles { get; set; } = [];
}

public sealed class CreateUserViewModel
{
    [Required]
    [StringLength(200)]
    [Display(Name = "Employee name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    [Display(Name = "Employee number")]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Designation { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Department { get; set; }

    [Required]
    [Phone]
    [StringLength(30)]
    [Display(Name = "Mobile number")]
    public string MobileNumber { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    [Display(Name = "Work phone")]
    public string? WorkPhoneNumber { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Select at least one access role.")]
    public List<string> Roles { get; set; } = [];

    [Display(Name = "Allow restricted clinical data")]
    public bool CanAccessRestrictedClinicalData { get; set; }
}

public sealed class UserAccessRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public string? WorkPhoneNumber { get; set; }
    public bool CanAccessRestrictedClinicalData { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsLocked { get; set; }
    public bool IsCurrentUser { get; set; }
}

public sealed class UpdateEmployeeProfileViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string Designation { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Department { get; set; }

    [Required]
    [Phone]
    [StringLength(30)]
    public string MobileNumber { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? WorkPhoneNumber { get; set; }

    public bool CanAccessRestrictedClinicalData { get; set; }
}

public sealed class UserRoleUpdateViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Select at least one access role.")]
    public List<string> Roles { get; set; } = [];
}

public sealed class ResetUserPasswordViewModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "New temporary password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
