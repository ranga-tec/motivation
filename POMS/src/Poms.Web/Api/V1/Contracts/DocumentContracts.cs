using Poms.Domain.Enums;

namespace Poms.Web.Api.V1.Contracts;

public sealed record DocumentOptionsResponse(IReadOnlyList<string> DocumentTypes, long MaxFileSizeMb, IReadOnlyList<string> AllowedExtensions);
public sealed record DocumentResponse(Guid Id, string Scope, Guid OwnerId, string DocumentType, string FileName, string ContentType, long? FileSize, string? Notes, string UploadedBy, DateTime UploadedAt, bool IsRestricted);

public sealed class UploadDocumentRequest
{
    public Guid? PatientId { get; init; }
    public Guid? EpisodeId { get; init; }
    public DocumentType DocumentType { get; init; }
    public string? Notes { get; init; }
    public bool IsRestricted { get; init; }
    public IFormFile File { get; init; } = default!;
}
