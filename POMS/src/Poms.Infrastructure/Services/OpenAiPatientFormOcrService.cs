using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Poms.Infrastructure.Services;

public sealed class OpenAiPatientFormOcrService : IPatientFormOcrEngine
{
    private const string ExtractionInstructions = """
        Extract the handwritten and printed values from this Exceed Lanka patient registration form.
        Return only the requested structured data. Never guess a value that is not legible.
        Use null for blank fields and for values below roughly 65% confidence, then explain each
        uncertain or omitted value in warnings. Preserve names, addresses, identifiers, and phone
        numbers as written, while removing label text. Convert dates to YYYY-MM-DD only when the
        complete date is clear. Gender must be Male, Female, Other, or null. Contacts refers only
        to rows in the Telephone No./Date Confirmed/Person Checked table; guardian phone and mobile
        belong in their dedicated fields. Transcribe all visible filled values in raw_transcription.
        This output is a draft for human review and must not claim that the data is verified.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonElement OutputSchema = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "patient_file_number": { "type": ["string", "null"] },
            "centre": { "type": ["string", "null"] },
            "full_name": { "type": ["string", "null"] },
            "preferred_name": { "type": ["string", "null"] },
            "date_of_birth": { "type": ["string", "null"] },
            "identification_number": { "type": ["string", "null"] },
            "gender": { "type": ["string", "null"], "enum": ["Male", "Female", "Other", null] },
            "employment": { "type": ["string", "null"] },
            "address": { "type": ["string", "null"] },
            "district": { "type": ["string", "null"] },
            "city": { "type": ["string", "null"] },
            "email": { "type": ["string", "null"] },
            "referral_source": { "type": ["string", "null"] },
            "travel_time_distance": { "type": ["string", "null"] },
            "guardian_name": { "type": ["string", "null"] },
            "guardian_relationship": { "type": ["string", "null"] },
            "guardian_address": { "type": ["string", "null"] },
            "guardian_phone": { "type": ["string", "null"] },
            "guardian_mobile": { "type": ["string", "null"] },
            "contacts": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "telephone_number": { "type": ["string", "null"] },
                  "date_confirmed": { "type": ["string", "null"] },
                  "person_checked": { "type": ["string", "null"] }
                },
                "required": ["telephone_number", "date_confirmed", "person_checked"],
                "additionalProperties": false
              }
            },
            "overall_confidence": { "type": "number", "minimum": 0, "maximum": 1 },
            "warnings": { "type": "array", "items": { "type": "string" } },
            "raw_transcription": { "type": ["string", "null"] }
          },
          "required": [
            "patient_file_number", "centre", "full_name", "preferred_name", "date_of_birth",
            "identification_number", "gender", "employment", "address", "district", "city",
            "email", "referral_source", "travel_time_distance", "guardian_name",
            "guardian_relationship", "guardian_address", "guardian_phone", "guardian_mobile",
            "contacts", "overall_confidence", "warnings", "raw_transcription"
          ],
          "additionalProperties": false
        }
        """).RootElement.Clone();

    private readonly HttpClient _httpClient;
    private readonly PatientFormOcrOptions _options;
    private readonly ILogger<OpenAiPatientFormOcrService> _logger;

    public OpenAiPatientFormOcrService(
        HttpClient httpClient,
        IOptions<PatientFormOcrOptions> options,
        ILogger<OpenAiPatientFormOcrService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        _options.Enabled &&
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out _);

    public PatientFormOcrProvider Provider => PatientFormOcrProvider.OpenAi;
    public string DisplayName => "OpenAI vision";
    public string Description => "Best option for handwriting; uses the configured OpenAI API and incurs usage cost.";

    public async Task<PatientFormOcrResult> ExtractAsync(
        Stream image,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new PatientFormOcrException("Patient form OCR is not configured.");

        if (contentType is not "image/jpeg" and not "image/png")
            throw new PatientFormOcrException("Patient form OCR accepts only JPG and PNG images.");

        await using var buffer = new MemoryStream();
        await image.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0)
            throw new PatientFormOcrException("The scanned form is empty.");
        if (buffer.Length > PatientFormImageValidator.MaxFileSizeBytes)
            throw new PatientFormOcrException("The scanned form must be 10 MB or smaller.");

        var imageUrl = $"data:{contentType};base64,{Convert.ToBase64String(buffer.ToArray())}";
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["store"] = false,
            ["input"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["role"] = "user",
                    ["content"] = new object[]
                    {
                        new { type = "input_text", text = ExtractionInstructions },
                        new { type = "input_image", image_url = imageUrl, detail = "high" }
                    }
                }
            },
            ["text"] = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "patient_registration_form",
                    strict = true,
                    schema = OutputSchema
                }
            },
            ["max_output_tokens"] = 2500
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new PatientFormOcrException("OCR timed out. Try the scan again.");
        }
        catch (HttpRequestException ex)
        {
            throw new PatientFormOcrException("OCR could not reach the configured vision service.", ex);
        }

        using (response)
        {
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var requestId = response.Headers.TryGetValues("x-request-id", out var values)
                ? values.FirstOrDefault()
                : null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Patient form OCR failed with HTTP {StatusCode}; request {RequestId}",
                    (int)response.StatusCode,
                    requestId);
                throw new PatientFormOcrException(
                    $"OCR service returned HTTP {(int)response.StatusCode}. Try again or contact an administrator.");
            }

            try
            {
                var outputText = ReadOutputText(responseJson);
                var result = JsonSerializer.Deserialize<PatientFormOcrResult>(outputText, JsonOptions)
                    ?? throw new JsonException("OCR output was empty.");
                result.Contacts ??= [];
                result.Warnings ??= [];
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Patient form OCR returned an unreadable response; request {RequestId}", requestId);
                throw new PatientFormOcrException("OCR returned an unreadable result. Try the scan again.", ex);
            }
        }
    }

    private static string ReadOutputText(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        if (!document.RootElement.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Missing response output.");
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var itemType) ||
                itemType.GetString() != "message" ||
                !item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("type", out var partType) &&
                    partType.GetString() == "output_text" &&
                    part.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? throw new JsonException("Empty response text.");
                }
            }
        }

        throw new JsonException("Missing OCR response text.");
    }
}
