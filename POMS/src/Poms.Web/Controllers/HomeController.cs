using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Infrastructure.Data;
using Poms.Web.Models;
using System.Diagnostics;

namespace Poms.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly PomsDbContext _context;

    public HomeController(ILogger<HomeController> logger, PomsDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalPatients = await _context.Patients.CountAsync();
        var activeEpisodes = await _context.Episodes.CountAsync(e => !e.ClosedOn.HasValue);
        var deliveriesThisMonth = await _context.Deliveries
            .CountAsync(d => d.DeliveryDate.HasValue &&
                        d.DeliveryDate.Value.Month == DateTime.Now.Month &&
                        d.DeliveryDate.Value.Year == DateTime.Now.Year);
        var pendingFollowUps = await _context.FollowUps
            .CountAsync(f => f.NextAppointmentDate.HasValue &&
                        f.NextAppointmentDate.Value <= DateOnly.FromDateTime(DateTime.Today.AddDays(7)));

        ViewBag.TotalPatients = totalPatients;
        ViewBag.ActiveEpisodes = activeEpisodes;
        ViewBag.DeliveriesThisMonth = deliveriesThisMonth;
        ViewBag.PendingFollowUps = pendingFollowUps;

        return View();
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
