namespace Poms.Web.ViewModels;

public class RecentActivityItem
{
    public DateTime Date { get; set; }
    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }
    public string ActivityType { get; set; } = default!;
    public string? User { get; set; }
    public string? Location { get; set; }
}
