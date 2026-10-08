using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Poms.Domain.Enums;

namespace Poms.Web.Api.V1.Contracts;

public sealed record ClinicalOption(int Id, string Name);
public sealed record PrescriptionOptionResponse(string Code, string Label, IReadOnlyList<string> SubTypes);
public sealed record ClinicalOptionsResponse(
    IReadOnlyList<ClinicalOption> MainProblemTypes,
    IReadOnlyList<ClinicalOption> CauseReasonTypes,
    IReadOnlyList<ClinicalOption> Devices,
    IReadOnlyList<string> AssessmentTypes,
    IReadOnlyList<string> LimbCategories,
    IReadOnlyList<string> Sides);

public sealed record PrescriptionResponse(Guid Id, string Side, string Code, string Label, string? SubType, string? OtherText);
public sealed record AssessmentResponse(Guid Id, Guid EpisodeId, string AssessmentType, string LimbCategory, DateOnly AssessedOn, TimeOnly? StartTime, TimeOnly? EndTime, int MainProblemTypeId, string MainProblemType, string Side, int CauseReasonTypeId, string CauseReasonType, string? CauseReasonOther, string? AdditionalInformation, bool IsRestricted, IReadOnlyList<PrescriptionResponse> Prescriptions);
public sealed record FittingResponse(Guid Id, Guid EpisodeId, DateOnly FittingDate, string? Notes, bool IsRestricted);
public sealed record DeliveryResponse(Guid Id, Guid EpisodeId, DateOnly DeliveryDate, TimeOnly? DeliveryTime, string? Notes, int? DeviceId, string? DeviceName, bool IsRestricted);
public sealed record FollowUpResponse(Guid Id, Guid EpisodeId, DateOnly FollowUpDate, TimeOnly? StartTime, TimeOnly? EndTime, string? Notes, bool IsRestricted);
public sealed record EpisodeClinicalRecordsResponse(IReadOnlyList<AssessmentResponse> Assessments, IReadOnlyList<FittingResponse> Fittings, IReadOnlyList<DeliveryResponse> Deliveries, IReadOnlyList<FollowUpResponse> FollowUps);

public sealed record PrescriptionRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] Side Side,
    [property: Required, StringLength(100)] string Code,
    [property: StringLength(100)] string? SubType,
    [property: StringLength(500)] string? OtherText);

public sealed record SaveAssessmentRequest : IValidatableObject
{
    public Guid EpisodeId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))] public AssessmentType AssessmentType { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))] public LimbCategory LimbCategory { get; init; }
    public DateOnly AssessedOn { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    [Range(1, int.MaxValue)] public int MainProblemTypeId { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))] public Side Side { get; init; }
    [Range(1, int.MaxValue)] public int CauseReasonTypeId { get; init; }
    [StringLength(500)] public string? CauseReasonOther { get; init; }
    [StringLength(2000)] public string? AdditionalInformation { get; init; }
    public bool IsRestricted { get; init; }
    [MinLength(1)] public IReadOnlyList<PrescriptionRequest> Prescriptions { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EpisodeId == Guid.Empty) yield return new("Select a patient record.", [nameof(EpisodeId)]);
        if (AssessedOn == default) yield return new("Select an assessment date.", [nameof(AssessedOn)]);
        if (!StartTime.HasValue) yield return new("Select a start time.", [nameof(StartTime)]);
        if (!EndTime.HasValue) yield return new("Select an end time.", [nameof(EndTime)]);
        if (StartTime.HasValue && EndTime.HasValue && EndTime <= StartTime) yield return new("The assessment end time must be later than the start time.", [nameof(EndTime)]);
        if (AssessmentType == AssessmentType.Prosthetic && LimbCategory == LimbCategory.Spinal) yield return new("Spinal is only valid for Orthotic assessments.", [nameof(LimbCategory)]);
        var requiredSides = Side == Poms.Domain.Enums.Side.Bilateral
            ? new[] { Poms.Domain.Enums.Side.Left, Poms.Domain.Enums.Side.Right }
            : new[] { Side };
        foreach (var requiredSide in requiredSides)
            if (!Prescriptions.Any(item => item.Side == requiredSide)) yield return new($"A prescription for {requiredSide} is required.", [nameof(Prescriptions)]);
        if (Prescriptions.Any(item => item.Code == "OTHER" && string.IsNullOrWhiteSpace(item.OtherText))) yield return new("Specify every 'Other' prescription.", [nameof(Prescriptions)]);
    }
}

public sealed record SaveFittingRequest(Guid EpisodeId, DateOnly FittingDate, [property: StringLength(2000)] string? Notes, bool IsRestricted);
public sealed record SaveDeliveryRequest(Guid EpisodeId, DateOnly DeliveryDate, TimeOnly? DeliveryTime, [property: StringLength(2000)] string? Notes, int? DeviceId, bool IsRestricted);
public sealed record SaveFollowUpRequest : IValidatableObject
{
    public Guid EpisodeId { get; init; }
    public DateOnly FollowUpDate { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    [StringLength(2000)] public string? Notes { get; init; }
    public bool IsRestricted { get; init; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EpisodeId == Guid.Empty) yield return new("Select a patient record.", [nameof(EpisodeId)]);
        if (FollowUpDate == default) yield return new("Select a follow-up date.", [nameof(FollowUpDate)]);
        if (!StartTime.HasValue) yield return new("Select a start time.", [nameof(StartTime)]);
        if (!EndTime.HasValue) yield return new("Select an end time.", [nameof(EndTime)]);
        if (StartTime.HasValue && EndTime.HasValue && EndTime <= StartTime) yield return new("The follow-up end time must be later than the start time.", [nameof(EndTime)]);
    }
}
