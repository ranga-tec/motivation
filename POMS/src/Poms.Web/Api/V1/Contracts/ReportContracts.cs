namespace Poms.Web.Api.V1.Contracts;

public sealed record ReportDefinition(string Key, string Title);
public sealed record ReportOptionsResponse(IReadOnlyList<ReportDefinition> Reports, IReadOnlyList<AdminItem> Centers, IReadOnlyList<AdminItem> Provinces);
public sealed record ReportResultResponse(string Key, string Title, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows, int TotalCount);
public sealed record DashboardResponse(int TotalPatients, int TodayAppointments, int AwaitingAppointments, int ActiveRecords, int AssessmentsThisMonth, int DeliveriesThisMonth, int ActivePatients, int TodayActivities, TodayActivityBreakdown TodayBreakdown);
public sealed record TodayActivityBreakdown(int Assessments, int Fittings, int Deliveries, int ScheduledVisits);
public sealed record DashboardActivity(string Type, string Description, DateTime OccurredAt, Guid PatientId, string PatientNumber, string PatientName, string Location, string? User, Guid? EpisodeId);
public sealed record DashboardRecord(Guid EpisodeId, Guid PatientId, string PatientNumber, string PatientName, string Location, string? AssessmentType, string? LimbCategory, string? Prescription, DateOnly LastActivity, string Stage, string Status);
public sealed class DashboardRecordQuery
{
    public Poms.Domain.Enums.RecordStatus? Status { get; init; }
    public int? CenterId { get; init; }
    [System.ComponentModel.DataAnnotations.StringLength(200)] public string? Search { get; init; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)] public int Page { get; init; } = 1;
    [System.ComponentModel.DataAnnotations.Range(1, 100)] public int PageSize { get; init; } = 20;
}
