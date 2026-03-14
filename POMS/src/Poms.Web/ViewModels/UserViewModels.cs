using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class UserListViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsActive { get; set; }
    public string? CenterName { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class CreateUserViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Display(Name = "Center")]
    public int? CenterId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Role")]
    public string? Role { get; set; }
}

public class EditUserViewModel
{
    public string Id { get; set; } = "";

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = "";

    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password (leave blank to keep current)")]
    public string? NewPassword { get; set; }

    [Display(Name = "Center")]
    public int? CenterId { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Role")]
    public string? Role { get; set; }

    public List<string> CurrentRoles { get; set; } = new();
}

public class AssignRolesViewModel
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public List<RoleSelection> Roles { get; set; } = new();
}

public class RoleSelection
{
    public string RoleName { get; set; } = "";
    public bool IsSelected { get; set; }
}
