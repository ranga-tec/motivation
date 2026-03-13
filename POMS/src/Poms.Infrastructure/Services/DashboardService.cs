// ============================================================================
// Poms.Infrastructure/Services/DashboardService.cs
// ============================================================================
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;

namespace Poms.Infrastructure.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync(int? centerId = null);
    Task<List<ChartDataPoint>> GetPatientRegistrationTrendAsync(int months = 12);
    Task<List<ChartDataPoint>> GetEpisodesByTypeAsync();
    Task<List<ChartDataPoint>> GetDeliveriesByMonthAsync(int months = 6);
    Task<List<RecentActivityItem>> GetRecentActivitiesAsync(int count = 10);
}

public class DashboardService : IDashboardService
{
    private readonly PomsDbContext _context;

    public DashboardService(PomsDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardData> GetDashboardDataAsync(int? centerId = null)
    {
        var patientsQuery = _context.Patients.AsQueryable();
        var episodesQuery = _context.Episodes.AsQueryable();

        if (centerId.HasValue)
        {
            patientsQuery = patientsQuery.Where(p => p.CenterId == centerId.Value);
            episodesQuery = episodesQuery.Where(e => e.Patient.CenterId == centerId.Value);
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);

        return new DashboardData
        {
            TotalPatients = await patientsQuery.CountAsync(),
            ActivePatients = await patientsQuery.CountAsync(p => p.IsActive),
            OpenEpisodes = await episodesQuery.CountAsync(e => e.ClosedOn == null),
            CompletedEpisodes = await episodesQuery.CountAsync(e => e.ClosedOn != null),
            DeliveriesThisMonth = await _context.Deliveries
                .Where(d => d.DeliveryDate >= startOfMonth && d.DeliveryDate <= today)
                .CountAsync(),
            PendingFollowUps = await _context.FollowUps
                .Where(f => f.NextAppointmentDate != null && f.NextAppointmentDate <= today)
                .CountAsync(),
            NewPatientsThisMonth = await patientsQuery
                .Where(p => p.RegistrationDate >= startOfMonth)
                .CountAsync(),
            TotalFittings = await _context.Fittings.CountAsync()
        };
    }

    public async Task<List<ChartDataPoint>> GetPatientRegistrationTrendAsync(int months = 12)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-months + 1));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        var data = await _context.Patients
            .Where(p => p.RegistrationDate >= startDate && p.RegistrationDate <= endDate)
            .GroupBy(p => new { p.RegistrationDate.Year, p.RegistrationDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var result = new List<ChartDataPoint>();
        for (var date = new DateTime(startDate.Year, startDate.Month, 1);
             date <= new DateTime(endDate.Year, endDate.Month, 1);
             date = date.AddMonths(1))
        {
            var monthData = data.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month);
            result.Add(new ChartDataPoint
            {
                Label = date.ToString("MMM yyyy"),
                Value = monthData?.Count ?? 0
            });
        }

        return result;
    }

    public async Task<List<ChartDataPoint>> GetEpisodesByTypeAsync()
    {
        var data = await _context.Episodes
            .GroupBy(e => e.Type)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString(),
                Value = g.Count()
            })
            .ToListAsync();

        return data;
    }

    public async Task<List<ChartDataPoint>> GetDeliveriesByMonthAsync(int months = 6)
    {
        var startDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(-months + 1));
        var endDate = DateOnly.FromDateTime(DateTime.Today);

        var data = await _context.Deliveries
            .Where(d => d.DeliveryDate.HasValue && d.DeliveryDate >= startDate && d.DeliveryDate <= endDate)
            .GroupBy(d => new { d.DeliveryDate!.Value.Year, d.DeliveryDate.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var result = new List<ChartDataPoint>();
        for (var date = new DateTime(startDate.Year, startDate.Month, 1);
             date <= new DateTime(endDate.Year, endDate.Month, 1);
             date = date.AddMonths(1))
        {
            var monthData = data.FirstOrDefault(d => d.Year == date.Year && d.Month == date.Month);
            result.Add(new ChartDataPoint
            {
                Label = date.ToString("MMM yyyy"),
                Value = monthData?.Count ?? 0
            });
        }

        return result;
    }

    public async Task<List<RecentActivityItem>> GetRecentActivitiesAsync(int count = 10)
    {
        var activities = new List<RecentActivityItem>();

        // Recent patient registrations
        var recentPatients = await _context.Patients
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentActivityItem
            {
                Type = "Patient",
                Description = $"New patient registered: {p.FirstName} {p.LastName}",
                Timestamp = p.CreatedAt,
                Icon = "fa-user-plus",
                Color = "primary"
            })
            .ToListAsync();
        activities.AddRange(recentPatients);

        // Recent episodes
        var recentEpisodes = await _context.Episodes
            .Include(e => e.Patient)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new RecentActivityItem
            {
                Type = "Episode",
                Description = $"Episode opened for {e.Patient.FirstName} {e.Patient.LastName} ({e.Type})",
                Timestamp = e.CreatedAt,
                Icon = "fa-folder-open",
                Color = "success"
            })
            .ToListAsync();
        activities.AddRange(recentEpisodes);

        // Recent deliveries
        var recentDeliveries = await _context.Deliveries
            .Include(d => d.Episode)
            .ThenInclude(e => e.Patient)
            .OrderByDescending(d => d.CreatedAt)
            .Take(5)
            .Select(d => new RecentActivityItem
            {
                Type = "Delivery",
                Description = $"Device delivered to {d.Episode.Patient.FirstName} {d.Episode.Patient.LastName}",
                Timestamp = d.CreatedAt,
                Icon = "fa-truck",
                Color = "info"
            })
            .ToListAsync();
        activities.AddRange(recentDeliveries);

        return activities
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }
}

public class DashboardData
{
    public int TotalPatients { get; set; }
    public int ActivePatients { get; set; }
    public int OpenEpisodes { get; set; }
    public int CompletedEpisodes { get; set; }
    public int DeliveriesThisMonth { get; set; }
    public int PendingFollowUps { get; set; }
    public int NewPatientsThisMonth { get; set; }
    public int TotalFittings { get; set; }
}

public class ChartDataPoint
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
}

public class RecentActivityItem
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
}
