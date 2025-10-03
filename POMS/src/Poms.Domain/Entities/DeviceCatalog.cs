// ============================================================================
// Poms.Domain/Entities/DeviceCatalog.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class DeviceType
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class DeviceCatalog
{
    public int Id { get; set; }
    public int DeviceTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? DefaultComponentsJson { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DeviceType DeviceType { get; set; } = default!;
}

public class ComponentCatalog
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int DeviceTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DeviceType DeviceType { get; set; } = default!;
}
