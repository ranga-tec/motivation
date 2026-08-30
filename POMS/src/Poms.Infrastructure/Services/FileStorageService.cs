using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Poms.Infrastructure.Services;

public sealed record StagedOcrImport(string Token, string FileName, string ContentType);
public sealed record StoredOcrImport(string StoragePath, string FileName, string ContentType);

public interface IFileStorageService
{
    Task<(string StoragePath, string FileName)> SaveFileAsync(IFormFile file, string patientNumber);
    Task<StagedOcrImport> StageOcrImportAsync(
        IFormFile file,
        string contentType,
        CancellationToken cancellationToken = default);
    Task<StoredOcrImport> MaterializeOcrImportAsync(
        string token,
        string patientNumber,
        CancellationToken cancellationToken = default);
    Task DeleteStagedOcrImportAsync(string token);
    Task CleanupExpiredOcrImportsAsync();
    Task<byte[]> GetFileAsync(string storagePath);
    Task DeleteFileAsync(string storagePath);
}

public class FileStorageService : IFileStorageService
{
    private static readonly TimeSpan StagingLifetime = TimeSpan.FromHours(24);
    private readonly string _rootPath;
    private readonly long _maxFileSizeBytes;
    private readonly string[] _allowedExtensions;
    private readonly string _stagingPath;

    public FileStorageService(string rootPath, long maxFileSizeMB = 10, string[]? allowedExtensions = null)
    {
        _rootPath = Path.GetFullPath(rootPath);
        _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
        _allowedExtensions = allowedExtensions ?? new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };
        _stagingPath = Path.Combine(_rootPath, "ocr-imports");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<(string StoragePath, string FileName)> SaveFileAsync(IFormFile file, string patientNumber)
    {
        ValidateFile(file);
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var now = DateTime.UtcNow;
        var yearMonth = $"{now.Year}/{now.Month:D2}";
        var directory = GetContainedPath(Path.Combine("patients", patientNumber, yearMonth));
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(directory, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
        {
            await file.CopyToAsync(stream);
        }

        return (Path.Combine("patients", patientNumber, yearMonth, fileName), Path.GetFileName(file.FileName));
    }

    public async Task<StagedOcrImport> StageOcrImportAsync(
        IFormFile file,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file);
        Directory.CreateDirectory(_stagingPath);
        await CleanupExpiredOcrImportsAsync();
        var token = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var dataFileName = $"{token}{extension}";
        var dataPath = Path.Combine(_stagingPath, dataFileName);
        var metadataPath = Path.Combine(_stagingPath, $"{token}.json");
        var safeOriginalName = Path.GetFileName(file.FileName);
        var metadata = new OcrImportMetadata(safeOriginalName, contentType, dataFileName, DateTime.UtcNow);

        try
        {
            await using (var output = new FileStream(dataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                await file.CopyToAsync(output, cancellationToken);
            }

            await using var metadataStream = new FileStream(metadataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await JsonSerializer.SerializeAsync(metadataStream, metadata, cancellationToken: cancellationToken);
            return new StagedOcrImport(token, safeOriginalName, contentType);
        }
        catch
        {
            TryDelete(dataPath);
            TryDelete(metadataPath);
            throw;
        }
    }

    public async Task<StoredOcrImport> MaterializeOcrImportAsync(
        string token,
        string patientNumber,
        CancellationToken cancellationToken = default)
    {
        await CleanupExpiredOcrImportsAsync();
        var normalizedToken = NormalizeToken(token);
        var metadataPath = Path.Combine(_stagingPath, $"{normalizedToken}.json");
        if (!File.Exists(metadataPath))
            throw new InvalidOperationException("The staged OCR scan has expired or is invalid. Upload the scan again.");

        OcrImportMetadata metadata;
        await using (var metadataStream = File.OpenRead(metadataPath))
        {
            metadata = await JsonSerializer.DeserializeAsync<OcrImportMetadata>(metadataStream, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("The staged OCR scan metadata is invalid.");
        }

        if (!string.Equals(Path.GetFileNameWithoutExtension(metadata.DataFileName), normalizedToken, StringComparison.Ordinal))
            throw new InvalidOperationException("The staged OCR scan metadata is invalid.");

        var sourcePath = Path.Combine(_stagingPath, metadata.DataFileName);
        if (!File.Exists(sourcePath))
            throw new InvalidOperationException("The staged OCR scan has expired. Upload the scan again.");

        var now = DateTime.UtcNow;
        var yearMonth = $"{now.Year}/{now.Month:D2}";
        var directory = GetContainedPath(Path.Combine("patients", patientNumber, yearMonth));
        Directory.CreateDirectory(directory);
        var extension = Path.GetExtension(metadata.DataFileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var targetPath = Path.Combine(directory, storedFileName);

        await using (var source = File.OpenRead(sourcePath))
        await using (var target = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
        {
            await source.CopyToAsync(target, cancellationToken);
        }

        return new StoredOcrImport(
            Path.Combine("patients", patientNumber, yearMonth, storedFileName),
            metadata.FileName,
            metadata.ContentType);
    }

    public Task DeleteStagedOcrImportAsync(string token)
    {
        var normalizedToken = NormalizeToken(token);
        var metadataPath = Path.Combine(_stagingPath, $"{normalizedToken}.json");
        if (File.Exists(metadataPath))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<OcrImportMetadata>(File.ReadAllText(metadataPath));
                if (metadata is not null &&
                    string.Equals(Path.GetFileNameWithoutExtension(metadata.DataFileName), normalizedToken, StringComparison.Ordinal))
                {
                    TryDelete(Path.Combine(_stagingPath, metadata.DataFileName));
                }
            }
            finally
            {
                TryDelete(metadataPath);
            }
        }

        return Task.CompletedTask;
    }

    public Task CleanupExpiredOcrImportsAsync()
    {
        if (!Directory.Exists(_stagingPath))
            return Task.CompletedTask;

        foreach (var metadataPath in Directory.EnumerateFiles(_stagingPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<OcrImportMetadata>(File.ReadAllText(metadataPath));
                if (metadata is null || metadata.CreatedAtUtc >= DateTime.UtcNow.Subtract(StagingLifetime))
                    continue;
                TryDelete(Path.Combine(_stagingPath, metadata.DataFileName));
                TryDelete(metadataPath);
            }
            catch
            {
                // Invalid metadata remains isolated from patient storage and can be removed by maintenance.
            }
        }

        return Task.CompletedTask;
    }

    public async Task<byte[]> GetFileAsync(string storagePath)
    {
        var fullPath = GetContainedPath(storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", storagePath);
        return await File.ReadAllBytesAsync(fullPath);
    }

    public Task DeleteFileAsync(string storagePath)
    {
        TryDelete(GetContainedPath(storagePath));
        return Task.CompletedTask;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length > _maxFileSizeBytes)
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSizeBytes / 1024 / 1024}MB");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            throw new InvalidOperationException($"File type {extension} is not allowed");
    }

    private static string NormalizeToken(string token)
    {
        if (!Guid.TryParseExact(token, "N", out var parsed))
            throw new InvalidOperationException("The staged OCR scan token is invalid.");
        return parsed.ToString("N");
    }

    private string GetContainedPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        var rootWithSeparator = _rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The storage path is invalid.");
        return fullPath;
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private sealed record OcrImportMetadata(
        string FileName,
        string ContentType,
        string DataFileName,
        DateTime CreatedAtUtc);
}
