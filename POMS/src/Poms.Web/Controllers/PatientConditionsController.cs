using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;

namespace Poms.Web.Controllers;

[Authorize(Policy = "DataEntry")]
public class PatientConditionsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly ILogger<PatientConditionsController> _logger;

    public PatientConditionsController(PomsDbContext context, ILogger<PatientConditionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: PatientConditions/Create?patientId=xxx
    public async Task<IActionResult> Create(Guid? patientId)
    {
        if (patientId == null) return NotFound();

        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound();

        var model = new PatientCondition
        {
            PatientId = patientId.Value
        };

        await PopulateDropdowns();
        ViewBag.PatientName = $"{patient.FirstName} {patient.LastName}".Trim();
        ViewBag.PatientNumber = patient.PatientNumber;

        return View(model);
    }

    // POST: PatientConditions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientCondition model)
    {
        // Remove validation for navigation properties
        ModelState.Remove("Patient");
        ModelState.Remove("Condition");

        if (ModelState.IsValid)
        {
            try
            {
                model.Id = Guid.NewGuid();
                _context.PatientConditions.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Condition added to patient {PatientId} by {User}",
                    model.PatientId, User.Identity?.Name);

                TempData["Success"] = "Condition added to patient successfully!";
                return RedirectToAction("Details", "Patients", new { id = model.PatientId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding condition to patient");
                ModelState.AddModelError("", "An error occurred while adding the condition.");
            }
        }

        await PopulateDropdowns();
        var patient = await _context.Patients.FindAsync(model.PatientId);
        ViewBag.PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : "";
        ViewBag.PatientNumber = patient?.PatientNumber ?? "";

        return View(model);
    }

    // GET: PatientConditions/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var patientCondition = await _context.PatientConditions
            .Include(pc => pc.Patient)
            .Include(pc => pc.Condition)
            .FirstOrDefaultAsync(pc => pc.Id == id);

        if (patientCondition == null) return NotFound();

        await PopulateDropdowns();
        ViewBag.PatientName = $"{patientCondition.Patient.FirstName} {patientCondition.Patient.LastName}".Trim();
        ViewBag.PatientNumber = patientCondition.Patient.PatientNumber;

        return View(patientCondition);
    }

    // POST: PatientConditions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PatientCondition model)
    {
        if (id != model.Id) return NotFound();

        // Remove validation for navigation properties
        ModelState.Remove("Patient");
        ModelState.Remove("Condition");

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Patient condition updated successfully!";
                return RedirectToAction("Details", "Patients", new { id = model.PatientId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientConditionExists(model.Id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient condition {Id}", id);
                ModelState.AddModelError("", "An error occurred while updating the condition.");
            }
        }

        await PopulateDropdowns();
        var patient = await _context.Patients.FindAsync(model.PatientId);
        ViewBag.PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}".Trim() : "";
        ViewBag.PatientNumber = patient?.PatientNumber ?? "";

        return View(model);
    }

    // POST: PatientConditions/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var patientCondition = await _context.PatientConditions.FindAsync(id);
        if (patientCondition != null)
        {
            var patientId = patientCondition.PatientId;
            _context.PatientConditions.Remove(patientCondition);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Condition removed from patient {PatientId} by {User}",
                patientId, User.Identity?.Name);

            TempData["Success"] = "Condition removed from patient successfully!";
            return RedirectToAction("Details", "Patients", new { id = patientId });
        }

        return NotFound();
    }

    private bool PatientConditionExists(Guid id)
    {
        return _context.PatientConditions.Any(e => e.Id == id);
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Conditions = new SelectList(
            await _context.Conditions
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(),
            "Id", "Name");

        ViewBag.SideOptions = new SelectList(Enum.GetValues(typeof(Side)).Cast<Side>());
        ViewBag.TypeOptions = new SelectList(Enum.GetValues(typeof(ConditionType)).Cast<ConditionType>());
    }
}