// ============================================================================
// Poms.Domain/Entities/EpisodeDocument.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;
using Poms.Domain.Enums;

public class EpisodeDocument : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileName { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSize { get; set; }
    public string? Notes { get; set; }
    public string UploadedBy { get; set; } = default!;
    public DateTime UploadedAt { get; set; }
    public bool IsRestricted { get; set; }

    public Episode Episode { get; set; } = default!;
}
