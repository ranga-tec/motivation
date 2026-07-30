using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AnyAuthenticatedUser")]
public class AppointmentsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IRestrictedAccessService _restrictedAccess;

    public AppointmentsController(
        PomsDbContext context,
        IRestrictedAccessService restrictedAccess)
    {
        _context = context;
        _restrictedAccess = restrictedAccess;
    }

    // GET: Appointments / Date-Based Actions tab (PRD 5.3)
    public async Task<IActionResult> Index(DateOnly? date, AppointmentType? type, AppointmentStatus? status)
    {
        var access = await _restrictedAccess.GetScopeAsync(User);
        var query = access.Filter(
            _context.Appointments.Include(a => a.Patient).Include(a => a.Episode));

        if (date.HasValue)
            query = query.Where(a => a.AppointmentDate == date.Value);
        if (type.HasValue)
            query = query.Where(a => a.Type == type.Value);
        else
            // PRD 13.3: delivery must not be shown as a pending appointment by default —
            // it is tracked via Delivery records/reports instead. Still viewable by explicitly filtering Type = Delivery.
            query = query.Where(a => a.Type != AppointmentType.Delivery);
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var appointments = await query.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime).ToListAsync();

        ViewBag.Date = date;
        ViewBag.Type = type;
        ViewBag.Status = status;
        return View(appointments);
    }

    // GET: Appointments/Create?patientId=&type=
    public async Task<IActionResult> Create(Guid? patientId, AppointmentType? type)
    {
        var vm = new AppointmentViewModel { AppointmentDate = DateOnly.FromDateTime(DateTime.Today) };
        if (type.HasValue) vm.Type = type.Value;

        if (patientId.HasValue)
        {
            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient != null)
            {
                vm.PatientId = patient.Id;
                vm.PatientNumber = patient.PatientNumber;
                vm.PatientName = patient.FullName;
            }
        }

        await PopulateDropdowns();
        return View(vm);
    }

    // AJAX: Appointments/GetEpisodesByPatient
    [HttpGet]
    public async Task<JsonResult> GetEpisodesByPatient(Guid patientId)
    {
        var access = await _restrictedAccess.GetScopeAsync(User);
        var episodes = await access.Filter(_context.Episodes)
            .Where(episode => episode.PatientId == patientId)
            .Select(e => new { e.Id, DisplayName = e.RecordDate.ToString() + " - " + e.Status })
            .ToListAsync();
        return Json(episodes);
    }

    // POST: Appointments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppointmentViewModel model)
    {
        if (model.EpisodeId.HasValue)
        {
            var episode = await _context.Episodes
                .FirstOrDefaultAsync(item => item.Id == model.EpisodeId.Value);
            if (episode == null || episode.PatientId != model.PatientId)
                return NotFound();

            var access = await _restrictedAccess.GetScopeAsync(User);
            var allowed = access.CanAccess(episode.IsRestricted, episode.CreatedBy);
            await _restrictedAccess.AuditAsync(
                access,
                allowed ? "CreateAppointment" : "CreateAppointmentDenied",
                nameof(Episode),
                episode.Id,
                episode.IsRestricted,
                allowed);
            if (!allowed) return NotFound();
        }

        if (ModelState.IsValid)
        {
            var appointment = new Appointment
            {
                PatientId = model.PatientId,
                EpisodeId = model.EpisodeId,
                Type = model.Type,
                AppointmentDate = model.AppointmentDate,
                AppointmentTime = model.AppointmentTime,
                Status = AppointmentStatus.Scheduled,
                Notes = model.Notes,
                CreatedBy = User.Identity?.Name
            };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment created.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Appointments/Complete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(Guid id)
    {
        var appointment = await _context.Appointments
            .Include(item => item.Episode)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (appointment != null)
        {
            if (!await CanAccessAppointmentAsync(appointment, "CompleteAppointment"))
                return NotFound();

            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedBy = User.Identity?.Name;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Appointments/Cancel/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var appointment = await _context.Appointments
            .Include(item => item.Episode)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (appointment != null)
        {
            if (!await CanAccessAppointmentAsync(appointment, "CancelAppointment"))
                return NotFound();

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedBy = User.Identity?.Name;
            appointment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Patients = new SelectList(
            await _context.Patients.Select(p => new { p.Id, DisplayName = p.PatientNumber + " - " + p.FullName }).ToListAsync(),
            "Id", "DisplayName");
        ViewBag.TypeOptions = new SelectList(Enum.GetValues(typeof(AppointmentType)).Cast<AppointmentType>());
    }

    private async Task<bool> CanAccessAppointmentAsync(Appointment appointment, string action)
    {
        if (appointment.Episode is null)
            return true;

        var access = await _restrictedAccess.GetScopeAsync(User);
        var allowed = access.CanAccess(
            appointment.Episode.IsRestricted,
            appointment.Episode.CreatedBy);
        await _restrictedAccess.AuditAsync(
            access,
            allowed ? action : $"{action}Denied",
            nameof(Episode),
            appointment.Episode.Id,
            appointment.Episode.IsRestricted,
            allowed);
        return allowed;
    }
}
