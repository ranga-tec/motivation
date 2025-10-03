using Microsoft.AspNetCore.Http;

namespace Poms.Infrastructure.Services;

public interface IFileStorageService
{
    Task<(string StoragePath, string FileName)> SaveFileAsync(IFormFile file, string patientNumber);
    Task<byte[]> GetFileAsync(string storagePath);
    Task DeleteFileAsync(string storagePath);
}

public class FileStorageService : IFileStorageService
{
    private readonly string _rootPath;
    private readonly long _maxFileSizeBytes;
    private readonly string[] _allowedExtensions;

    public FileStorageService(string rootPath, long maxFileSizeMB = 10, string[]? allowedExtensions = null)
    {
        _rootPath = rootPath;
        _maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
        _allowedExtensions = allowedExtensions ?? new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };

        if (!Directory.Exists(_rootPath))
            Directory.CreateDirectory(_rootPath);
    }

    public async Task<(string StoragePath, string FileName)> SaveFileAsync(IFormFile file, string patientNumber)
    {
        if (file.Length > _maxFileSizeBytes)
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {_maxFileSizeBytes / 1024 / 1024}MB");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
            throw new InvalidOperationException($"File type {extension} is not allowed");

        var now = DateTime.UtcNow;
        var yearMonth = $"{now.Year}/{now.Month:D2}";
        var directory = Path.Combine(_rootPath, "patients", patientNumber, yearMonth);
        
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(directory, fileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = Path.Combine("patients", patientNumber, yearMonth, fileName);
        return (relativePath, file.FileName);
    }

    public async Task<byte[]> GetFileAsync(string storagePath)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", storagePath);

        return await File.ReadAllBytesAsync(fullPath);
    }

    public Task DeleteFileAsync(string storagePath)
    {
        var fullPath = Path.Combine(_rootPath, storagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
