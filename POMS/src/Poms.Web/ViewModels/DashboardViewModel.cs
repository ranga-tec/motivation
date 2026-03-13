namespace Poms.Web.ViewModels;

public class DashboardViewModel
{
    // KPI Data wrapper
    public DashboardData Data { get; set; } = new();

    // Chart Data - flat properties for direct access
    public List<ChartDataPoint> RegistrationTrend { get; set; } = new();
    public List<ChartDataPoint> EpisodesByType { get; set; } = new();
    public List<ChartDataPoint> DeliveriesByMonth { get; set; } = new();

    // Recent Activities
    public List<RecentActivity> RecentActivities { get; set; } = new();
}

public class DashboardData
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int OpenEpisodes { get; set; }
    public int CompletedEpisodes { get; set; }
    public int DeliveriesThisMonth { get; set; }
    public int PendingFollowUps { get; set; }
}

public class ChartDataPoint
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
}

public class RecentActivity
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string User { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Color { get; set; } = "primary";
    public string Icon { get; set; } = "fa-circle";
}
