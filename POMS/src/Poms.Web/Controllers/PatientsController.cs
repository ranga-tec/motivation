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
    private readonly IDuplicateCheckService _duplicateCheckService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IRestrictedAccessService _restrictedAccess;
    private readonly IAppointmentAssigneeService _appointmentAssignees;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        PomsDbContext context,
        IPatientNumberService patientNumberService,
        IDuplicateCheckService duplicateCheckService,
        IFileStorageService fileStorageService,
        IRestrictedAccessService restrictedAccess,
        IAppointmentAssigneeService appointmentAssignees,
        ILogger<PatientsController> logger)
    {
        _context = context;
        _patientNumberService = patientNumberService;
        _duplicateCheckService = duplicateCheckService;
        _fileStorageService = fileStorageService;
        _restrictedAccess = restrictedAccess;
        _appointmentAssignees = appointmentAssignees;
        _logger = logger;
    }

    // GET: Patients
    public async Task<IActionResult> Index(string searchString, int? centerId, int? provinceId, int? districtId, int? cityId, int page = 1)
    {
        var query = _context.Patients
            .Include(p => p.Province)
            .Include(p => p.District)
            .Include(p => p.City)
            .Include(p => p.Center)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var normalizedSearch = searchString.Trim().ToLower();
            query = query.Where(p =>
                p.PatientNumber.ToLower().Contains(normalizedSearch) ||
                p.FullName.ToLower().Contains(normalizedSearch) ||
                p.NameWithInitials.ToLower().Contains(normalizedSearch) ||
                p.IdentificationNumber.ToLower().Contains(normalizedSearch) ||
                p.Contacts.Any(c => c.TelephoneNo.ToLower().Contains(normalizedSearch)));

            searchString = searchString.Trim();
        }

        if (centerId.HasValue)
            query = query.Where(p => p.CenterId == centerId.Value);

        if (provinceId.HasValue)
            query = query.Where(p => p.ProvinceId == provinceId.Value);

        if (districtId.HasValue)
            query = query.Where(p => p.DistrictId == districtId.Value);

        if (cityId.HasValue)
            query = query.Where(p => p.CityId == cityId.Value);

        var pageSize = 20;
        var totalCount = await query.CountAsync();
        var patients = await query
            .OrderByDescending(p => p.RegistrationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.CenterId = centerId;
        ViewBag.ProvinceId = provinceId;
        ViewBag.DistrictId = districtId;
        ViewBag.CityId = cityId;
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
            .Include(p => p.City)
            .Include(p => p.Center)
            .Include(p => p.ReferralSource)
            .Include(p => p.Contacts)
            .Include(p => p.Episodes)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (patient == null) return NotFound();

        await ApplyRestrictedFiltersAndAudit(patient, "PatientDetails");
        return View(patient);
    }

    // GET: Patients/Folder/5 - rich tabbed Patient Folder view (PRD 7)
    public async Task<IActionResult> Folder(Guid id)
    {
        var patient = await _context.Patients
            .Include(p => p.Province)
            .Include(p => p.District)
            .Include(p => p.City)
            .Include(p => p.Center)
            .Include(p => p.ReferralSource)
            .Include(p => p.Contacts)
            .Include(p => p.Documents)
            .Include(p => p.Appointments)
            .Include(p => p.Episodes).ThenInclude(e => e.Assessments).ThenInclude(a => a.Prescriptions)
            .Include(p => p.Episodes).ThenInclude(e => e.Assessments).ThenInclude(a => a.MainProblemType)
            .Include(p => p.Episodes).ThenInclude(e => e.Assessments).ThenInclude(a => a.CauseReasonType)
            .Include(p => p.Episodes).ThenInclude(e => e.Fittings)
            .Include(p => p.Episodes).ThenInclude(e => e.Deliveries)
            .Include(p => p.Episodes).ThenInclude(e => e.FollowUps)
            .Include(p => p.Episodes).ThenInclude(e => e.Documents)
            .Include(p => p.Episodes).ThenInclude(e => e.Center)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (patient == null) return NotFound();
        await ApplyRestrictedFiltersAndAudit(patient, "PatientFolder");
        return View(patient);
    }

    // GET: Patients/Create
    public async Task<IActionResult> Create()
    {
        var model = new PatientViewModel
        {
            RegistrationDate = DateOnly.FromDateTime(DateTime.Today),
            RegistrationProcessedBy = User.Identity?.Name ?? "",
            Contacts = new List<PatientContactViewModel> { new() }
        };

        await PopulateDropdowns(model);

        return PatientFormResult(model, "create");
    }

    // POST: Patients/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientViewModel model)
    {
        await ValidateLocationAsync(model);
        var photoValidation = await PatientPhotoValidator.ValidateAsync(model.ProfilePhoto);
        if (!photoValidation.IsValid)
            ModelState.AddModelError(nameof(model.ProfilePhoto), photoValidation.Error!);
        var assignee = await _appointmentAssignees.ResolveAsync(
            model.AssignedClinicianEntry,
            model.AssignedClinicianUserId);
        if (!assignee.IsValid)
            ModelState.AddModelError(nameof(model.AssignedClinicianEntry), assignee.Error!);

        if (ModelState.IsValid)
        {
            var duplicate = await _duplicateCheckService.CheckAsync(
                model.IdentificationType, model.IdentificationNumber, model.FullName, model.Dob);

            if (duplicate.IsExactDuplicate)
            {
                ModelState.AddModelError(nameof(model.IdentificationNumber),
                    $"Duplicate {model.IdentificationType} found for existing patient {duplicate.ExistingPatientNumber} - {duplicate.ExistingPatientName}.");
            }
            else if (duplicate.HasSimilarNameOrDob && !model.ConfirmDuplicateWarning)
            {
                ViewBag.ShowDuplicateWarning = true;
                ViewBag.ExistingPatientNumber = duplicate.ExistingPatientNumber;
                ViewBag.ExistingPatientName = duplicate.ExistingPatientName;
            }
            else
            {
                try
                {
                    string? savedPhotoPath = null;
                    var registrationDate = model.RegistrationDate ?? DateOnly.FromDateTime(DateTime.Today);
                    var patientNumber = await _patientNumberService.GeneratePatientNumberAsync(
                        model.CenterId, registrationDate);

                    var patient = new Patient
                    {
                        PatientNumber = patientNumber,
                        FullName = model.FullName,
                        NameWithInitials = model.NameWithInitials,
                        Dob = model.Dob,
                        Sex = model.Sex,
                        Employment = model.Employment,
                        Category = model.Category,
                        Nationality = model.Nationality,
                        IdentificationType = model.IdentificationType,
                        IdentificationNumber = model.IdentificationNumber,
                        Address1 = model.Address1,
                        Address2 = model.Address2,
                        ProvinceId = model.ProvinceId,
                        DistrictId = model.DistrictId,
                        CityId = model.CityId,
                        CityOther = model.CityOther,
                        Email = model.Email,
                        ReferralSourceId = model.ReferralSourceId,
                        ReferralSourceOther = model.ReferralSourceOther,
                        ReferralPersonName = model.ReferralPersonName,
                        ReferralPersonContactNumber = model.ReferralPersonContactNumber,
                        AssignedClinicianUserId = assignee.UserId,
                        AssignedClinicianName = assignee.FullName,
                        TravelTimeDistance = model.TravelTimeDistance,
                        CenterId = model.CenterId,
                        RegistrationDate = registrationDate,
                        RegistrationProcessedBy = string.IsNullOrWhiteSpace(model.RegistrationProcessedBy)
                            ? (User.Identity?.Name ?? "")
                            : model.RegistrationProcessedBy,
                        Remarks = model.Remarks,
                        GuardianName = model.GuardianName,
                        GuardianRelationship = model.GuardianRelationship,
                        GuardianAddress = model.GuardianAddress,
                        GuardianPhone = model.GuardianPhone,
                        GuardianMobile = model.GuardianMobile,
                        CreatedBy = User.Identity?.Name
                    };

                    foreach (var c in model.Contacts.Where(c => !string.IsNullOrWhiteSpace(c.TelephoneNo)))
                    {
                        patient.Contacts.Add(new PatientContact
                        {
                            TelephoneNo = c.TelephoneNo!,
                            DateConfirmed = c.DateConfirmed,
                            PersonChecked = c.PersonChecked,
                            CreatedBy = User.Identity?.Name
                        });
                    }

                    if (model.ProfilePhoto is not null)
                    {
                        var savedPhoto = await _fileStorageService.SaveFileAsync(model.ProfilePhoto, patientNumber);
                        savedPhotoPath = savedPhoto.StoragePath;
                        patient.Documents.Add(new PatientDocument
                        {
                            DocumentType = DocumentType.PatientPhoto,
                            FileName = Path.GetFileName(model.ProfilePhoto.FileName),
                            StoragePath = savedPhoto.StoragePath,
                            ContentType = photoValidation.ContentType!,
                            Notes = "Patient profile photo",
                            UploadedBy = User.Identity?.Name ?? "",
                            UploadedAt = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name
                        });
                    }

                    _context.Add(patient);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        if (savedPhotoPath is not null)
                            await _fileStorageService.DeleteFileAsync(savedPhotoPath);
                        throw;
                    }

                    _logger.LogInformation("Patient {PatientNumber} created by {User}",
                        patientNumber, User.Identity?.Name);

                    TempData["Success"] = $"Patient {patientNumber} registered successfully!";
                    return RedirectToAction(nameof(Folder), new { id = patient.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating patient");
                    ModelState.AddModelError("", "An error occurred while creating the patient.");
                }
            }
        }

        await PopulateDropdowns(model);
        return PatientFormResult(model, "create");
    }

    // GET: Patients/Edit/5
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();

        var patient = await _context.Patients
            .Include(p => p.Contacts)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null) return NotFound();

        var model = new PatientViewModel
        {
            Id = patient.Id,
            PatientNumber = patient.PatientNumber,
            FullName = patient.FullName,
            NameWithInitials = patient.NameWithInitials,
            Dob = patient.Dob,
            Sex = patient.Sex,
            Employment = patient.Employment,
            Category = patient.Category,
            Nationality = patient.Nationality,
            IdentificationType = patient.IdentificationType,
            IdentificationNumber = patient.IdentificationNumber,
            Address1 = patient.Address1,
            Address2 = patient.Address2,
            ProvinceId = patient.ProvinceId,
            DistrictId = patient.DistrictId,
            CityId = patient.CityId,
            CityOther = patient.CityOther,
            Email = patient.Email,
            ReferralSourceId = patient.ReferralSourceId,
            ReferralSourceOther = patient.ReferralSourceOther,
            ReferralPersonName = patient.ReferralPersonName,
            ReferralPersonContactNumber = patient.ReferralPersonContactNumber,
            AssignedClinicianUserId = patient.AssignedClinicianUserId,
            AssignedClinicianEntry = patient.AssignedClinicianName ?? string.Empty,
            TravelTimeDistance = patient.TravelTimeDistance,
            CenterId = patient.CenterId,
            RegistrationDate = patient.RegistrationDate,
            RegistrationProcessedBy = patient.RegistrationProcessedBy,
            Remarks = patient.Remarks,
            GuardianName = patient.GuardianName,
            GuardianRelationship = patient.GuardianRelationship,
            GuardianAddress = patient.GuardianAddress,
            GuardianPhone = patient.GuardianPhone,
            GuardianMobile = patient.GuardianMobile,
            ExistingProfilePhotoUrl = patient.Documents.Any(d => d.DocumentType == DocumentType.PatientPhoto)
                ? Url.Action("PatientPhoto", "Files", new { patientId = patient.Id })
                : null,
            Contacts = patient.Contacts.Select(c => new PatientContactViewModel
            {
                Id = c.Id,
                TelephoneNo = c.TelephoneNo,
                DateConfirmed = c.DateConfirmed,
                PersonChecked = c.PersonChecked
            }).ToList()
        };

        if (model.Contacts.Count == 0)
            model.Contacts.Add(new PatientContactViewModel());

        await PopulateDropdowns(model, hydrateAssignee: true);
        return PatientFormResult(model, "edit");
    }

    // POST: Patients/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PatientViewModel model)
    {
        if (id != model.Id) return NotFound();

        await ValidateLocationAsync(model);
        var photoValidation = await PatientPhotoValidator.ValidateAsync(model.ProfilePhoto);
        if (!photoValidation.IsValid)
            ModelState.AddModelError(nameof(model.ProfilePhoto), photoValidation.Error!);
        var assignee = await _appointmentAssignees.ResolveAsync(
            model.AssignedClinicianEntry,
            model.AssignedClinicianUserId);
        if (!assignee.IsValid)
            ModelState.AddModelError(nameof(model.AssignedClinicianEntry), assignee.Error!);

        if (ModelState.IsValid)
        {
            var duplicate = await _duplicateCheckService.CheckAsync(
                model.IdentificationType, model.IdentificationNumber, model.FullName, model.Dob, excludePatientId: id);

            if (duplicate.IsExactDuplicate)
            {
                ModelState.AddModelError(nameof(model.IdentificationNumber),
                    $"Duplicate {model.IdentificationType} found for existing patient {duplicate.ExistingPatientNumber} - {duplicate.ExistingPatientName}.");
            }
            else if (duplicate.HasSimilarNameOrDob && !model.ConfirmDuplicateWarning)
            {
                ViewBag.ShowDuplicateWarning = true;
                ViewBag.ExistingPatientNumber = duplicate.ExistingPatientNumber;
                ViewBag.ExistingPatientName = duplicate.ExistingPatientName;
            }
            else
            {
                try
                {
                    var patient = await _context.Patients
                        .Include(p => p.Contacts)
                        .Include(p => p.Documents)
                        .FirstOrDefaultAsync(p => p.Id == id);
                    if (patient == null) return NotFound();
                    string? savedPhotoPath = null;
                    var retiredPhotoPaths = new List<string>();

                    patient.FullName = model.FullName;
                    patient.NameWithInitials = model.NameWithInitials;
                    patient.Dob = model.Dob;
                    patient.Sex = model.Sex;
                    patient.Employment = model.Employment;
                    patient.Category = model.Category;
                    patient.Nationality = model.Nationality;
                    patient.IdentificationType = model.IdentificationType;
                    patient.IdentificationNumber = model.IdentificationNumber;
                    patient.Address1 = model.Address1;
                    patient.Address2 = model.Address2;
                    patient.ProvinceId = model.ProvinceId;
                    patient.DistrictId = model.DistrictId;
                    patient.CityId = model.CityId;
                    patient.CityOther = model.CityOther;
                    patient.Email = model.Email;
                    patient.ReferralSourceId = model.ReferralSourceId;
                    patient.ReferralSourceOther = model.ReferralSourceOther;
                    patient.ReferralPersonName = model.ReferralPersonName;
                    patient.ReferralPersonContactNumber = model.ReferralPersonContactNumber;
                    patient.AssignedClinicianUserId = assignee.UserId;
                    patient.AssignedClinicianName = assignee.FullName;
                    patient.TravelTimeDistance = model.TravelTimeDistance;
                    patient.Remarks = model.Remarks;
                    patient.GuardianName = model.GuardianName;
                    patient.GuardianRelationship = model.GuardianRelationship;
                    patient.GuardianAddress = model.GuardianAddress;
                    patient.GuardianPhone = model.GuardianPhone;
                    patient.GuardianMobile = model.GuardianMobile;
                    patient.UpdatedBy = User.Identity?.Name;
                    patient.UpdatedAt = DateTime.UtcNow;

                    // Rebuild contacts (simplest correct approach - at most a handful of rows, no history requirement)
                    _context.PatientContacts.RemoveRange(patient.Contacts);
                    foreach (var c in model.Contacts.Where(c => !string.IsNullOrWhiteSpace(c.TelephoneNo)))
                    {
                        patient.Contacts.Add(new PatientContact
                        {
                            TelephoneNo = c.TelephoneNo!,
                            DateConfirmed = c.DateConfirmed,
                            PersonChecked = c.PersonChecked,
                            CreatedBy = User.Identity?.Name
                        });
                    }

                    var currentPhotos = patient.Documents
                        .Where(d => d.DocumentType == DocumentType.PatientPhoto)
                        .ToList();
                    if (model.ProfilePhoto is not null)
                    {
                        var savedPhoto = await _fileStorageService.SaveFileAsync(model.ProfilePhoto, patient.PatientNumber);
                        savedPhotoPath = savedPhoto.StoragePath;

                        foreach (var currentPhoto in currentPhotos)
                        {
                            currentPhoto.IsDeleted = true;
                            currentPhoto.DeletedAt = DateTime.UtcNow;
                            currentPhoto.DeletedBy = User.Identity?.Name;
                            retiredPhotoPaths.Add(currentPhoto.StoragePath);
                        }

                        patient.Documents.Add(new PatientDocument
                        {
                            DocumentType = DocumentType.PatientPhoto,
                            FileName = Path.GetFileName(model.ProfilePhoto.FileName),
                            StoragePath = savedPhoto.StoragePath,
                            ContentType = photoValidation.ContentType!,
                            Notes = "Patient profile photo",
                            UploadedBy = User.Identity?.Name ?? "",
                            UploadedAt = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name
                        });
                    }
                    else if (model.RemoveProfilePhoto)
                    {
                        foreach (var currentPhoto in currentPhotos)
                        {
                            currentPhoto.IsDeleted = true;
                            currentPhoto.DeletedAt = DateTime.UtcNow;
                            currentPhoto.DeletedBy = User.Identity?.Name;
                            retiredPhotoPaths.Add(currentPhoto.StoragePath);
                        }
                    }

                    _context.Update(patient);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        if (savedPhotoPath is not null)
                            await _fileStorageService.DeleteFileAsync(savedPhotoPath);
                        throw;
                    }

                    foreach (var retiredPhotoPath in retiredPhotoPaths)
                    {
                        try
                        {
                            await _fileStorageService.DeleteFileAsync(retiredPhotoPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not remove retired photo {StoragePath}", retiredPhotoPath);
                        }
                    }

                    TempData["Success"] = "Patient updated successfully!";
                    return RedirectToAction(nameof(Folder), new { id = patient.Id });
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
        }

        await PopulateDropdowns(model);
        await PopulateExistingPhotoStateAsync(model);
        return PatientFormResult(model, "edit");
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
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();
        return Json(districts);
    }

    // AJAX: Get Cities by District
    [HttpGet]
    public async Task<JsonResult> GetCitiesByDistrict(int districtId)
    {
        var cities = await _context.Cities
            .Where(c => c.DistrictId == districtId && c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();
        return Json(cities);
    }

    // AJAX: live duplicate check on ID number blur
    [HttpGet]
    public async Task<JsonResult> CheckDuplicateAjax(IdentificationType idType, string idNumber, string? fullName, DateOnly? dob)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return Json(new { isExactDuplicate = false, hasSimilarNameOrDob = false });

        var result = await _duplicateCheckService.CheckAsync(idType, idNumber, fullName, dob);
        return Json(new
        {
            isExactDuplicate = result.IsExactDuplicate,
            hasSimilarNameOrDob = result.HasSimilarNameOrDob,
            existingPatientNumber = result.ExistingPatientNumber,
            existingPatientName = result.ExistingPatientName
        });
    }

    private bool PatientExists(Guid id)
    {
        return _context.Patients.Any(e => e.Id == id);
    }

    private async Task PopulateExistingPhotoStateAsync(PatientViewModel model)
    {
        if (model.Id == Guid.Empty) return;

        var hasPhoto = await _context.PatientDocuments
            .AnyAsync(d => d.PatientId == model.Id && d.DocumentType == DocumentType.PatientPhoto);
        model.ExistingProfilePhotoUrl = hasPhoto
            ? Url.Action("PatientPhoto", "Files", new { patientId = model.Id })
            : null;
    }

    private async Task ValidateLocationAsync(PatientViewModel model)
    {
        if (model.ProvinceId > 0 && model.DistrictId > 0)
        {
            var districtMatchesProvince = await _context.Districts
                .AnyAsync(d => d.Id == model.DistrictId && d.ProvinceId == model.ProvinceId);
            if (!districtMatchesProvince)
                ModelState.AddModelError(nameof(model.DistrictId), "Select a district in the chosen province.");
        }

        if (model.CityId.HasValue)
        {
            var cityMatchesDistrict = await _context.Cities.AnyAsync(c =>
                c.Id == model.CityId.Value &&
                c.DistrictId == model.DistrictId &&
                c.IsActive);
            if (!cityMatchesDistrict)
                ModelState.AddModelError(nameof(model.CityId), "Select a city in the chosen district.");
            else
                model.CityOther = null;
        }
        else
        {
            model.CityOther = model.CityOther?.Trim();
        }
    }

    private IActionResult PatientFormResult(PatientViewModel model, string mode)
    {
        var isModalRequest =
            string.Equals(Request.Query["modal"], "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        ViewData["PatientFormMode"] = mode;
        ViewData["PatientFormModal"] = isModalRequest;

        return isModalRequest
            ? PartialView("_PatientWizard", model)
            : View(mode == "edit" ? "Edit" : "Create", model);
    }

    private async Task PopulateDropdowns(PatientViewModel? model = null, bool hydrateAssignee = false)
    {
        ViewBag.Provinces = new SelectList(await _context.Provinces.OrderBy(p => p.Name).ToListAsync(), "Id", "Name");
        ViewBag.Districts = new SelectList(await _context.Districts.OrderBy(d => d.Name).ToListAsync(), "Id", "Name");
        ViewBag.Cities = new SelectList(await _context.Cities.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
        ViewBag.Centers = new SelectList(await _context.Centers.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
        ViewBag.SexOptions = new SelectList(Enum.GetValues(typeof(Sex)).Cast<Sex>());
        ViewBag.PatientCategories = new SelectList(Enum.GetValues(typeof(PatientCategory)).Cast<PatientCategory>());
        ViewBag.IdentificationTypes = new SelectList(Enum.GetValues(typeof(IdentificationType)).Cast<IdentificationType>());
        ViewBag.ReferralSources = new SelectList(await _context.ReferralSources.Where(r => r.IsActive).ToListAsync(), "Id", "Name");
        if (model is not null)
        {
            model.AssigneeOptions = await _appointmentAssignees.GetOptionsAsync();
            if (hydrateAssignee && !string.IsNullOrWhiteSpace(model.AssignedClinicianUserId))
            {
                var selected = model.AssigneeOptions.FirstOrDefault(option =>
                    option.UserId == model.AssignedClinicianUserId);
                if (selected is not null)
                    model.AssignedClinicianEntry = selected.DisplayText;
            }
        }
    }

    private async Task ApplyRestrictedFiltersAndAudit(Patient patient, string action)
    {
        var access = await _restrictedAccess.GetScopeAsync(User);

        patient.Documents = patient.Documents
            .Where(document => access.CanAccess(document.IsRestricted, document.CreatedBy))
            .ToList();
        patient.Episodes = patient.Episodes
            .Where(episode => access.CanAccess(episode.IsRestricted, episode.CreatedBy))
            .ToList();
        var visibleEpisodeIds = patient.Episodes
            .Select(episode => episode.Id)
            .ToHashSet();
        patient.Appointments = patient.Appointments
            .Where(appointment =>
                !appointment.EpisodeId.HasValue ||
                visibleEpisodeIds.Contains(appointment.EpisodeId.Value))
            .ToList();

        foreach (var episode in patient.Episodes)
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
            episode.Documents = episode.Documents
                .Where(document => access.CanAccess(document.IsRestricted, document.CreatedBy))
                .ToList();
        }

        var restrictedItemsShown =
            patient.Documents.Count(document => document.IsRestricted) +
            patient.Episodes.Sum(episode =>
                (episode.IsRestricted ? 1 : 0) +
                episode.Assessments.Count(record => record.IsRestricted) +
                episode.Fittings.Count(record => record.IsRestricted) +
                episode.Deliveries.Count(record => record.IsRestricted) +
                episode.FollowUps.Count(record => record.IsRestricted) +
                episode.Documents.Count(document => document.IsRestricted));

        await _restrictedAccess.AuditAsync(
            access,
            action,
            nameof(Patient),
            patient.Id,
            restrictedItemsShown > 0,
            true,
            new { RestrictedItemsShown = restrictedItemsShown });
        if (restrictedItemsShown > 0)
            Response.Headers.CacheControl = "no-store, private";
    }
}
