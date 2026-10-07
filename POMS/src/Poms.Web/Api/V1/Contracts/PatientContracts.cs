using System.ComponentModel.DataAnnotations;

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
