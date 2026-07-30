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
}

public sealed class UserAccessRowViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsLocked { get; set; }
    public bool IsCurrentUser { get; set; }
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
