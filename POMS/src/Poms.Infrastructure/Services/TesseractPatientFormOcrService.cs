using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Poms.Infrastructure.Services;

public sealed class TesseractPatientFormOcrService : IPatientFormOcrEngine
{
    private readonly OfflinePatientFormOcrOptions _options;
    private readonly ILogger<TesseractPatientFormOcrService> _logger;
    private readonly string? _executablePath;

    public TesseractPatientFormOcrService(
        IOptions<PatientFormOcrOptions> options,
        ILogger<TesseractPatientFormOcrService> logger)
    {
        _options = options.Value.Offline;
        _logger = logger;
        _executablePath = ResolveExecutable(_options.ExecutablePath);
    }

    public PatientFormOcrProvider Provider => PatientFormOcrProvider.Offline;
    public string DisplayName => "Offline OCR";
    public string Description => "Runs locally with no API charge; printed text is stronger than handwriting.";
    public bool IsConfigured => _options.Enabled && _executablePath is not null;

    public async Task<PatientFormOcrResult> ExtractAsync(
        Stream image,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new PatientFormOcrException("Offline OCR is not installed or enabled.");

        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            _ => throw new PatientFormOcrException("Offline OCR accepts only JPG and PNG images.")
        };

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            $"poms-patient-ocr-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var imagePath = Path.Combine(tempDirectory, $"form{extension}");

        try
        {
            await using (var target = new FileStream(
                imagePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous))
            {
                await image.CopyToAsync(target, cancellationToken);
                if (target.Length == 0)
                    throw new PatientFormOcrException("The scanned form is empty.");
                if (target.Length > PatientFormImageValidator.MaxFileSizeBytes)
                    throw new PatientFormOcrException("The scanned form must be 10 MB or smaller.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _executablePath!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(imagePath);
            startInfo.ArgumentList.Add("stdout");
            startInfo.ArgumentList.Add("-l");
            startInfo.ArgumentList.Add(_options.Language);
            startInfo.ArgumentList.Add("--psm");
            startInfo.ArgumentList.Add("6");
            startInfo.ArgumentList.Add("tsv");

            using var process = new Process { StartInfo = startInfo };
            try
            {
                process.Start();
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                throw new PatientFormOcrException("Offline OCR could not start Tesseract.", ex);
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 10, 300)));

            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw new PatientFormOcrException("Offline OCR timed out. Try a clearer, smaller scan.");
            }

            var output = await outputTask;
            var error = await errorTask;
            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Tesseract patient OCR failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                throw new PatientFormOcrException("Offline OCR could not read this scan.");
            }

            return TesseractPatientFormParser.Parse(output);
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static string? ResolveExecutable(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        if (Path.IsPathFullyQualified(configuredPath) ||
            configuredPath.Contains(Path.DirectorySeparatorChar) ||
            configuredPath.Contains(Path.AltDirectorySeparatorChar))
        {
            return File.Exists(configuredPath) ? Path.GetFullPath(configuredPath) : null;
        }

        var candidates = OperatingSystem.IsWindows()
            ? new[] { configuredPath, $"{configuredPath}.exe" }
            : new[] { configuredPath };
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var candidate in candidates)
            {
                var path = Path.Combine(directory, candidate);
                if (File.Exists(path))
                    return path;
            }
        }

        return null;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Process may have exited between the check and kill request.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // OS cleanup can remove an abandoned temporary OCR directory later.
        }
    }
}

public static partial class TesseractPatientFormParser
{
    private sealed record Word(
        int Block,
        int Paragraph,
        int Line,
        int Left,
        int Top,
        int Width,
        int Height,
        double Confidence,
        string Text);

