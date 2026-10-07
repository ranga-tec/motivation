namespace Poms.Web.Services;

public static class StoragePathValidator
{
    public static void Validate(
        string fileStoragePath,
        string dataProtectionKeysPath,
        bool isProduction,
        bool allowEphemeralStorage)
    {
        ValidatePath(fileStoragePath, "FileStorage:RootPath", isProduction, allowEphemeralStorage);
        ValidatePath(dataProtectionKeysPath, "DataProtection:KeysPath", isProduction, allowEphemeralStorage);
    }

    private static void ValidatePath(
        string configuredPath,
        string configurationKey,
        bool isProduction,
        bool allowEphemeralStorage)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException($"{configurationKey} must be configured.");

        var fullPath = Path.GetFullPath(configuredPath);
        if (isProduction && !Path.IsPathRooted(configuredPath))
            throw new InvalidOperationException($"{configurationKey} must be an absolute path in production.");

        if (isProduction && !allowEphemeralStorage && IsTemporaryPath(fullPath))
        {
            throw new InvalidOperationException(
                $"{configurationKey} points to temporary storage. Mount persistent storage or set " +
                "Storage:AllowEphemeral=true only for a disposable demo deployment.");
        }

        Directory.CreateDirectory(fullPath);
        var probePath = Path.Combine(fullPath, $".poms-write-probe-{Guid.NewGuid():N}");
        try
        {
            File.WriteAllText(probePath, string.Empty);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"{configurationKey} is not writable: {fullPath}", exception);
        }
        finally
        {
            if (File.Exists(probePath))
                File.Delete(probePath);
        }
    }

    private static bool IsTemporaryPath(string fullPath)
    {
        var normalized = fullPath.Replace('\\', '/').TrimEnd('/') + "/";
        var systemTemporaryPath = Path.GetFullPath(Path.GetTempPath())
            .Replace('\\', '/')
            .TrimEnd('/') + "/";

        return normalized.StartsWith(systemTemporaryPath, StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("/tmp/", StringComparison.Ordinal)
            || normalized.StartsWith("/var/tmp/", StringComparison.Ordinal);
    }
}
