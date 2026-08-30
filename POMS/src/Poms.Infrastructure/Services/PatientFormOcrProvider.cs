namespace Poms.Infrastructure.Services;

public enum PatientFormOcrProvider
{
    OpenAi,
    Offline
}

public sealed record PatientFormOcrProviderStatus(
    PatientFormOcrProvider Provider,
    string DisplayName,
    string Description,
    bool IsConfigured);

public interface IPatientFormOcrEngine
{
    PatientFormOcrProvider Provider { get; }
    string DisplayName { get; }
    string Description { get; }
    bool IsConfigured { get; }

    Task<PatientFormOcrResult> ExtractAsync(
        Stream image,
        string contentType,
        CancellationToken cancellationToken = default);
}

public interface IPatientFormOcrService
{
    IReadOnlyList<PatientFormOcrProviderStatus> Providers { get; }
    bool IsConfigured(PatientFormOcrProvider provider);

    Task<PatientFormOcrResult> ExtractAsync(
        PatientFormOcrProvider provider,
        Stream image,
        string contentType,
        CancellationToken cancellationToken = default);
}

public sealed class PatientFormOcrException : Exception
{
    public PatientFormOcrException(string message) : base(message) { }
    public PatientFormOcrException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class PatientFormOcrService : IPatientFormOcrService
{
    private readonly IReadOnlyDictionary<PatientFormOcrProvider, IPatientFormOcrEngine> _engines;

    public PatientFormOcrService(IEnumerable<IPatientFormOcrEngine> engines)
    {
        _engines = engines.ToDictionary(engine => engine.Provider);
    }

    public IReadOnlyList<PatientFormOcrProviderStatus> Providers => _engines.Values
        .OrderBy(engine => engine.Provider)
        .Select(engine => new PatientFormOcrProviderStatus(
            engine.Provider,
            engine.DisplayName,
            engine.Description,
            engine.IsConfigured))
        .ToList();

    public bool IsConfigured(PatientFormOcrProvider provider) =>
        _engines.TryGetValue(provider, out var engine) && engine.IsConfigured;

    public Task<PatientFormOcrResult> ExtractAsync(
        PatientFormOcrProvider provider,
        Stream image,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_engines.TryGetValue(provider, out var engine) || !engine.IsConfigured)
        {
            throw new PatientFormOcrException(
                $"{provider} patient form OCR is not configured.");
        }

        return engine.ExtractAsync(image, contentType, cancellationToken);
    }
}