    public static PatientFormOcrResult Parse(string tsv)
    {
        var rows = tsv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var pageWidth = 0;
        var pageHeight = 0;
        var words = new List<Word>();

        foreach (var rawRow in rows.Skip(1))
        {
            var columns = rawRow.TrimEnd('\r').Split('\t', 12);
            if (columns.Length < 11 || !int.TryParse(columns[0], out var level))
                continue;

            if (level == 1 && columns.Length >= 10)
            {
                int.TryParse(columns[8], out pageWidth);
                int.TryParse(columns[9], out pageHeight);
                continue;
            }

            if (level != 5 || columns.Length < 12 || string.IsNullOrWhiteSpace(columns[11]))
                continue;

            if (!TryInt(columns[2], out var block) ||
                !TryInt(columns[3], out var paragraph) ||
                !TryInt(columns[4], out var line) ||
                !TryInt(columns[6], out var left) ||
                !TryInt(columns[7], out var top) ||
                !TryInt(columns[8], out var width) ||
                !TryInt(columns[9], out var height) ||
                !double.TryParse(columns[10], NumberStyles.Float, CultureInfo.InvariantCulture, out var confidence))
            {
                continue;
            }

            words.Add(new Word(block, paragraph, line, left, top, width, height, confidence, columns[11].Trim()));
        }

        if (pageWidth <= 0 || pageHeight <= 0 || words.Count == 0)
            throw new PatientFormOcrException("Offline OCR did not find readable text in this scan.");

        string? Field(double left, double top, double right, double bottom, params string[] labels) =>
            Extract(words, pageWidth, pageHeight, left, top, right, bottom, labels);

        var guardian = Field(.02, .72, .58, .84, "name", "relationship");
        var relationship = FindRelationship(guardian);
        var guardianName = RemovePhrase(guardian, relationship);
        var rawText = string.Join(Environment.NewLine, words
            .OrderBy(word => word.Block)
            .ThenBy(word => word.Paragraph)
            .ThenBy(word => word.Line)
            .ThenBy(word => word.Left)
            .GroupBy(word => (word.Block, word.Paragraph, word.Line))
            .Select(lineWords => string.Join(' ', lineWords.Select(word => word.Text))));

        var result = new PatientFormOcrResult
        {
            PatientFileNumber = Field(.72, .08, .99, .17, "patient", "file", "no"),
            Centre = Field(.06, .13, .50, .20, "centre"),
            FullName = Field(.04, .21, .52, .29, "full", "name"),
            PreferredName = Field(.04, .27, .52, .34, "preferred", "name"),
            DateOfBirth = NormalizeDate(Field(.51, .20, .99, .28, "date", "of", "birth", "dd/mm/yyyy")),
            IdentificationNumber = Field(.51, .26, .99, .34, "nic", "nic#"),
            Gender = NormalizeGender(Field(.51, .31, .99, .40, "gender")),
            Employment = Field(.51, .38, .99, .47, "employment"),
            Address = Field(.03, .31, .53, .47, "address"),
            District = Field(.51, .45, .99, .54, "district"),
            Email = Field(.51, .52, .99, .61, "email"),
            ReferralSource = Field(.51, .58, .99, .73, "how", "the", "patient", "get", "to", "know", "exceed"),
            TravelTimeDistance = Field(.14, .93, .55, 1.00, "time/distance", "travel", "center", "centre"),
            GuardianName = NullIfBlank(guardianName),
            GuardianRelationship = NullIfBlank(relationship),
            GuardianAddress = Field(.02, .82, .59, .95, "address"),
            GuardianPhone = ExtractPhone(Field(.57, .72, .99, .85, "phone")),
            GuardianMobile = ExtractPhone(Field(.57, .82, .99, .95, "mobile")),
            OverallConfidence = Math.Round(Math.Clamp(words.Where(word => word.Confidence >= 0).DefaultIfEmpty()
                .Average(word => word?.Confidence ?? 0) / 100d, 0, 1), 2),
            RawTranscription = rawText,
            Warnings =
            [
                "Offline OCR is optimized for printed text. Check every handwritten value against the scan before saving.",
                "Offline confidence measures character recognition, not whether a value was mapped to the correct database field."
            ]
        };

        var tableText = Field(.02, .44, .53, .73, "telephone", "no", "date", "confirmed", "person", "checked");
        foreach (Match match in PhonePattern().Matches(tableText ?? string.Empty))
        {
            result.Contacts.Add(new PatientFormOcrContact { TelephoneNumber = match.Value });
        }

        AddBlankWarnings(result);
        return result;
    }

