// ============================================================================
// Poms.Domain/Entities/EmployeeProfile.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class EmployeeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public string EmployeeNumber { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Designation { get; set; } = default!;
    public string? Department { get; set; }
    public string MobileNumber { get; set; } = default!;
    public string? WorkPhoneNumber { get; set; }
    public bool CanAccessRestrictedClinicalData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
