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

[Authorize(Policy = "DataEntry")]
public class PatientsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IPatientNumberService _patientNumberService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        PomsDbContext context,
        IPatientNumberService patientNumberService,
        ILogger<PatientsController> logger)
    {
        _context = context;
        _patientNumberService = patientNumberService;
        _logger = logger;
    }

    // GET: Patients
    public async Task<IActionResult> Index(string searchString, int? centerId, int? districtId, int page = 1)
    {
        var query = _context.Patients
            .Include(p => p.Province)
            .Include(p => p.District)
            .Include(p => p.Center)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(p => 
                p.PatientNumber.Contains(searchString) ||
                p.FirstName.Contains(searchString) ||
                (p.LastName != null && p.LastName.Contains(searchString)) ||
                (p.NationalId != null && p.NationalId.Contains(searchString)));
        }

        if (centerId.HasValue)
            query = query.Where(p => p.CenterId == centerId.Value);

        if (districtId.HasValue)
            query = query.Where(p => p.DistrictId == districtId.Value);

        var pageSize = 20;
        var totalCount = await query.CountAsync();
        var patients = await query
            .OrderByDescending(p => p.RegistrationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.CenterId = centerId;
        ViewBag.DistrictId = districtId;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await PopulateDropdowns();
        return View(patients);
    }

    // GET: Patients/Details/5
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null) return NotFound();

        var patient = await _context.Patients
            .Include(p => p.Province)
            .Include(p => p.District)
            .Include(p => p.Center)
            .Include(p => p.Conditions).ThenInclude(pc => pc.Condition)
            .Include(p => p.Episodes)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (patient == null) return NotFound();

        return View(patient);
    }

    // GET: Patients/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new PatientViewModel());
    }

    // POST: Patients/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var registrationDate = model.RegistrationDate ?? DateOnly.FromDateTime(DateTime.Today);
                var patientNumber = await _patientNumberService.GeneratePatientNumberAsync(
                    model.CenterId, registrationDate);

                var patient = new Patient
                {
                    PatientNumber = patientNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Dob = model.Dob,
                    Sex = model.Sex,
                    NationalId = model.NationalId,
                    Address1 = model.Address1,
                    Address2 = model.Address2,
                    ProvinceId = model.ProvinceId,
                    DistrictId = model.DistrictId,
                    Phone1 = model.Phone1,
                    Phone2 = model.Phone2,
                    Email = model.Email,
                    CenterId = model.CenterId,
                    RegistrationDate = registrationDate,
                    ReferredBy = model.ReferredBy,
                    Remarks = model.Remarks ?? "",
                    GuardianName = model.GuardianName,
                    GuardianRelationship = model.GuardianRelationship,
                    GuardianAddress = model.GuardianAddress,
                    GuardianPhone1 = model.GuardianPhone1,
                    GuardianPhone2 = model.GuardianPhone2,
                    CreatedBy = User.Identity?.Name
                };

                _context.Add(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Patient {PatientNumber} created by {User}", 
                    patientNumber, User.Identity?.Name);

                TempData["Success"] = $"Patient {patientNumber} registered successfully!";
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                ModelState.AddModelError("", "An error occurred while creating the patient.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Patients/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var patient = await _context.Patients.FindAsync(id);
        if (patient == null) return NotFound();

        var model = new PatientViewModel
        {
            Id = patient.Id,
            PatientNumber = patient.PatientNumber,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Dob = patient.Dob,
            Sex = patient.Sex,
            NationalId = patient.NationalId,
            Address1 = patient.Address1,
            Address2 = patient.Address2,
            ProvinceId = patient.ProvinceId,
            DistrictId = patient.DistrictId,
            Phone1 = patient.Phone1,
            Phone2 = patient.Phone2,
            Email = patient.Email,
            CenterId = patient.CenterId,
            RegistrationDate = patient.RegistrationDate,
            ReferredBy = patient.ReferredBy,
            Remarks = patient.Remarks,
            GuardianName = patient.GuardianName,
            GuardianRelationship = patient.GuardianRelationship,
            GuardianAddress = patient.GuardianAddress,
            GuardianPhone1 = patient.GuardianPhone1,
            GuardianPhone2 = patient.GuardianPhone2
        };

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Patients/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PatientViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null) return NotFound();

                // Update fields (PatientNumber is immutable)
                patient.FirstName = model.FirstName;
                patient.LastName = model.LastName;
                patient.Dob = model.Dob;
                patient.Sex = model.Sex;
                patient.NationalId = model.NationalId;
                patient.Address1 = model.Address1;
                patient.Address2 = model.Address2;
                patient.ProvinceId = model.ProvinceId;
                patient.DistrictId = model.DistrictId;
                patient.Phone1 = model.Phone1;
                patient.Phone2 = model.Phone2;
                patient.Email = model.Email;
                patient.ReferredBy = model.ReferredBy;
                patient.Remarks = model.Remarks;
                patient.GuardianName = model.GuardianName;
                patient.GuardianRelationship = model.GuardianRelationship;
                patient.GuardianAddress = model.GuardianAddress;
                patient.GuardianPhone1 = model.GuardianPhone1;
                patient.GuardianPhone2 = model.GuardianPhone2;
                patient.UpdatedBy = User.Identity?.Name;
                patient.UpdatedAt = DateTime.UtcNow;

                _context.Update(patient);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Patient updated successfully!";
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(model.Id))
                    return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", id);
                ModelState.AddModelError("", "An error occurred while updating the patient.");
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Patients/Delete/5
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();

        var patient = await _context.Patients
            .Include(p => p.Province)
            .Include(p => p.District)
            .Include(p => p.Center)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (patient == null) return NotFound();

        return View(patient);
    }

    // POST: Patients/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient != null)
        {
            // Soft delete
            patient.IsDeleted = true;
            patient.DeletedAt = DateTime.UtcNow;
            patient.DeletedBy = User.Identity?.Name;
            _context.Update(patient);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Patient {PatientNumber} soft deleted by {User}", 
                patient.PatientNumber, User.Identity?.Name);
        }

        return RedirectToAction(nameof(Index));
    }

    // AJAX: Get Districts by Province
    [HttpGet]
    public async Task<JsonResult> GetDistrictsByProvince(int provinceId)
    {
        var districts = await _context.Districts
            .Where(d => d.ProvinceId == provinceId)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();
        return Json(districts);
    }

    // AJAX: Get Centers by District
    [HttpGet]
    public async Task<JsonResult> GetCentersByDistrict(int districtId)
    {
        var centers = await _context.Centers
            .Where(c => c.DistrictId == districtId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        return Json(centers);
    }

    private bool PatientExists(Guid id)
    {
        return _context.Patients.Any(e => e.Id == id);
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Provinces = new SelectList(await _context.Provinces.ToListAsync(), "Id", "Name");
        ViewBag.Districts = new SelectList(await _context.Districts.ToListAsync(), "Id", "Name");
        ViewBag.Centers = new SelectList(await _context.Centers.ToListAsync(), "Id", "Name");
        ViewBag.SexOptions = new SelectList(Enum.GetValues(typeof(Sex)).Cast<Sex>());
    }
}