    private static string? Extract(
        IEnumerable<Word> words,
        int pageWidth,
        int pageHeight,
        double left,
        double top,
        double right,
        double bottom,
        params string[] labels)
    {
        var excluded = labels.Select(NormalizeToken).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selected = words
            .Where(word => word.Confidence >= 0)
            .Where(word =>
            {
                var centerX = (word.Left + (word.Width / 2d)) / pageWidth;
                var centerY = (word.Top + (word.Height / 2d)) / pageHeight;
                return centerX >= left && centerX <= right && centerY >= top && centerY <= bottom;
            })
            .Where(word => !excluded.Contains(NormalizeToken(word.Text)))
            .OrderBy(word => word.Top)
            .ThenBy(word => word.Left)
            .Select(word => word.Text)
            .ToList();

        return NullIfBlank(string.Join(' ', selected));
    }

    private static string? NormalizeDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = DatePattern().Match(value);
        if (!match.Success ||
            !int.TryParse(match.Groups[1].Value, out var day) ||
            !int.TryParse(match.Groups[2].Value, out var month) ||
            !int.TryParse(match.Groups[3].Value, out var year))
        {
            return null;
        }

        if (year < 100)
            year += year > DateTime.Today.Year % 100 ? 1900 : 2000;

        return DateOnly.TryParseExact(
            $"{year:D4}-{month:D2}-{day:D2}",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
    }

    private static string? NormalizeGender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (Regex.IsMatch(value, @"\bfemale\b", RegexOptions.IgnoreCase))
            return "Female";
        if (Regex.IsMatch(value, @"\bmale\b", RegexOptions.IgnoreCase))
            return "Male";
        if (Regex.IsMatch(value, @"\bother\b", RegexOptions.IgnoreCase))
            return "Other";
        return null;
    }

    private static string? ExtractPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var phone = new string(value.Where(character => char.IsDigit(character) || character == '+').ToArray());
        return phone.Count(char.IsDigit) >= 7 ? phone : null;
    }

    private static string? FindRelationship(string? guardian)
    {
        if (string.IsNullOrWhiteSpace(guardian))
            return null;

        string[] relationships =
        [
            "Grandmother", "Grandfather", "Caregiver", "Daughter", "Brother", "Sister",
            "Mother", "Father", "Spouse", "Nephew", "Cousin", "Friend", "Uncle", "Aunt",
            "Niece", "Other", "Son"
        ];
        return relationships.FirstOrDefault(value =>
            Regex.IsMatch(guardian, $@"\b{Regex.Escape(value)}\b", RegexOptions.IgnoreCase));
    }

    private static string? RemovePhrase(string? value, string? phrase)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(phrase))
            return value;
        return NullIfBlank(Regex.Replace(value, $@"\b{Regex.Escape(phrase)}\b", string.Empty, RegexOptions.IgnoreCase));
    }

    private static string NormalizeToken(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string? NullIfBlank(string? value)
    {
        var trimmed = value?.Trim(' ', ':', '#', '-', '.');
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool TryInt(string value, out int parsed) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

    private static void AddBlankWarnings(PatientFormOcrResult result)
    {
        if (string.IsNullOrWhiteSpace(result.FullName))
            result.Warnings.Add("Full name was not read; enter it manually.");
        if (string.IsNullOrWhiteSpace(result.DateOfBirth))
            result.Warnings.Add("Date of birth was not read clearly; enter it manually.");
        if (string.IsNullOrWhiteSpace(result.IdentificationNumber))
            result.Warnings.Add("Identification number is blank or unreadable.");
        if (string.IsNullOrWhiteSpace(result.Centre))
            result.Warnings.Add("Centre is blank or unreadable; select the branch manually.");
    }

    [GeneratedRegex(@"\b(?:\+?\d[\d\s-]{5,}\d)\b")]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b(\d{1,2})\s*[.\-/]\s*(\d{1,2})\s*[.\-/]\s*(\d{2,4})\b")]
    private static partial Regex DatePattern();
}
