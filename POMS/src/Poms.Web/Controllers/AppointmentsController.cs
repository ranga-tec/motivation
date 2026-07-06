using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AnyAuthenticatedUser")]
public class AppointmentsController : Controller
{
    private readonly PomsDbContext _context;

    public AppointmentsController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: Appointments / Date-Based Actions tab (PRD 5.3)
    public async Task<IActionResult> Index(DateOnly? date, AppointmentType? type, AppointmentStatus? status)
    {
        var query = _context.Appointments.Include(a => a.Patient).Include(a => a.Episode).AsQueryable();

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
        var episodes = await _context.Episodes
            .Where(e => e.PatientId == patientId)
            .Select(e => new { e.Id, DisplayName = e.RecordDate.ToString() + " - " + e.Status })
            .ToListAsync();
        return Json(episodes);
    }

    // POST: Appointments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppointmentViewModel model)
    {
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
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
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
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
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
}
