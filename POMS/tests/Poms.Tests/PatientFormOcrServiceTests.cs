using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Poms.Infrastructure.Services;

namespace Poms.Tests;

public class PatientFormOcrServiceTests
{
    [Fact]
    public async Task ExtractAsync_SendsPrivateStructuredImageRequest_AndReadsDraft()
    {
        string? requestJson = null;
        var extractedJson = JsonSerializer.Serialize(new
        {
            patient_file_number = "PH/26/319",
            centre = (string?)null,
            full_name = "Mohamed Lukman Ruqaiya",
            preferred_name = "Lukman",
            date_of_birth = "2020-10-26",
            identification_number = (string?)null,
            gender = "Female",
            employment = (string?)null,
            address = "270/6 Poruthota, Kochchikade",
            district = "Gampaha",
            city = "Kochchikade",
            email = (string?)null,
            referral_source = "Ayati",
            travel_time_distance = "01 hours",
            guardian_name = "Lukman",
            guardian_relationship = "Father",
            guardian_address = "270/6 Poruthota, Kochchikade",
            guardian_phone = "0778548589",
            guardian_mobile = "0751334161",
            contacts = Array.Empty<object>(),
            overall_confidence = 0.86,
            warnings = new[] { "Centre is blank." },
            raw_transcription = "Full Name: Mohamed Lukman Ruqaiya"
        });
        var apiResponse = JsonSerializer.Serialize(new
        {
            output = new[]
            {
                new
                {
                    type = "message",
                    content = new[] { new { type = "output_text", text = extractedJson } }
                }
            }
        });
        var handler = new StubHttpMessageHandler(async request =>
        {
            requestJson = await request.Content!.ReadAsStringAsync();
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("test-key");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(apiResponse, Encoding.UTF8, "application/json")
            };
        });
        var service = CreateService(handler);

        var result = await service.ExtractAsync(
            new MemoryStream(new byte[] { 1, 2, 3 }),
            "image/png");

        result.FullName.Should().Be("Mohamed Lukman Ruqaiya");
        result.Gender.Should().Be("Female");
        result.Warnings.Should().ContainSingle("Centre is blank.");

        using var sent = JsonDocument.Parse(requestJson!);
        sent.RootElement.GetProperty("store").GetBoolean().Should().BeFalse();
        sent.RootElement.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean().Should().BeTrue();
        var content = sent.RootElement.GetProperty("input")[0].GetProperty("content");
        content[1].GetProperty("image_url").GetString().Should().Be("data:image/png;base64,AQID");
    }

    [Fact]
    public async Task ExtractAsync_WhenNotConfigured_StopsBeforeNetworkCall()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("Network should not be called."));
        var service = CreateService(handler, apiKey: null);

        var action = () => service.ExtractAsync(
            new MemoryStream(new byte[] { 1 }),
            "image/png");

        await action.Should().ThrowAsync<PatientFormOcrException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task ImageValidator_AcceptsPngSignature_AndRejectsExtensionMismatch()
    {
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0 };
        var valid = CreateFormFile(pngBytes, "form.png", "image/png");
        var mismatch = CreateFormFile(pngBytes, "form.jpg", "image/jpeg");

        var validResult = await PatientFormImageValidator.ValidateAsync(valid);
        var mismatchResult = await PatientFormImageValidator.ValidateAsync(mismatch);

        validResult.IsValid.Should().BeTrue();
        validResult.ContentType.Should().Be("image/png");
        mismatchResult.IsValid.Should().BeFalse();
    }

    private static OpenAiPatientFormOcrService CreateService(
        HttpMessageHandler handler,
        string? apiKey = "test-key") =>
        new(
            new HttpClient(handler),
            Options.Create(new PatientFormOcrOptions
            {
                Endpoint = "https://example.test/v1/responses",
                Model = "test-model",
                ApiKey = apiKey
            }),
            NullLogger<OpenAiPatientFormOcrService>.Instance);

    private static IFormFile CreateFormFile(byte[] bytes, string fileName, string contentType)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "FormImage", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responseFactory(request);
    }
}
