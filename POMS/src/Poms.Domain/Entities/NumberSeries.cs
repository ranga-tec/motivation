// ============================================================================
// Poms.Domain/Entities/NumberSeries.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class NumberSeries
{
    public int Id { get; set; }
    public int CenterId { get; set; }
    public int Year { get; set; }
    public int LastSeq { get; set; }
}
