// ============================================================================
// Poms.Domain/Entities/ApplicationUser.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public int? CenterId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public Center? Center { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
