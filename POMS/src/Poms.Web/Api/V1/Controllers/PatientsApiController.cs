using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Infrastructure.Data;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize(Policy = "DataEntry")]
public sealed class PatientsApiController(PomsDbContext context) : ControllerBase
{
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
}
