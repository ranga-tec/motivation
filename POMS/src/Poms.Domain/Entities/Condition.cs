// ============================================================================
// Poms.Domain/Entities/Condition.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Enums;

public class Condition
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public BodyRegion BodyRegion { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class PatientCondition
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public int ConditionId { get; set; }
    public Side Side { get; set; }
    public ConditionType Type { get; set; }
    public DateOnly? OnsetDate { get; set; }
    public string? Remarks { get; set; }

    public Patient Patient { get; set; } = default!;
    public Condition Condition { get; set; } = default!;
}
