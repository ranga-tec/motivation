using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Poms.Domain.Enums;

namespace Poms.Web.Api.V1.Contracts;

public sealed class PatientListQuery
{
    [StringLength(200)]
    public string? Search { get; init; }

    public int? CenterId { get; init; }
    public int? ProvinceId { get; init; }
    public int? DistrictId { get; init; }
    public int? CityId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record PatientSummaryResponse(
    Guid Id,
    string PatientNumber,
    string FullName,
    string NameWithInitials,
    DateOnly DateOfBirth,
    string Sex,
    string Category,
    int CenterId,
    string CenterName,
    DateOnly RegistrationDate);

public sealed record PatientContactResponse(
    Guid Id,
    string TelephoneNumber,
    DateOnly? DateConfirmed,
    string? PersonChecked);

public sealed record PatientDetailResponse(
    Guid Id,
    string PatientNumber,
    string FullName,
    string NameWithInitials,
    DateOnly DateOfBirth,
    string Sex,
    string Category,
    string IdentificationType,
    string IdentificationNumber,
    string Address1,
    string? Address2,
    string Province,
    string District,
    string? City,
    string? CityOther,
    string? Email,
    string? Nationality,
    int CenterId,
    string Center,
    DateOnly RegistrationDate,
    string? AssignedClinicianName,
    IReadOnlyList<PatientContactResponse> Contacts);

public sealed record CreatePatientRequest : IValidatableObject
{
    [Required, StringLength(200)] public string FullName { get; init; } = string.Empty;
    [Required, StringLength(100)] public string NameWithInitials { get; init; } = string.Empty;
    public DateOnly DateOfBirth { get; init; }
    [Required, JsonConverter(typeof(JsonStringEnumConverter))] public Sex? Sex { get; init; }
    [StringLength(200)] public string? Employment { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))] public PatientCategory Category { get; init; } = PatientCategory.Local;
    [StringLength(100)] public string? Nationality { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))] public IdentificationType IdentificationType { get; init; }
    [StringLength(50)] public string? IdentificationNumber { get; init; }
    [Required, StringLength(300)] public string Address1 { get; init; } = string.Empty;
    [StringLength(300)] public string? Address2 { get; init; }
    [Range(1, int.MaxValue)] public int ProvinceId { get; init; }
    [Range(1, int.MaxValue)] public int DistrictId { get; init; }
    public int? CityId { get; init; }
    [StringLength(100)] public string? CityOther { get; init; }
    [StringLength(256)] public string? Email { get; init; }
    public int? ReferralSourceId { get; init; }
    [StringLength(150)] public string? ReferralSourceOther { get; init; }
    [StringLength(150)] public string? ReferralPersonName { get; init; }
    [StringLength(30)] public string? ReferralPersonContactNumber { get; init; }
    [StringLength(100)] public string? TravelTimeDistance { get; init; }
    [Range(1, int.MaxValue)] public int CenterId { get; init; }
    public DateOnly? RegistrationDate { get; init; }
    [StringLength(1000)] public string? Remarks { get; init; }
    [Required, StringLength(400)] public string AssignedClinicianEntry { get; init; } = string.Empty;
    public string? AssignedClinicianUserId { get; init; }
    [Required, StringLength(100)] public string GuardianName { get; init; } = string.Empty;
    [Required, StringLength(50)] public string GuardianRelationship { get; init; } = string.Empty;
    [StringLength(300)] public string? GuardianAddress { get; init; }
    [StringLength(30)] public string? GuardianPhone { get; init; }
    [StringLength(30)] public string? GuardianMobile { get; init; }
    public bool ConfirmPossibleDuplicate { get; init; }
    public IReadOnlyList<CreatePatientContactRequest> Contacts { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var latestAllowed = DateOnly.FromDateTime(DateTime.Today.AddDays(-3));
        if (DateOfBirth == default || DateOfBirth > latestAllowed)
            yield return new($"Date of birth must be on or before {latestAllowed:dd-MMM-yyyy}.", [nameof(DateOfBirth)]);
        if (Category == PatientCategory.Foreign && string.IsNullOrWhiteSpace(Nationality))
            yield return new("Nationality is required for foreign patients.", [nameof(Nationality)]);
        if (CityId is null && string.IsNullOrWhiteSpace(CityOther))
            yield return new("Select a city or enter one if not listed.", [nameof(CityOther)]);
        if (IdentificationType != IdentificationType.NotApplicable && string.IsNullOrWhiteSpace(IdentificationNumber))
            yield return new("Identification number is required unless the identification type is N/A.", [nameof(IdentificationNumber)]);
        if (!string.IsNullOrWhiteSpace(Email) && !IsNotApplicable(Email) && !new EmailAddressAttribute().IsValid(Email))
            yield return new("Enter a valid email address, or N/A if the patient has none.", [nameof(Email)]);
    }

    private static bool IsNotApplicable(string value)
    {
        var normalized = value.Trim().TrimEnd('.');
        return normalized.Equals("N/A", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("NA", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("N/A/", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record CreatePatientContactRequest(
    [param: Required, StringLength(30)] string TelephoneNumber,
    DateOnly? DateConfirmed,
    [param: StringLength(100)] string? PersonChecked);

public sealed record RegistrationOption(int Id, string Name, int? ParentId = null);
public sealed record AssigneeOption(string UserId, string DisplayName, string FullName, bool IsPreferred);
public sealed record PatientRegistrationOptionsResponse(
    IReadOnlyList<RegistrationOption> Provinces,
    IReadOnlyList<RegistrationOption> Districts,
    IReadOnlyList<RegistrationOption> Cities,
    IReadOnlyList<RegistrationOption> Centers,
    IReadOnlyList<RegistrationOption> ReferralSources,
    IReadOnlyList<AssigneeOption> Assignees);
