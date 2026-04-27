namespace Poms.Web.Models;

public sealed class VersionSwitchOptions
{
    public string Current { get; set; } = "prototype";
    public string? PrototypeUrl { get; set; }
    public string? ProductionUrl { get; set; }
    public string? Secret { get; set; }

    public string CurrentVariant => NormalizeVariant(Current);
    public string CurrentLabel => CurrentVariant == "production" ? "PRODUCTION" : "PROTOTYPE";
    public string TargetVariant => CurrentVariant == "production" ? "prototype" : "production";
    public string TargetLabel => TargetVariant == "production" ? "PRODUCTION" : "PROTOTYPE";
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(GetUrl("prototype")) &&
        !string.IsNullOrWhiteSpace(GetUrl("production"));

    public string? GetUrl(string? variant)
    {
        var normalizedVariant = NormalizeVariant(variant);
        var rawUrl = normalizedVariant == "production" ? ProductionUrl : PrototypeUrl;
        return string.IsNullOrWhiteSpace(rawUrl) ? null : rawUrl.TrimEnd('/');
    }

    private static string NormalizeVariant(string? variant) =>
        string.Equals(variant, "production", StringComparison.OrdinalIgnoreCase)
            ? "production"
            : "prototype";
}
