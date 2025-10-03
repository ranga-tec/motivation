// ============================================================================
// Poms.Domain/Entities/PatientDocument.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;

public class PatientDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Title { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string? TagsJson { get; set; }
    public string? Remarks { get; set; }
    public string UploadedBy { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    
    public Patient Patient { get; set; } = default!;
}
