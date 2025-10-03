// ============================================================================
// Poms.Domain/Entities/Delivery.cs
// ============================================================================
namespace Poms.Domain.Entities;

using Poms.Domain.Common;

public class Delivery : BaseEntity
{
    public Guid EpisodeId { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public int? DeviceId { get; set; }
    public string? SerialNumber { get; set; }
    public string? ComponentsJson { get; set; }
    public string? DeliveredBy { get; set; }
    public string? Remarks { get; set; }
    
    public Episode Episode { get; set; } = default!;
    public DeviceCatalog? Device { get; set; }
}
