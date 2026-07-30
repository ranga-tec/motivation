using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Poms.Infrastructure.Services;

namespace Poms.Tests;

public class PatientPhotoValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidPng_ReturnsCanonicalContentType()
    {
        var bytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D
        };
        var file = CreateFile(bytes, "patient.png", "image/png");

        var result = await PatientPhotoValidator.ValidateAsync(file);

        result.IsValid.Should().BeTrue();
        result.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task ValidateAsync_SpoofedPng_IsRejected()
    {
        var file = CreateFile("not an image"u8.ToArray(), "patient.png", "image/png");

        var result = await PatientPhotoValidator.ValidateAsync(file);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not a valid");
    }

    [Fact]
    public async Task ValidateAsync_FileOverFiveMegabytes_IsRejected()
    {
        var bytes = new byte[PatientPhotoValidator.MaxFileSizeBytes + 1];
        var file = CreateFile(bytes, "patient.jpg", "image/jpeg");

        var result = await PatientPhotoValidator.ValidateAsync(file);

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("5 MB");
    }

    private static FormFile CreateFile(byte[] bytes, string fileName, string contentType)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "ProfilePhoto", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
