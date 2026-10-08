namespace Poms.Web.Api.V1.Contracts;

public sealed record ReportDefinition(string Key, string Title);
public sealed record ReportOptionsResponse(IReadOnlyList<ReportDefinition> Reports, IReadOnlyList<AdminItem> Centers, IReadOnlyList<AdminItem> Provinces);
public sealed record ReportResultResponse(string Key, string Title, IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows, int TotalCount);
public sealed record DashboardResponse(int TotalPatients, int TodayAppointments, int AwaitingAppointments, int ActiveRecords, int AssessmentsThisMonth, int DeliveriesThisMonth);
