using Microsoft.AspNetCore.Http;

namespace Poms.Infrastructure.Services;

public static class PatientPhotoValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public sealed record Result(bool IsValid, string? ContentType = null, string? Error = null);

    public static async Task<Result> ValidateAsync(IFormFile? file)
    {
        if (file is null) return new Result(true);

        if (file.Length == 0)
            return new Result(false, Error: "Choose a non-empty image file.");

        if (file.Length > MaxFileSizeBytes)
            return new Result(false, Error: "The patient photo must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png")
            return new Result(false, Error: "Use a JPG or PNG image.");

        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header);

        var isJpeg = bytesRead >= 3 &&
                     header[0] == 0xFF &&
                     header[1] == 0xD8 &&
                     header[2] == 0xFF;
        var isPng = bytesRead >= 8 &&
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A;

        if (isJpeg && extension is ".jpg" or ".jpeg")
            return new Result(true, "image/jpeg");

        if (isPng && extension == ".png")
            return new Result(true, "image/png");

        return new Result(false, Error: "The selected file is not a valid JPG or PNG image.");
    }
}
