// ============================================================================
// Poms.Domain/Entities/AuditLog.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Action { get; set; } = default!; // Create/Update/Delete
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string? Changes { get; set; } // JSON diff
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
