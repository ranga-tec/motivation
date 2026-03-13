// ============================================================================
// Poms.Domain/Entities/SystemSetting.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
