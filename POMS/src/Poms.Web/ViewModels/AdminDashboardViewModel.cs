namespace Poms.Web.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalPatients { get; set; }
    public int TotalEpisodes { get; set; }
    public int TotalCenters { get; set; }
    public int TotalDeviceTypes { get; set; }
    public List<UserActivitySummary> RecentUserActivity { get; set; } = new();
    public List<AuditLogEntry> RecentAuditLogs { get; set; } = new();
}

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string? Changes { get; set; }
    public DateTime Timestamp { get; set; }
}

public class UserActivitySummary
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime? LastLoginAt { get; set; }
    public int ActionCount { get; set; }
}
