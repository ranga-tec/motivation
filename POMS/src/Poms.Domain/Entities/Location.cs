// ============================================================================
// Poms.Domain/Entities/Location.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class Province
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ICollection<District> Districts { get; set; } = new List<District>();
}

public class District
{
    public int Id { get; set; }
    public int ProvinceId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Province Province { get; set; } = default!;
    public ICollection<Center> Centers { get; set; } = new List<Center>();
}

public class Center
{
    public int Id { get; set; }
    public int DistrictId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public District District { get; set; } = default!;
}
