using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "DataEntry")]
public sealed class PatientsApiController(
    PomsDbContext context,
    IPatientNumberService patientNumbers,
    IDuplicateCheckService duplicateCheck,
    IAppointmentAssigneeService appointmentAssignees) : ControllerBase
{
    [HttpGet("registration-options")]
    [ProducesResponseType<PatientRegistrationOptionsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientRegistrationOptionsResponse>> RegistrationOptions(
        CancellationToken cancellationToken)
    {
        var provinces = await context.Provinces.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new RegistrationOption(item.Id, item.Name, null)).ToListAsync(cancellationToken);
        var districts = await context.Districts.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new RegistrationOption(item.Id, item.Name, item.ProvinceId)).ToListAsync(cancellationToken);
        var cities = await context.Cities.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new RegistrationOption(item.Id, item.Name, item.DistrictId)).ToListAsync(cancellationToken);
        var centers = await context.Centers.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new RegistrationOption(item.Id, item.Name, item.DistrictId)).ToListAsync(cancellationToken);
        var referrals = await context.ReferralSources.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new RegistrationOption(item.Id, item.Name, null)).ToListAsync(cancellationToken);
        var assignees = (await appointmentAssignees.GetOptionsAsync())
            .Select(item => new AssigneeOption(item.UserId, item.DisplayText, item.FullName, item.IsPreferred))
            .ToList();

        return Ok(new PatientRegistrationOptionsResponse(
            provinces, districts, cities, centers, referrals, assignees));
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<PatientSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PatientSummaryResponse>>> List(
        [FromQuery] PatientListQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(patient =>
                patient.PatientNumber.ToLower().Contains(search) ||
                patient.FullName.ToLower().Contains(search) ||
                patient.NameWithInitials.ToLower().Contains(search) ||
                patient.IdentificationNumber.ToLower().Contains(search) ||
                patient.Contacts.Any(contact => contact.TelephoneNo.ToLower().Contains(search)));
        }

        if (request.CenterId.HasValue)
            query = query.Where(patient => patient.CenterId == request.CenterId.Value);
        if (request.ProvinceId.HasValue)
            query = query.Where(patient => patient.ProvinceId == request.ProvinceId.Value);
        if (request.DistrictId.HasValue)
            query = query.Where(patient => patient.DistrictId == request.DistrictId.Value);
        if (request.CityId.HasValue)
            query = query.Where(patient => patient.CityId == request.CityId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(patient => patient.RegistrationDate)
            .ThenBy(patient => patient.PatientNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(patient => new PatientSummaryResponse(
                patient.Id,
                patient.PatientNumber,
                patient.FullName,
                patient.NameWithInitials,
                patient.Dob,
                patient.Sex.ToString(),
                patient.Category.ToString(),
                patient.CenterId,
                patient.Center.Name,
                patient.RegistrationDate))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<PatientSummaryResponse>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)request.PageSize)));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PatientDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var patient = await context.Patients
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new PatientDetailResponse(
                item.Id,
                item.PatientNumber,
                item.FullName,
                item.NameWithInitials,
                item.Dob,
                item.Sex.ToString(),
                item.Category.ToString(),
                item.IdentificationType.ToString(),
                item.IdentificationNumber,
                item.Address1,
                item.Address2,
                item.Province.Name,
                item.District.Name,
                item.City != null ? item.City.Name : null,
                item.CityOther,
                item.Email,
                item.Nationality,
                item.CenterId,
                item.Center.Name,
                item.RegistrationDate,
                item.AssignedClinicianName,
                item.Contacts
                    .OrderBy(contact => contact.CreatedAt)
                    .Select(contact => new PatientContactResponse(
                        contact.Id,
                        contact.TelephoneNo,
                        contact.DateConfirmed,
                        contact.PersonChecked))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<ActionResult<PatientEditResponse>> GetForEdit(Guid id, CancellationToken cancellationToken)
    {
        var patient = await context.Patients.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new PatientEditResponse(
                item.Id, item.FullName, item.NameWithInitials, item.Dob, item.Sex.ToString(), item.Employment,
                item.Category.ToString(), item.Nationality, item.IdentificationType.ToString(), item.IdentificationNumber,
                item.Address1, item.Address2, item.ProvinceId, item.DistrictId, item.CityId, item.CityOther, item.Email,
                item.ReferralSourceId, item.ReferralSourceOther, item.ReferralPersonName,
                item.ReferralPersonContactNumber, item.TravelTimeDistance, item.CenterId, item.RegistrationDate,
                item.Remarks, item.AssignedClinicianName ?? "", item.AssignedClinicianUserId,
                item.GuardianName, item.GuardianRelationship, item.GuardianAddress, item.GuardianPhone,
                item.GuardianMobile, item.Contacts.OrderBy(contact => contact.CreatedAt)
                    .Select(contact => new CreatePatientContactRequest(
                        contact.TelephoneNo, contact.DateConfirmed, contact.PersonChecked)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<PatientDetailResponse>> Update(
        Guid id, [FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await context.Patients.Include(item => item.Contacts)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (patient is null) return NotFound();
        var errors = await ValidateReferencesAsync(request, cancellationToken);
        var assignee = await appointmentAssignees.ResolveAsync(request.AssignedClinicianEntry, request.AssignedClinicianUserId);
        if (!assignee.IsValid) errors[nameof(request.AssignedClinicianEntry)] = [assignee.Error!];
        if (errors.Count > 0) return BadRequest(new ValidationProblemDetails(errors) { Status = 400 });

        var identificationNumber = request.IdentificationType == IdentificationType.NotApplicable
            ? string.Empty : request.IdentificationNumber?.Trim() ?? string.Empty;
        var duplicate = await duplicateCheck.CheckAsync(request.IdentificationType, identificationNumber,
            request.FullName.Trim(), request.DateOfBirth, id);
        if (duplicate.IsExactDuplicate || duplicate.HasSimilarNameOrDob && !request.ConfirmPossibleDuplicate)
            return DuplicateProblem(duplicate);

        patient.FullName = request.FullName.Trim(); patient.NameWithInitials = request.NameWithInitials.Trim();
        patient.Dob = request.DateOfBirth; patient.Sex = request.Sex!.Value; patient.Employment = Clean(request.Employment);
        patient.Category = request.Category; patient.Nationality = Clean(request.Nationality);
        patient.IdentificationType = request.IdentificationType; patient.IdentificationNumber = identificationNumber;
        patient.Address1 = request.Address1.Trim(); patient.Address2 = Clean(request.Address2);
        patient.ProvinceId = request.ProvinceId; patient.DistrictId = request.DistrictId; patient.CityId = request.CityId;
        patient.CityOther = request.CityId.HasValue ? null : Clean(request.CityOther); patient.Email = NormalizeEmail(request.Email);
        patient.ReferralSourceId = request.ReferralSourceId; patient.ReferralSourceOther = Clean(request.ReferralSourceOther);
        patient.ReferralPersonName = Clean(request.ReferralPersonName); patient.ReferralPersonContactNumber = Clean(request.ReferralPersonContactNumber);
        patient.TravelTimeDistance = Clean(request.TravelTimeDistance); patient.CenterId = request.CenterId;
        patient.RegistrationDate = request.RegistrationDate ?? patient.RegistrationDate; patient.Remarks = Clean(request.Remarks);
        patient.AssignedClinicianUserId = assignee.UserId; patient.AssignedClinicianName = assignee.FullName;
        patient.GuardianName = request.GuardianName.Trim(); patient.GuardianRelationship = request.GuardianRelationship.Trim();
        patient.GuardianAddress = Clean(request.GuardianAddress); patient.GuardianPhone = Clean(request.GuardianPhone);
        patient.GuardianMobile = Clean(request.GuardianMobile); patient.UpdatedBy = Actor(); patient.UpdatedAt = DateTime.UtcNow;
        var requestedContacts = request.Contacts
            .Where(contact => !string.IsNullOrWhiteSpace(contact.TelephoneNumber))
            .ToList();
        var existingContacts = patient.Contacts.ToList();
        var actor = Actor();
        var sharedContactCount = Math.Min(existingContacts.Count, requestedContacts.Count);
        for (var index = 0; index < sharedContactCount; index++)
        {
            var existing = existingContacts[index];
            var requested = requestedContacts[index];
            existing.TelephoneNo = requested.TelephoneNumber.Trim();
            existing.DateConfirmed = requested.DateConfirmed;
            existing.PersonChecked = Clean(requested.PersonChecked);
            existing.UpdatedBy = actor;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        foreach (var existing in existingContacts.Skip(requestedContacts.Count))
        {
            patient.Contacts.Remove(existing);
            context.PatientContacts.Remove(existing);
        }
        foreach (var requested in requestedContacts.Skip(existingContacts.Count))
        {
            var addedContact = new PatientContact
            {
                PatientId = patient.Id,
                TelephoneNo = requested.TelephoneNumber.Trim(),
                DateConfirmed = requested.DateConfirmed,
                PersonChecked = Clean(requested.PersonChecked),
                CreatedBy = actor
            };
            patient.Contacts.Add(addedContact);
            context.PatientContacts.Add(addedContact);
        }
        await context.SaveChangesAsync(cancellationToken);
        return Ok((await BuildDetailResponseAsync(id, cancellationToken))!);
    }

    [HttpPost]
    [Authorize(Policy = "ApiWrite")]
    [ProducesResponseType<PatientDetailResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PatientDetailResponse>> Create(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var errors = await ValidateReferencesAsync(request, cancellationToken);
        var assignee = await appointmentAssignees.ResolveAsync(
            request.AssignedClinicianEntry,
            request.AssignedClinicianUserId);
        if (!assignee.IsValid)
            errors[nameof(request.AssignedClinicianEntry)] = [assignee.Error!];
        if (errors.Count > 0)
            return BadRequest(new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest });

        var identificationNumber = request.IdentificationType == IdentificationType.NotApplicable
            ? string.Empty
            : request.IdentificationNumber?.Trim() ?? string.Empty;
        var duplicate = await duplicateCheck.CheckAsync(
            request.IdentificationType,
            identificationNumber,
            request.FullName.Trim(),
            request.DateOfBirth);
        if (duplicate.IsExactDuplicate || duplicate.HasSimilarNameOrDob && !request.ConfirmPossibleDuplicate)
        {
            return DuplicateProblem(duplicate);
        }

        var registrationDate = request.RegistrationDate ?? DateOnly.FromDateTime(DateTime.Today);
        var patientNumber = await patientNumbers.GeneratePatientNumberAsync(request.CenterId, registrationDate);
        var actor = User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "API user";
        var patient = new Patient
        {
            PatientNumber = patientNumber,
            FullName = request.FullName.Trim(),
            NameWithInitials = request.NameWithInitials.Trim(),
            Dob = request.DateOfBirth,
            Sex = request.Sex!.Value,
            Employment = request.Employment?.Trim(),
            Category = request.Category,
            Nationality = request.Nationality?.Trim(),
            IdentificationType = request.IdentificationType,
            IdentificationNumber = identificationNumber,
            Address1 = request.Address1.Trim(),
            Address2 = request.Address2?.Trim(),
            ProvinceId = request.ProvinceId,
            DistrictId = request.DistrictId,
            CityId = request.CityId,
            CityOther = request.CityId.HasValue ? null : request.CityOther?.Trim(),
            Email = NormalizeEmail(request.Email),
            ReferralSourceId = request.ReferralSourceId,
            ReferralSourceOther = request.ReferralSourceOther?.Trim(),
            ReferralPersonName = request.ReferralPersonName?.Trim(),
            ReferralPersonContactNumber = request.ReferralPersonContactNumber?.Trim(),
            TravelTimeDistance = request.TravelTimeDistance?.Trim(),
            CenterId = request.CenterId,
            RegistrationDate = registrationDate,
            RegistrationProcessedBy = actor,
            Remarks = request.Remarks?.Trim(),
            AssignedClinicianUserId = assignee.UserId,
            AssignedClinicianName = assignee.FullName,
            GuardianName = request.GuardianName.Trim(),
            GuardianRelationship = request.GuardianRelationship.Trim(),
            GuardianAddress = request.GuardianAddress?.Trim(),
            GuardianPhone = request.GuardianPhone?.Trim(),
            GuardianMobile = request.GuardianMobile?.Trim(),
            CreatedBy = actor
        };
        foreach (var contact in request.Contacts.Where(item => !string.IsNullOrWhiteSpace(item.TelephoneNumber)))
            patient.Contacts.Add(new PatientContact
            {
                TelephoneNo = contact.TelephoneNumber.Trim(),
                DateConfirmed = contact.DateConfirmed,
                PersonChecked = contact.PersonChecked?.Trim(),
                CreatedBy = actor
            });

        context.Patients.Add(patient);
        await context.SaveChangesAsync(cancellationToken);
        var response = await BuildDetailResponseAsync(patient.Id, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = patient.Id }, response!);
    }

    private async Task<Dictionary<string, string[]>> ValidateReferencesAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (!await context.Districts.AnyAsync(item => item.Id == request.DistrictId && item.ProvinceId == request.ProvinceId, cancellationToken))
            errors[nameof(request.DistrictId)] = ["Select a district in the chosen province."];
        if (request.CityId.HasValue && !await context.Cities.AnyAsync(item => item.Id == request.CityId && item.DistrictId == request.DistrictId && item.IsActive, cancellationToken))
            errors[nameof(request.CityId)] = ["Select an active city in the chosen district."];
        if (!await context.Centers.AnyAsync(item => item.Id == request.CenterId && item.IsActive, cancellationToken))
            errors[nameof(request.CenterId)] = ["Select an active treatment centre."];
        if (request.ReferralSourceId.HasValue && !await context.ReferralSources.AnyAsync(item => item.Id == request.ReferralSourceId && item.IsActive, cancellationToken))
            errors[nameof(request.ReferralSourceId)] = ["Select an active referral source."];
        return errors;
    }

    private async Task<PatientDetailResponse?> BuildDetailResponseAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Patients.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new PatientDetailResponse(
                item.Id, item.PatientNumber, item.FullName, item.NameWithInitials, item.Dob,
                item.Sex.ToString(), item.Category.ToString(), item.IdentificationType.ToString(),
                item.IdentificationNumber, item.Address1, item.Address2, item.Province.Name,
                item.District.Name, item.City != null ? item.City.Name : null, item.CityOther,
                item.Email, item.Nationality, item.CenterId, item.Center.Name, item.RegistrationDate,
                item.AssignedClinicianName,
                item.Contacts.OrderBy(contact => contact.CreatedAt)
                    .Select(contact => new PatientContactResponse(contact.Id, contact.TelephoneNo, contact.DateConfirmed, contact.PersonChecked)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);

    private static string? NormalizeEmail(string? email)
    {
        var value = email?.Trim();
        var normalized = value?.TrimEnd('.');
        return normalized is not null && (normalized.Equals("N/A", StringComparison.OrdinalIgnoreCase) || normalized.Equals("NA", StringComparison.OrdinalIgnoreCase))
            ? "N/A"
            : value;
    }

    private ConflictObjectResult DuplicateProblem(DuplicateCheckResult duplicate)
    {
        var problem = new ProblemDetails { Status = 409,
            Title = duplicate.IsExactDuplicate ? "Duplicate patient" : "Possible matching patient",
            Detail = duplicate.IsExactDuplicate ? "A patient with this identification document already exists."
                : "A patient with the same name and date of birth already exists. Confirm the match was reviewed before continuing." };
        problem.Extensions["duplicateType"] = duplicate.IsExactDuplicate ? "exact" : "possible";
        problem.Extensions["existingPatientNumber"] = duplicate.ExistingPatientNumber;
        problem.Extensions["existingPatientName"] = duplicate.ExistingPatientName;
        return Conflict(problem);
    }

    private string Actor() => User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "API user";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
