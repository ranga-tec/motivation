using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Poms.Domain.Enums;

namespace Poms.Web.Api.V1.Contracts;

public sealed record EpisodeOption(int Id, string Name);
public sealed record EpisodeOptionsResponse(IReadOnlyList<EpisodeOption> Centers, IReadOnlyList<string> Statuses);

public sealed record EpisodeResponse(
    Guid Id,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    int CenterId,
    string CenterName,
    string Status,
    DateOnly RecordDate,
    TimeOnly? RecordTime,
    string? Remarks,
    bool IsRestricted,
    int AssessmentCount,
    int FittingCount,
    int DeliveryCount,
    int FollowUpCount,
    int DocumentCount);

public sealed record SaveEpisodeRequest : IValidatableObject
{
    public Guid PatientId { get; init; }
    [Range(1, int.MaxValue)] public int CenterId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))] public RecordStatus Status { get; init; } = RecordStatus.Active;
    public DateOnly RecordDate { get; init; }
    public TimeOnly? RecordTime { get; init; }
    [StringLength(1000)] public string? Remarks { get; init; }
    public bool IsRestricted { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PatientId == Guid.Empty)
            yield return new("Select a patient.", [nameof(PatientId)]);
        if (RecordDate == default)
            yield return new("Select a record date.", [nameof(RecordDate)]);
        if (!RecordTime.HasValue)
            yield return new("Select a record time.", [nameof(RecordTime)]);
    }
}
