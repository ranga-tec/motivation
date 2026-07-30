namespace Poms.Infrastructure.Data;

/// <summary>
/// Sri Lankan provinces, administrative districts, and postal localities.
/// Postal localities are sourced from the Sri Lanka Department of Posts Post Code Directory.
/// Kept in code so existing installations can be brought up to date idempotently.
/// </summary>
internal static class SriLankaLocationData
{
    internal sealed record DistrictSeed(string Code, string Name, params string[] Cities);
    internal sealed record ProvinceSeed(string Code, string Name, params DistrictSeed[] Districts);

    internal static readonly ProvinceSeed[] Provinces =
    [
        new("WP", "Western Province",
            new("CO", "Colombo", SriLankaPostalLocalities.CO),
            new("GM", "Gampaha", SriLankaPostalLocalities.GM),
            new("KL", "Kalutara", SriLankaPostalLocalities.KL)),

        new("CP", "Central Province",
            new("KA", "Kandy", SriLankaPostalLocalities.KA),
            new("MT", "Matale", SriLankaPostalLocalities.MT),
            new("NE", "Nuwara Eliya", SriLankaPostalLocalities.NE)),

        new("SP", "Southern Province",
            new("GL", "Galle", SriLankaPostalLocalities.GL),
            new("MR", "Matara", SriLankaPostalLocalities.MR),
            new("HB", "Hambantota", SriLankaPostalLocalities.HB)),

        new("NP", "Northern Province",
            new("JA", "Jaffna", SriLankaPostalLocalities.JA),
            new("KN", "Kilinochchi", SriLankaPostalLocalities.KN),
            new("MN", "Mannar", SriLankaPostalLocalities.MN),
            new("ML", "Mullaitivu", SriLankaPostalLocalities.ML),
            new("VA", "Vavuniya", SriLankaPostalLocalities.VA)),

        new("EP", "Eastern Province",
            new("TC", "Trincomalee", SriLankaPostalLocalities.TC),
            new("BT", "Batticaloa", SriLankaPostalLocalities.BT),
            new("AM", "Ampara", SriLankaPostalLocalities.AM)),

        new("NWP", "North Western Province",
            new("KU", "Kurunegala", SriLankaPostalLocalities.KU),
            new("PU", "Puttalam", SriLankaPostalLocalities.PU)),

        new("NCP", "North Central Province",
            new("AN", "Anuradhapura", SriLankaPostalLocalities.AN),
            new("PO", "Polonnaruwa", SriLankaPostalLocalities.PO)),

        new("UP", "Uva Province",
            new("BA", "Badulla", SriLankaPostalLocalities.BA),
            new("MO", "Monaragala", SriLankaPostalLocalities.MO)),

        new("SGP", "Sabaragamuwa Province",
            new("RT", "Ratnapura", SriLankaPostalLocalities.RT),
            new("KE", "Kegalle", SriLankaPostalLocalities.KE))
    ];
}
