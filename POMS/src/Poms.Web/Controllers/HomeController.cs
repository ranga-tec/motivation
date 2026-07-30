using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Models;
using Poms.Web.ViewModels;
using System.Diagnostics;

namespace Poms.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly PomsDbContext _context;
    private readonly IRestrictedAccessService _restrictedAccess;

    public HomeController(
        ILogger<HomeController> logger,
        PomsDbContext context,
        IRestrictedAccessService restrictedAccess)
    {
        _logger = logger;
        _context = context;
        _restrictedAccess = restrictedAccess;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var access = await _restrictedAccess.GetScopeAsync(User);

        var totalPatients = await _context.Patients.CountAsync();

        var activeRecordsQuery = access.Filter(
            _context.Episodes.Where(e => e.Status == RecordStatus.Active));
        var activeRecords = await activeRecordsQuery.CountAsync();
        var activePatients = await activeRecordsQuery.Select(e => e.PatientId).Distinct().CountAsync();

        var todaysAssessments = await access.Filter(_context.Assessments)
            .CountAsync(a => a.AssessedOn == today);
        var todaysFittings = await access.Filter(_context.Fittings)
            .CountAsync(f => f.FittingDate == today);
        var todaysDeliveries = await access.Filter(_context.Deliveries)
            .CountAsync(d => d.DeliveryDate == today);
        var todaysAppointments = await _context.Appointments
            .Where(a => a.AppointmentDate == today && a.Status == AppointmentStatus.Scheduled && a.Type != AppointmentType.Delivery)
            .Include(a => a.Patient)
            .ToListAsync();

        var recentActivities = await BuildRecentActivitiesFeed(access);

        ViewBag.Patients = new SelectList(
            await _context.Patients.Select(p => new { p.Id, DisplayName = p.PatientNumber + " - " + p.FullName }).ToListAsync(),
            "Id", "DisplayName");

        ViewBag.TotalPatients = totalPatients;
        ViewBag.ActiveRecords = activeRecords;
        ViewBag.ActivePatients = activePatients;
        ViewBag.TodaysAssessments = todaysAssessments;
        ViewBag.TodaysFittings = todaysFittings;
        ViewBag.TodaysDeliveries = todaysDeliveries;
        ViewBag.TodaysAppointments = todaysAppointments;
        ViewBag.RecentActivities = recentActivities;

        return View();
    }

    // GET: Home/ActiveRecordsDrillDown (PRD 5.2.2)
    public async Task<IActionResult> ActiveRecordsDrillDown()
    {
        var access = await _restrictedAccess.GetScopeAsync(User);
        var records = await access.Filter(_context.Episodes)
            .Where(e => e.Status == RecordStatus.Active)
            .Include(e => e.Patient)
            .Include(e => e.Center)
            .Include(e => e.Assessments)
            .Include(e => e.Fittings)
            .Include(e => e.Deliveries)
            .Include(e => e.FollowUps)
            .ToListAsync();

        foreach (var episode in records)
        {
            episode.Assessments = episode.Assessments
                .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
                .ToList();
            episode.Fittings = episode.Fittings
                .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
                .ToList();
            episode.Deliveries = episode.Deliveries
                .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
                .ToList();
            episode.FollowUps = episode.FollowUps
                .Where(record => access.CanAccess(record.IsRestricted, record.CreatedBy))
                .ToList();
        }

        var rows = records.Select(e =>
        {
            var lastAssessment = e.Assessments.Select(a => (DateTime?)a.AssessedOn.ToDateTime(TimeOnly.MinValue)).DefaultIfEmpty(null).Max();
            var lastFitting = e.Fittings.Select(f => (DateTime?)f.FittingDate.ToDateTime(TimeOnly.MinValue)).DefaultIfEmpty(null).Max();
            var lastDelivery = e.Deliveries.Select(d => (DateTime?)d.DeliveryDate.ToDateTime(TimeOnly.MinValue)).DefaultIfEmpty(null).Max();
            var lastFollowUp = e.FollowUps.Select(f => (DateTime?)f.FollowUpDate.ToDateTime(TimeOnly.MinValue)).DefaultIfEmpty(null).Max();

            var lastActivity = new[] { lastAssessment, lastFitting, lastDelivery, lastFollowUp }
                .Where(d => d.HasValue).Select(d => d!.Value).DefaultIfEmpty(e.CreatedAt).Max();

            var stage = e.Deliveries.Any() ? "Delivered"
                : e.Fittings.Any() ? "Fitting"
                : e.Assessments.Any() ? "Assessed"
                : "Registered";

            var assessmentType = e.Assessments.Select(a => a.AssessmentType.ToString()).LastOrDefault();
            var limbCategory = e.Assessments.Select(a => a.LimbCategory.ToString()).LastOrDefault();

            return new
            {
                PatientNumber = e.Patient.PatientNumber,
                PatientName = e.Patient.FullName,
                Location = e.Center.Name,
                AssessmentType = assessmentType,
                LimbCategory = limbCategory,
                LastActivityDate = lastActivity,
                CurrentStage = stage,
                RecordStatus = e.Status.ToString()
            };
        }).OrderByDescending(r => r.LastActivityDate).ToList();

        return View(rows);
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

    private async Task<List<RecentActivityItem>> BuildRecentActivitiesFeed(RestrictedAccessScope access)
    {
        const int take = 20;

        var patients = await _context.Patients
            .Include(p => p.Center)
            .OrderByDescending(p => p.CreatedAt).Take(take)
            .Select(p => new RecentActivityItem
            {
                Date = p.CreatedAt, PatientNumber = p.PatientNumber, PatientName = p.FullName,
                ActivityType = "Patient Registered", User = p.CreatedBy, Location = p.Center.Name
            }).ToListAsync();

        var assessments = await access.Filter(_context.Assessments)
            .Include(a => a.Episode).ThenInclude(e => e.Patient)
            .Include(a => a.Episode).ThenInclude(e => e.Center)
            .OrderByDescending(a => a.CreatedAt).Take(take)
            .Select(a => new RecentActivityItem
            {
                Date = a.CreatedAt, PatientNumber = a.Episode.Patient.PatientNumber, PatientName = a.Episode.Patient.FullName,
                ActivityType = "Assessment Created", User = a.CreatedBy, Location = a.Episode.Center.Name
            }).ToListAsync();

        var fittings = await access.Filter(_context.Fittings)
            .Include(f => f.Episode).ThenInclude(e => e.Patient)
            .Include(f => f.Episode).ThenInclude(e => e.Center)
            .OrderByDescending(f => f.CreatedAt).Take(take)
            .Select(f => new RecentActivityItem
            {
                Date = f.CreatedAt, PatientNumber = f.Episode.Patient.PatientNumber, PatientName = f.Episode.Patient.FullName,
                ActivityType = "Fitting Added", User = f.CreatedBy, Location = f.Episode.Center.Name
            }).ToListAsync();

        var deliveries = await access.Filter(_context.Deliveries)
            .Include(d => d.Episode).ThenInclude(e => e.Patient)
            .Include(d => d.Episode).ThenInclude(e => e.Center)
            .OrderByDescending(d => d.CreatedAt).Take(take)
            .Select(d => new RecentActivityItem
            {
                Date = d.CreatedAt, PatientNumber = d.Episode.Patient.PatientNumber, PatientName = d.Episode.Patient.FullName,
                ActivityType = "Delivery Recorded", User = d.CreatedBy, Location = d.Episode.Center.Name
            }).ToListAsync();

        var followUps = await access.Filter(_context.FollowUps)
            .Include(f => f.Episode).ThenInclude(e => e.Patient)
            .Include(f => f.Episode).ThenInclude(e => e.Center)
            .OrderByDescending(f => f.CreatedAt).Take(take)
            .Select(f => new RecentActivityItem
            {
                Date = f.CreatedAt, PatientNumber = f.Episode.Patient.PatientNumber, PatientName = f.Episode.Patient.FullName,
                ActivityType = "Follow-up Added", User = f.CreatedBy, Location = f.Episode.Center.Name
            }).ToListAsync();

        var patientDocs = await access.Filter(_context.PatientDocuments)
            .Include(d => d.Patient).ThenInclude(p => p.Center)
            .OrderByDescending(d => d.UploadedAt).Take(take)
            .Select(d => new RecentActivityItem
            {
                Date = d.UploadedAt, PatientNumber = d.Patient.PatientNumber, PatientName = d.Patient.FullName,
                ActivityType = "Document Uploaded", User = d.UploadedBy, Location = d.Patient.Center.Name
            }).ToListAsync();

        return patients.Concat(assessments).Concat(fittings).Concat(deliveries).Concat(followUps).Concat(patientDocs)
            .OrderByDescending(x => x.Date).Take(take).ToList();
    }
}
