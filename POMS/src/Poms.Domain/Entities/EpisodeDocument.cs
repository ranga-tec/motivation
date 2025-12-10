// ============================================================================
// Poms.Domain/Entities/EpisodeDocument.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;

public class EpisodeDocument : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public string Title { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string StoragePath { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long FileSize { get; set; }
    public string? TagsJson { get; set; }
    public string? Remarks { get; set; }
    public string UploadedBy { get; set; } = default!;
    public DateTime UploadedAt { get; set; }

    public Episode Episode { get; set; } = default!;
}
