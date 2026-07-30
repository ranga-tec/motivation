// ============================================================================
// Poms.Domain/Entities/PatientDocument.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class PatientDocument : BaseEntity
{
    public Guid PatientId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public string? Notes { get; set; }
    public string UploadedBy { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public bool IsRestricted { get; set; }

    public Patient Patient { get; set; } = default!;
}
