using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "AdminOnly")]
public sealed class AdminApiController(PomsDbContext context, UserManager<IdentityUser> users, RoleManager<IdentityRole> roles) : ControllerBase
{
    private static readonly string[] LookupTypes = ["referral-sources", "nationalities", "main-problem-types", "cause-reason-types"];

    [HttpGet("catalog")]
    public async Task<AdminCatalogResponse> Catalog(CancellationToken token) => new(
        await context.Provinces.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, x.Code, x.Name, true, null, null)).ToListAsync(token),
        await context.Districts.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, x.Code, x.Name, true, x.ProvinceId, x.Province.Name)).ToListAsync(token),
        await context.Cities.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, "", x.Name, x.IsActive, x.DistrictId, x.District.Name)).ToListAsync(token),
        await context.Centers.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminCenter(x.Id, x.DistrictId, x.District.Name, x.Code, x.Name, x.Address, x.Phone, x.IsActive, x.RequiresPatientNumberFlag, x.PatientNumberFlagCode)).ToListAsync(token),
        new Dictionary<string, IReadOnlyList<AdminItem>> {
            ["referral-sources"] = await context.ReferralSources.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, "", x.Name, x.IsActive, null, null)).ToListAsync(token),
            ["nationalities"] = await context.Nationalities.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, "", x.Name, x.IsActive, null, null)).ToListAsync(token),
            ["main-problem-types"] = await context.MainProblemTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, "", x.Name, x.IsActive, null, null)).ToListAsync(token),
            ["cause-reason-types"] = await context.CauseReasonTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, "", x.Name, x.IsActive, null, null)).ToListAsync(token)
        },
        await context.DeviceTypes.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminItem(x.Id, x.Code, x.Name, true, null, null)).ToListAsync(token),
        await context.DeviceCatalogs.AsNoTracking().OrderBy(x => x.Name).Select(x => new AdminDevice(x.Id, x.DeviceTypeId, x.DeviceType.Name, x.Code, x.Name, x.IsActive)).ToListAsync(token));

    [HttpPost("lookups/{type}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<AdminItem>> CreateLookup(string type, SaveNamedItemRequest request, CancellationToken token)
    {
        if (!LookupTypes.Contains(type)) return NotFound();
        var name = request.Name.Trim(); if (name.Length == 0) return Validation(nameof(request.Name), "Name is required.");
        if (await LookupExists(type, name, null, token)) return ConflictProblem("An item with this name already exists.");
        object item = type switch { "referral-sources" => new ReferralSource { Name = name, IsActive = request.IsActive }, "nationalities" => new Nationality { Name = name, IsActive = request.IsActive }, "main-problem-types" => new MainProblemType { Name = name, IsActive = request.IsActive }, _ => new CauseReasonType { Name = name, IsActive = request.IsActive } };
        context.Add(item); await context.SaveChangesAsync(token); var id = (int)item.GetType().GetProperty("Id")!.GetValue(item)!;
        return Created($"/api/v1/admin/lookups/{type}/{id}", new AdminItem(id, "", name, request.IsActive));
    }

    [HttpPut("lookups/{type}/{id:int}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<AdminItem>> UpdateLookup(string type, int id, SaveNamedItemRequest request, CancellationToken token)
    {
        if (!LookupTypes.Contains(type)) return NotFound(); var name = request.Name.Trim();
        if (name.Length == 0) return Validation(nameof(request.Name), "Name is required.");
        if (await LookupExists(type, name, id, token)) return ConflictProblem("An item with this name already exists.");
        object? item = type switch { "referral-sources" => await context.ReferralSources.FindAsync([id], token), "nationalities" => await context.Nationalities.FindAsync([id], token), "main-problem-types" => await context.MainProblemTypes.FindAsync([id], token), _ => await context.CauseReasonTypes.FindAsync([id], token) };
        if (item is null) return NotFound(); item.GetType().GetProperty("Name")!.SetValue(item, name); item.GetType().GetProperty("IsActive")!.SetValue(item, request.IsActive); await context.SaveChangesAsync(token); return Ok(new AdminItem(id, "", name, request.IsActive));
    }

    [HttpPost("provinces")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> CreateProvince(SaveCodedItemRequest request, CancellationToken token) => SaveProvince(null, request, token);
    [HttpPut("provinces/{id:int}")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> UpdateProvince(int id, SaveCodedItemRequest request, CancellationToken token) => SaveProvince(id, request, token);
    private async Task<ActionResult<AdminItem>> SaveProvince(int? id, SaveCodedItemRequest request, CancellationToken token) { var code = request.Code.Trim().ToUpperInvariant(); var name = request.Name.Trim(); if (code.Length == 0 || name.Length == 0) return Validation("province", "Code and name are required."); if (await context.Provinces.AnyAsync(x => x.Code == code && x.Id != id, token)) return ConflictProblem("Province code already exists."); var item = id is null ? new Province() : await context.Provinces.FindAsync([id.Value], token); if (item is null) return NotFound(); item.Code = code; item.Name = name; if (id is null) context.Provinces.Add(item); await context.SaveChangesAsync(token); return id is null ? Created($"/api/v1/admin/provinces/{item.Id}", new AdminItem(item.Id, code, name, true)) : Ok(new AdminItem(item.Id, code, name, true)); }

    [HttpPost("districts")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> CreateDistrict(SaveCodedItemRequest request, CancellationToken token) => SaveDistrict(null, request, token);
    [HttpPut("districts/{id:int}")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> UpdateDistrict(int id, SaveCodedItemRequest request, CancellationToken token) => SaveDistrict(id, request, token);
    private async Task<ActionResult<AdminItem>> SaveDistrict(int? id, SaveCodedItemRequest request, CancellationToken token) { if (request.ParentId is null || !await context.Provinces.AnyAsync(x => x.Id == request.ParentId, token)) return Validation(nameof(request.ParentId), "Select a province."); var code = request.Code.Trim().ToUpperInvariant(); var name = request.Name.Trim(); if (code.Length == 0 || name.Length == 0) return Validation("district", "Code and name are required."); var item = id is null ? new District() : await context.Districts.FindAsync([id.Value], token); if (item is null) return NotFound(); item.ProvinceId = request.ParentId.Value; item.Code = code; item.Name = name; if (id is null) context.Districts.Add(item); await context.SaveChangesAsync(token); return Ok(new AdminItem(item.Id, code, name, true, item.ProvinceId)); }

    [HttpPost("cities")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> CreateCity(SaveCodedItemRequest request, CancellationToken token) => SaveCity(null, request, token);
    [HttpPut("cities/{id:int}")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> UpdateCity(int id, SaveCodedItemRequest request, CancellationToken token) => SaveCity(id, request, token);
    private async Task<ActionResult<AdminItem>> SaveCity(int? id, SaveCodedItemRequest request, CancellationToken token) { if (request.ParentId is null || !await context.Districts.AnyAsync(x => x.Id == request.ParentId, token)) return Validation(nameof(request.ParentId), "Select a district."); var name = request.Name.Trim(); if (name.Length == 0) return Validation(nameof(request.Name), "Name is required."); var item = id is null ? new City() : await context.Cities.FindAsync([id.Value], token); if (item is null) return NotFound(); item.DistrictId = request.ParentId.Value; item.Name = name; item.IsActive = request.IsActive; if (id is null) context.Cities.Add(item); await context.SaveChangesAsync(token); return Ok(new AdminItem(item.Id, "", name, item.IsActive, item.DistrictId)); }

    [HttpPost("centers")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminCenter>> CreateCenter(SaveCenterRequest request, CancellationToken token) => SaveCenter(null, request, token);
    [HttpPut("centers/{id:int}")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminCenter>> UpdateCenter(int id, SaveCenterRequest request, CancellationToken token) => SaveCenter(id, request, token);
    private async Task<ActionResult<AdminCenter>> SaveCenter(int? id, SaveCenterRequest request, CancellationToken token) { var district = await context.Districts.FindAsync([request.DistrictId], token); if (district is null) return Validation(nameof(request.DistrictId), "Select a district."); var code = request.Code.Trim().ToUpperInvariant(); var name = request.Name.Trim(); if (code.Length == 0 || name.Length == 0) return Validation("center", "Code and name are required."); if (request.RequiresPatientNumberFlag && string.IsNullOrWhiteSpace(request.PatientNumberFlagCode)) return Validation(nameof(request.PatientNumberFlagCode), "Flag code is required."); var item = id is null ? new Center() : await context.Centers.FindAsync([id.Value], token); if (item is null) return NotFound(); item.DistrictId = district.Id; item.Code = code; item.Name = name; item.Address = Clean(request.Address); item.Phone = Clean(request.Phone); item.IsActive = request.IsActive; item.RequiresPatientNumberFlag = request.RequiresPatientNumberFlag; item.PatientNumberFlagCode = request.RequiresPatientNumberFlag ? request.PatientNumberFlagCode?.Trim().ToUpperInvariant() : null; if (id is null) context.Centers.Add(item); await context.SaveChangesAsync(token); return Ok(new AdminCenter(item.Id, item.DistrictId, district.Name, item.Code, item.Name, item.Address, item.Phone, item.IsActive, item.RequiresPatientNumberFlag, item.PatientNumberFlagCode)); }

    [HttpPost("device-types")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> CreateDeviceType(SaveCodedItemRequest request, CancellationToken token) => SaveDeviceType(null, request, token);
    [HttpPut("device-types/{id:int}")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminItem>> UpdateDeviceType(int id, SaveCodedItemRequest request, CancellationToken token) => SaveDeviceType(id, request, token);
    private async Task<ActionResult<AdminItem>> SaveDeviceType(int? id, SaveCodedItemRequest request, CancellationToken token) { var code = request.Code.Trim().ToUpperInvariant(); var name = request.Name.Trim(); if (code.Length == 0 || name.Length == 0) return Validation("deviceType", "Code and name are required."); if (await context.DeviceTypes.AnyAsync(x => x.Code == code && x.Id != id, token)) return ConflictProblem("Device type code already exists."); var item = id is null ? new DeviceType() : await context.DeviceTypes.FindAsync([id.Value], token); if (item is null) return NotFound(); item.Code = code; item.Name = name; if (id is null) context.DeviceTypes.Add(item); await context.SaveChangesAsync(token); return Ok(new AdminItem(item.Id, code, name, true)); }

    [HttpPost("devices")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminDevice>> CreateDevice(SaveDeviceRequest request, CancellationToken token) => SaveDevice(null, request, token);
    [HttpPut("devices/{id:int}")]
    [Authorize(Policy = "ApiWrite")]
    public Task<ActionResult<AdminDevice>> UpdateDevice(int id, SaveDeviceRequest request, CancellationToken token) => SaveDevice(id, request, token);
    private async Task<ActionResult<AdminDevice>> SaveDevice(int? id, SaveDeviceRequest request, CancellationToken token) { var type = await context.DeviceTypes.FindAsync([request.DeviceTypeId], token); if (type is null) return Validation(nameof(request.DeviceTypeId), "Select a device type."); var code = request.Code.Trim().ToUpperInvariant(); var name = request.Name.Trim(); if (code.Length == 0 || name.Length == 0) return Validation("device", "Code and name are required."); if (await context.DeviceCatalogs.AnyAsync(x => x.Code == code && x.Id != id, token)) return ConflictProblem("Device code already exists."); var item = id is null ? new DeviceCatalog() : await context.DeviceCatalogs.FindAsync([id.Value], token); if (item is null) return NotFound(); item.DeviceTypeId = type.Id; item.Code = code; item.Name = name; item.IsActive = request.IsActive; if (id is null) context.DeviceCatalogs.Add(item); await context.SaveChangesAsync(token); return Ok(new AdminDevice(item.Id, item.DeviceTypeId, type.Name, code, name, item.IsActive)); }

    [HttpGet("users")]
    public async Task<AdminUsersResponse> Users(CancellationToken token)
    {
        var current = CurrentUserId(); var all = await users.Users.OrderBy(x => x.Email).ToListAsync(token); var ids = all.Select(x => x.Id).ToList(); var profiles = await context.EmployeeProfiles.Where(x => ids.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId, token); var rows = new List<AdminUser>();
        foreach (var user in all) { profiles.TryGetValue(user.Id, out var p); rows.Add(new AdminUser(user.Id, user.Email ?? user.UserName ?? "", p?.FullName ?? user.Email ?? "Staff member", p?.EmployeeNumber ?? "Profile incomplete", p?.Designation ?? "Not provided", p?.Department, p?.MobileNumber ?? "Not provided", p?.WorkPhoneNumber, p?.CanAccessRestrictedClinicalData ?? false, (await users.GetRolesAsync(user)).ToList(), IsLocked(user), user.Id == current)); }
        return new AdminUsersResponse(await roles.Roles.Where(x => x.Name != null).Select(x => x.Name!).OrderBy(x => x).ToListAsync(token), rows);
    }

    [HttpPost("users")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<AdminUser>> CreateUser(CreateAdminUserRequest request, CancellationToken token)
    {
        var error = await ValidateUserRequest(request.Email, request.EmployeeNumber, request.Roles, null, token); if (error is not null) return error;
        var email = request.Email.Trim(); var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true, LockoutEnabled = true }; var created = await users.CreateAsync(user, request.Password); if (!created.Succeeded) return IdentityProblem(created);
        var roleResult = await users.AddToRolesAsync(user, request.Roles.Distinct(StringComparer.OrdinalIgnoreCase)); if (!roleResult.Succeeded) { await users.DeleteAsync(user); return IdentityProblem(roleResult); }
        var profile = new EmployeeProfile { UserId = user.Id, EmployeeNumber = request.EmployeeNumber.Trim().ToUpperInvariant(), FullName = request.FullName.Trim(), Designation = request.Designation.Trim(), Department = Clean(request.Department), MobileNumber = request.MobileNumber.Trim(), WorkPhoneNumber = Clean(request.WorkPhoneNumber), CanAccessRestrictedClinicalData = request.CanAccessRestrictedClinicalData, CreatedBy = Actor() }; context.EmployeeProfiles.Add(profile); await context.SaveChangesAsync(token);
        return Created($"/api/v1/admin/users/{user.Id}", MapUser(user, profile, request.Roles, false));
    }

    [HttpPut("users/{id}/profile")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<AdminUser>> UpdateProfile(string id, UpdateAdminProfileRequest request, CancellationToken token) { var user = await users.FindByIdAsync(id); if (user is null) return NotFound(); if (await context.EmployeeProfiles.AnyAsync(x => x.UserId != id && x.EmployeeNumber == request.EmployeeNumber.Trim().ToUpper(), token)) return ConflictProblem("Employee number is already in use."); var p = await context.EmployeeProfiles.SingleOrDefaultAsync(x => x.UserId == id, token) ?? new EmployeeProfile { UserId = id, CreatedBy = Actor() }; if (p.Id == Guid.Empty) context.EmployeeProfiles.Add(p); else if (context.Entry(p).State == EntityState.Detached) context.EmployeeProfiles.Add(p); p.EmployeeNumber = request.EmployeeNumber.Trim().ToUpperInvariant(); p.FullName = request.FullName.Trim(); p.Designation = request.Designation.Trim(); p.Department = Clean(request.Department); p.MobileNumber = request.MobileNumber.Trim(); p.WorkPhoneNumber = Clean(request.WorkPhoneNumber); p.CanAccessRestrictedClinicalData = request.CanAccessRestrictedClinicalData; p.UpdatedAt = DateTime.UtcNow; p.UpdatedBy = Actor(); await context.SaveChangesAsync(token); return Ok(MapUser(user, p, await users.GetRolesAsync(user), IsLocked(user))); }

    [HttpPut("users/{id}/roles")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<IActionResult> UpdateRoles(string id, UpdateRolesRequest request) { var user = await users.FindByIdAsync(id); if (user is null) return NotFound(); if (request.Roles.Count == 0 || await InvalidRoles(request.Roles)) return Validation(nameof(request.Roles), "Select valid access roles."); var current = await users.GetRolesAsync(user); var removesAdmin = current.Contains("ADMIN") && !request.Roles.Contains("ADMIN", StringComparer.OrdinalIgnoreCase); if (removesAdmin && (CurrentUserId() == id || await IsLastActiveAdmin(user))) return ConflictProblem("The current or last active administrator must keep Administrator access."); var add = await users.AddToRolesAsync(user, request.Roles.Except(current, StringComparer.OrdinalIgnoreCase)); if (!add.Succeeded) return IdentityProblem(add); var remove = await users.RemoveFromRolesAsync(user, current.Except(request.Roles, StringComparer.OrdinalIgnoreCase)); return remove.Succeeded ? NoContent() : IdentityProblem(remove); }

    [HttpPut("users/{id}/lock")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<IActionResult> SetLock(string id, SetLockRequest request) { var user = await users.FindByIdAsync(id); if (user is null) return NotFound(); if (request.Locked && (CurrentUserId() == id || await users.IsInRoleAsync(user, "ADMIN") && await IsLastActiveAdmin(user))) return ConflictProblem("The current or last active administrator cannot be locked."); var enabled = await users.SetLockoutEnabledAsync(user, true); if (!enabled.Succeeded) return IdentityProblem(enabled); var result = await users.SetLockoutEndDateAsync(user, request.Locked ? DateTimeOffset.MaxValue : null); return result.Succeeded ? NoContent() : IdentityProblem(result); }

    [HttpPut("users/{id}/password")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<IActionResult> ResetPassword(string id, ResetPasswordRequest request) { var user = await users.FindByIdAsync(id); if (user is null) return NotFound(); if (string.IsNullOrWhiteSpace(request.NewPassword)) return Validation(nameof(request.NewPassword), "Password is required."); var result = await users.ResetPasswordAsync(user, await users.GeneratePasswordResetTokenAsync(user), request.NewPassword); return result.Succeeded ? NoContent() : IdentityProblem(result); }

    private async Task<ActionResult?> ValidateUserRequest(string email, string employeeNumber, IReadOnlyList<string> selectedRoles, string? id, CancellationToken token) { if (await users.FindByEmailAsync(email.Trim()) is not null) return ConflictProblem("Email address already exists."); if (await context.EmployeeProfiles.AnyAsync(x => x.UserId != id && x.EmployeeNumber == employeeNumber.Trim().ToUpper(), token)) return ConflictProblem("Employee number already exists."); if (selectedRoles.Count == 0 || await InvalidRoles(selectedRoles)) return Validation(nameof(selectedRoles), "Select valid access roles."); return null; }
    private async Task<bool> InvalidRoles(IEnumerable<string> selected) { var available = await roles.Roles.Where(x => x.Name != null).Select(x => x.Name!).ToListAsync(); return selected.Any(x => !available.Contains(x, StringComparer.OrdinalIgnoreCase)); }
    private async Task<bool> IsLastActiveAdmin(IdentityUser target) { var admins = await users.GetUsersInRoleAsync("ADMIN"); return admins.Count(x => !IsLocked(x)) <= 1 && admins.Any(x => x.Id == target.Id && !IsLocked(x)); }
    private async Task<bool> LookupExists(string type, string name, int? id, CancellationToken token) => type switch { "referral-sources" => await context.ReferralSources.AnyAsync(x => x.Name == name && x.Id != id, token), "nationalities" => await context.Nationalities.AnyAsync(x => x.Name == name && x.Id != id, token), "main-problem-types" => await context.MainProblemTypes.AnyAsync(x => x.Name == name && x.Id != id, token), _ => await context.CauseReasonTypes.AnyAsync(x => x.Name == name && x.Id != id, token) };
    private static bool IsLocked(IdentityUser user) => user.LockoutEnd > DateTimeOffset.UtcNow;
    private AdminUser MapUser(IdentityUser user, EmployeeProfile p, IEnumerable<string> userRoles, bool locked) => new(user.Id, user.Email ?? "", p.FullName, p.EmployeeNumber, p.Designation, p.Department, p.MobileNumber, p.WorkPhoneNumber, p.CanAccessRestrictedClinicalData, userRoles.ToList(), locked, CurrentUserId() == user.Id);
    private string? CurrentUserId() => users.GetUserId(User) ?? User.FindFirst("sub")?.Value;
    private BadRequestObjectResult Validation(string field, string message) => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] }) { Status = 400 });
    private ObjectResult ConflictProblem(string detail) => Problem(statusCode: 409, title: "The change conflicts with existing data.", detail: detail);
    private ObjectResult IdentityProblem(IdentityResult result) => Problem(statusCode: 400, title: "Identity operation failed.", detail: string.Join("; ", result.Errors.Select(x => x.Description)));
    private string Actor() => User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "API administrator";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
