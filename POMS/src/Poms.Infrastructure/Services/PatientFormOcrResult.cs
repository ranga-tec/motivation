using System.Text.Json.Serialization;

namespace Poms.Infrastructure.Services;

public sealed class PatientFormOcrResult
{
    [JsonPropertyName("patient_file_number")]
    public string? PatientFileNumber { get; set; }

    [JsonPropertyName("centre")]
    public string? Centre { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("preferred_name")]
    public string? PreferredName { get; set; }

    [JsonPropertyName("date_of_birth")]
    public string? DateOfBirth { get; set; }

    [JsonPropertyName("identification_number")]
    public string? IdentificationNumber { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("employment")]
    public string? Employment { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("district")]
    public string? District { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("referral_source")]
    public string? ReferralSource { get; set; }

    [JsonPropertyName("travel_time_distance")]
    public string? TravelTimeDistance { get; set; }

    [JsonPropertyName("guardian_name")]
    public string? GuardianName { get; set; }

    [JsonPropertyName("guardian_relationship")]
    public string? GuardianRelationship { get; set; }

    [JsonPropertyName("guardian_address")]
    public string? GuardianAddress { get; set; }

    [JsonPropertyName("guardian_phone")]
    public string? GuardianPhone { get; set; }

    [JsonPropertyName("guardian_mobile")]
    public string? GuardianMobile { get; set; }

    [JsonPropertyName("contacts")]
    public List<PatientFormOcrContact> Contacts { get; set; } = [];

    [JsonPropertyName("overall_confidence")]
    public double OverallConfidence { get; set; }

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = [];

    [JsonPropertyName("raw_transcription")]
    public string? RawTranscription { get; set; }
}

public sealed class PatientFormOcrContact
{
    [JsonPropertyName("telephone_number")]
    public string? TelephoneNumber { get; set; }

    [JsonPropertyName("date_confirmed")]
    public string? DateConfirmed { get; set; }

    [JsonPropertyName("person_checked")]
    public string? PersonChecked { get; set; }
}
