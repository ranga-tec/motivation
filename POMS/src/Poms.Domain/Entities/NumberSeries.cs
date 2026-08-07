// ============================================================================
// Poms.Domain/Entities/NumberSeries.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class NumberSeries
{
    public int Id { get; set; }
    // Kept nullable for compatibility with early databases that did not store the centre.
    public int? CenterId { get; set; }
    public string FlagCode { get; set; } = "";
    public int Year { get; set; }
    public int LastSeq { get; set; }
}
