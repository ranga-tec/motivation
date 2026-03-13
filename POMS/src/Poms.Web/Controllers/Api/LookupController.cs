using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Infrastructure.Data;

namespace Poms.Web.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class LookupController : ControllerBase
{
    private readonly PomsDbContext _context;

    public LookupController(PomsDbContext context)
    {
        _context = context;
    }

    // GET: api/Lookup/Provinces
    [HttpGet("Provinces")]
    public async Task<IActionResult> GetProvinces()
    {
        var provinces = await _context.Provinces
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Code, p.Name })
            .OrderBy(p => p.Name)
            .ToListAsync();
        return Ok(provinces);
    }

    // GET: api/Lookup/Districts?provinceId=1
    [HttpGet("Districts")]
    public async Task<IActionResult> GetDistricts(int? provinceId)
    {
        var query = _context.Districts.Where(d => d.IsActive);

        if (provinceId.HasValue)
            query = query.Where(d => d.ProvinceId == provinceId.Value);

        var districts = await query
            .Select(d => new { d.Id, d.Code, d.Name, d.ProvinceId })
            .OrderBy(d => d.Name)
            .ToListAsync();
        return Ok(districts);
    }

    // GET: api/Lookup/Centers?districtId=1
    [HttpGet("Centers")]
    public async Task<IActionResult> GetCenters(int? districtId)
    {
        var query = _context.Centers.Where(c => c.IsActive);

        if (districtId.HasValue)
            query = query.Where(c => c.DistrictId == districtId.Value);

        var centers = await query
            .Select(c => new { c.Id, c.Code, c.Name, c.DistrictId })
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(centers);
    }

    // GET: api/Lookup/DeviceTypes
    [HttpGet("DeviceTypes")]
    public async Task<IActionResult> GetDeviceTypes()
    {
        var deviceTypes = await _context.DeviceTypes
            .Select(d => new { d.Id, d.Code, d.Name })
            .OrderBy(d => d.Name)
            .ToListAsync();
        return Ok(deviceTypes);
    }

    // GET: api/Lookup/Devices?deviceTypeId=1
    [HttpGet("Devices")]
    public async Task<IActionResult> GetDevices(int? deviceTypeId)
    {
        var query = _context.DeviceCatalogs.Where(d => d.IsActive);

        if (deviceTypeId.HasValue)
            query = query.Where(d => d.DeviceTypeId == deviceTypeId.Value);

        var devices = await query
            .Select(d => new { d.Id, d.Code, d.Name, d.DeviceTypeId })
            .OrderBy(d => d.Name)
            .ToListAsync();
        return Ok(devices);
    }

    // GET: api/Lookup/Components?deviceTypeId=1
    [HttpGet("Components")]
    public async Task<IActionResult> GetComponents(int? deviceTypeId)
    {
        var query = _context.ComponentCatalogs.Where(c => c.IsActive);

        if (deviceTypeId.HasValue)
            query = query.Where(c => c.DeviceTypeId == deviceTypeId.Value);

        var components = await query
            .Select(c => new { c.Id, c.Code, c.Name, c.Category, c.DeviceTypeId })
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Name)
            .ToListAsync();
        return Ok(components);
    }

    // GET: api/Lookup/Conditions
    [HttpGet("Conditions")]
    public async Task<IActionResult> GetConditions(string? bodyRegion)
    {
        var query = _context.Conditions.Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(bodyRegion))
            query = query.Where(c => c.BodyRegion.ToString() == bodyRegion);

        var conditions = await query
            .Select(c => new { c.Id, c.Code, c.Name, BodyRegion = c.BodyRegion.ToString() })
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(conditions);
    }

    // GET: api/Lookup/Patients?search=john
    [HttpGet("Patients")]
    public async Task<IActionResult> SearchPatients(string? search)
    {
        if (string.IsNullOrEmpty(search) || search.Length < 2)
            return Ok(new List<object>());

        var patients = await _context.Patients
            .Where(p => p.IsActive &&
                (p.PatientNumber.Contains(search) ||
                 p.FirstName.Contains(search) ||
                 (p.LastName != null && p.LastName.Contains(search)) ||
                 (p.NationalId != null && p.NationalId.Contains(search))))
            .Take(20)
            .Select(p => new
            {
                p.Id,
                p.PatientNumber,
                Name = p.FirstName + " " + p.LastName,
                p.NationalId
            })
            .ToListAsync();
        return Ok(patients);
    }
}
