// ============================================================================
// Poms.Domain/Entities/NumberSeries.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class NumberSeries
{
    public int Id { get; set; }
    public string FlagCode { get; set; } = "";
    public int Year { get; set; }
    public int LastSeq { get; set; }
}
