namespace Poms.Infrastructure.Services;

public sealed class PatientFormOcrOptions
{
    public const string SectionName = "PatientFormOcr";

    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = "https://api.openai.com/v1/responses";
    public string Model { get; set; } = "gpt-4.1-mini-2025-04-14";
    public string? ApiKey { get; set; }
    public OfflinePatientFormOcrOptions Offline { get; set; } = new();
}

public sealed class OfflinePatientFormOcrOptions
{
    public bool Enabled { get; set; } = true;
    public string ExecutablePath { get; set; } = "tesseract";
    public string Language { get; set; } = "eng";
    public int TimeoutSeconds { get; set; } = 60;
}
