using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Models;
using System.Diagnostics;

namespace Poms.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly PomsDbContext _context;
    private readonly IDashboardService _dashboardService;

    public HomeController(
        ILogger<HomeController> logger,
        PomsDbContext context,
        IDashboardService dashboardService)
    {
        _logger = logger;
        _context = context;
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var dashboardData = await _dashboardService.GetDashboardDataAsync();
        var registrationTrend = await _dashboardService.GetPatientRegistrationTrendAsync(12);
        var episodesByType = await _dashboardService.GetEpisodesByTypeAsync();
        var deliveriesByMonth = await _dashboardService.GetDeliveriesByMonthAsync(6);
        var recentActivities = await _dashboardService.GetRecentActivitiesAsync(10);

        var viewModel = new DashboardViewModel
        {
            Data = dashboardData,
            RegistrationTrend = registrationTrend,
            EpisodesByType = episodesByType,
            DeliveriesByMonth = deliveriesByMonth,
            RecentActivities = recentActivities
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

public class DashboardViewModel
{
    public DashboardData Data { get; set; } = new();
    public List<ChartDataPoint> RegistrationTrend { get; set; } = new();
    public List<ChartDataPoint> EpisodesByType { get; set; } = new();
    public List<ChartDataPoint> DeliveriesByMonth { get; set; } = new();
    public List<RecentActivityItem> RecentActivities { get; set; } = new();
}
