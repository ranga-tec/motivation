// ============================================================================
// Poms.Domain/Entities/MasterData.cs
// ============================================================================
namespace Poms.Domain.Entities;

public class City
{
    public int Id { get; set; }
    public int DistrictId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;

    public District District { get; set; } = default!;
}

public class ReferralSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class MainProblemType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class CauseReasonType
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}

public class Nationality
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
