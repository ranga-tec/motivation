using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class AdminController : Controller
{
    private static readonly string[] LookupTypes = { "referral-sources", "nationalities", "main-problem-types", "cause-reason-types" };

    private readonly PomsDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        PomsDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public IActionResult Index() => View();

    // ---------------- Locations (Center) ----------------

    public async Task<IActionResult> Locations()
    {
        var centers = await _context.Centers.Include(c => c.District).ToListAsync();
        return View(centers);
    }

    public async Task<IActionResult> LocationCreate()
    {
        await PopulateDistricts();
        return View(new LocationViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocationCreate(LocationViewModel model)
    {
        if (ModelState.IsValid)
        {
            _context.Centers.Add(new Center
            {
                DistrictId = model.DistrictId,
                Code = model.Code,
                Name = model.Name,
                Address = model.Address,
                Phone = model.Phone,
                IsActive = model.IsActive,
                RequiresPatientNumberFlag = model.RequiresPatientNumberFlag,
                PatientNumberFlagCode = model.RequiresPatientNumberFlag ? model.PatientNumberFlagCode : null
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Location added.";
            return RedirectToAction(nameof(Locations));
        }

        await PopulateDistricts();
        return View(model);
    }

    public async Task<IActionResult> LocationEdit(int id)
    {
        var center = await _context.Centers.FindAsync(id);
        if (center == null) return NotFound();

        await PopulateDistricts();
        return View(new LocationViewModel
        {
            Id = center.Id, DistrictId = center.DistrictId, Code = center.Code, Name = center.Name,
            Address = center.Address, Phone = center.Phone, IsActive = center.IsActive,
            RequiresPatientNumberFlag = center.RequiresPatientNumberFlag, PatientNumberFlagCode = center.PatientNumberFlagCode
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocationEdit(int id, LocationViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var center = await _context.Centers.FindAsync(id);
            if (center == null) return NotFound();

            center.DistrictId = model.DistrictId;
            center.Code = model.Code;
            center.Name = model.Name;
            center.Address = model.Address;
            center.Phone = model.Phone;
            center.IsActive = model.IsActive;
            center.RequiresPatientNumberFlag = model.RequiresPatientNumberFlag;
            center.PatientNumberFlagCode = model.RequiresPatientNumberFlag ? model.PatientNumberFlagCode : null;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Location updated.";
            return RedirectToAction(nameof(Locations));
        }

        await PopulateDistricts();
        return View(model);
    }

    // ---------------- Lookup tables (ReferralSources / Nationalities / MainProblemTypes / CauseReasonTypes) ----------------

    public async Task<IActionResult> Lookup(string type)
    {
        if (!LookupTypes.Contains(type)) return NotFound();
        ViewBag.LookupType = type;
        ViewBag.LookupTitle = LookupTitle(type);
        return View(await GetLookupItems(type));
    }

    public IActionResult LookupCreate(string type)
    {
        if (!LookupTypes.Contains(type)) return NotFound();
        ViewBag.LookupType = type;
        ViewBag.LookupTitle = LookupTitle(type);
        return View(new LookupItemViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LookupCreate(string type, LookupItemViewModel model)
    {
        if (!LookupTypes.Contains(type)) return NotFound();

        if (ModelState.IsValid)
        {
            await AddLookupItem(type, model.Name, model.IsActive);
            TempData["Success"] = "Item added.";
            return RedirectToAction(nameof(Lookup), new { type });
        }

        ViewBag.LookupType = type;
        ViewBag.LookupTitle = LookupTitle(type);
        return View(model);
    }

    public async Task<IActionResult> LookupEdit(string type, int id)
    {
        if (!LookupTypes.Contains(type)) return NotFound();

        var item = (await GetLookupItems(type)).FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        ViewBag.LookupType = type;
        ViewBag.LookupTitle = LookupTitle(type);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LookupEdit(string type, int id, LookupItemViewModel model)
    {
        if (!LookupTypes.Contains(type) || id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await UpdateLookupItem(type, id, model.Name, model.IsActive);
            TempData["Success"] = "Item updated.";
            return RedirectToAction(nameof(Lookup), new { type });
        }

        ViewBag.LookupType = type;
        ViewBag.LookupTitle = LookupTitle(type);
        return View(model);
    }

    // ---------------- Provinces / Districts / Cities ----------------

    public async Task<IActionResult> Provinces() => View(await _context.Provinces.ToListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProvinceCreate(string code, string name)
    {
        _context.Provinces.Add(new Province { Code = code, Name = name });
        await _context.SaveChangesAsync();
        TempData["Success"] = "Province added.";
        return RedirectToAction(nameof(Provinces));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProvinceEdit(int id, string code, string name)
    {
        var province = await _context.Provinces.FindAsync(id);
        if (province == null) return NotFound();

        province.Code = code;
        province.Name = name;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Province updated.";
        return RedirectToAction(nameof(Provinces));
    }

    public async Task<IActionResult> Districts()
    {
        var districts = await _context.Districts.Include(d => d.Province).ToListAsync();
        ViewBag.Provinces = new SelectList(await _context.Provinces.ToListAsync(), "Id", "Name");
        return View(districts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DistrictCreate(int provinceId, string code, string name)
    {
        _context.Districts.Add(new District { ProvinceId = provinceId, Code = code, Name = name });
        await _context.SaveChangesAsync();
        TempData["Success"] = "District added.";
        return RedirectToAction(nameof(Districts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DistrictEdit(int id, int provinceId, string code, string name)
    {
        var district = await _context.Districts.FindAsync(id);
        if (district == null) return NotFound();

        district.ProvinceId = provinceId;
        district.Code = code;
        district.Name = name;
        await _context.SaveChangesAsync();
        TempData["Success"] = "District updated.";
        return RedirectToAction(nameof(Districts));
    }

    public async Task<IActionResult> Cities()
    {
        var cities = await _context.Cities.Include(c => c.District).ToListAsync();
        ViewBag.Districts = new SelectList(await _context.Districts.ToListAsync(), "Id", "Name");
        return View(cities);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CityCreate(int districtId, string name)
    {
        _context.Cities.Add(new City { DistrictId = districtId, Name = name });
        await _context.SaveChangesAsync();
        TempData["Success"] = "City added.";
        return RedirectToAction(nameof(Cities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CityEdit(int id, int districtId, string name, bool isActive)
    {
        var city = await _context.Cities.FindAsync(id);
        if (city == null) return NotFound();

        city.DistrictId = districtId;
        city.Name = name;
        city.IsActive = isActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = "City updated.";
        return RedirectToAction(nameof(Cities));
    }

    // ---------------- Users ----------------

    public async Task<IActionResult> Users()
    {
        return View(await BuildUserManagementViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserCreate([Bind(Prefix = "NewUser")] CreateUserViewModel model)
    {
        var roles = await GetAvailableRoles();
        ValidateSelectedRoles(model.Roles, roles, "NewUser.Roles");

        if (await _userManager.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError("NewUser.Email", "An account with this email address already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Users), await BuildUserManagementViewModel(model));
        }

        var email = model.Email.Trim();
        var user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult, "NewUser.Password");
            return View(nameof(Users), await BuildUserManagementViewModel(model));
        }

        var roleResult = await _userManager.AddToRolesAsync(user, model.Roles.Distinct(StringComparer.OrdinalIgnoreCase));
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            AddIdentityErrors(roleResult, "NewUser.Roles");
            return View(nameof(Users), await BuildUserManagementViewModel(model));
        }

        _logger.LogInformation(
            "Administrator {Administrator} created user {UserEmail} with roles {Roles}.",
            User.Identity?.Name,
            email,
            string.Join(", ", model.Roles));
        TempData["Success"] = $"Account created for {email}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserChangeRole(UserRoleUpdateViewModel model)
    {
        var availableRoles = await GetAvailableRoles();
        ValidateSelectedRoles(model.Roles, availableRoles, nameof(model.Roles));
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelStateError();
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var requestedRoles = model.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var removesAdmin = currentRoles.Contains("ADMIN") &&
            !requestedRoles.Contains("ADMIN", StringComparer.OrdinalIgnoreCase);

        if (removesAdmin && await IsLastActiveAdministrator(user))
        {
            TempData["Error"] = "The last active administrator must keep Administrator access.";
            return RedirectToAction(nameof(Users));
        }

        if (_userManager.GetUserId(User) == user.Id && removesAdmin)
        {
            TempData["Error"] = "You cannot remove your own Administrator access.";
            return RedirectToAction(nameof(Users));
        }

        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                TempData["Error"] = IdentityErrorMessage(addResult);
                return RedirectToAction(nameof(Users));
            }
        }

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = IdentityErrorMessage(removeResult);
                return RedirectToAction(nameof(Users));
            }
        }

        _logger.LogInformation(
            "Administrator {Administrator} changed roles for {UserEmail} to {Roles}.",
            User.Identity?.Name,
            user.Email,
            string.Join(", ", requestedRoles));
        TempData["Success"] = $"Access updated for {user.Email}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserSetLock(string userId, bool lockAccount)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (lockAccount && _userManager.GetUserId(User) == user.Id)
        {
            TempData["Error"] = "You cannot lock your own account.";
            return RedirectToAction(nameof(Users));
        }

        if (lockAccount && await _userManager.IsInRoleAsync(user, "ADMIN") &&
            await IsLastActiveAdministrator(user))
        {
            TempData["Error"] = "The last active administrator cannot be locked.";
            return RedirectToAction(nameof(Users));
        }

        var enabledResult = await _userManager.SetLockoutEnabledAsync(user, true);
        var lockResult = enabledResult.Succeeded
            ? await _userManager.SetLockoutEndDateAsync(
                user,
                lockAccount ? DateTimeOffset.MaxValue : null)
            : enabledResult;

        if (!lockResult.Succeeded)
        {
            TempData["Error"] = IdentityErrorMessage(lockResult);
            return RedirectToAction(nameof(Users));
        }

        _logger.LogInformation(
            "Administrator {Administrator} {Action} user {UserEmail}.",
            User.Identity?.Name,
            lockAccount ? "locked" : "unlocked",
            user.Email);
        TempData["Success"] = lockAccount
            ? $"{user.Email} can no longer sign in."
            : $"{user.Email} can sign in again.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserResetPassword(ResetUserPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstModelStateError();
            return RedirectToAction(nameof(Users));
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user is null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            TempData["Error"] = IdentityErrorMessage(result);
            return RedirectToAction(nameof(Users));
        }

        _logger.LogInformation(
            "Administrator {Administrator} reset the password for {UserEmail}.",
            User.Identity?.Name,
            user.Email);
        TempData["Success"] = $"A new temporary password was set for {user.Email}.";
        return RedirectToAction(nameof(Users));
    }

    // ---------------- Helpers ----------------

    private async Task PopulateDistricts()
    {
        ViewBag.Districts = new SelectList(await _context.Districts.ToListAsync(), "Id", "Name");
    }

    private async Task<UserManagementViewModel> BuildUserManagementViewModel(
        CreateUserViewModel? newUser = null)
    {
        var currentUserId = _userManager.GetUserId(User);
        var users = await _userManager.Users.OrderBy(user => user.Email).ToListAsync();
        var rows = new List<UserAccessRowViewModel>(users.Count);

        foreach (var user in users)
        {
            rows.Add(new UserAccessRowViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "Unknown account",
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                IsLocked = IsUserLocked(user),
                IsCurrentUser = user.Id == currentUserId
            });
        }

        return new UserManagementViewModel
        {
            NewUser = newUser ?? new CreateUserViewModel(),
            Users = rows,
            AvailableRoles = await GetAvailableRoles()
        };
    }

    private async Task<List<string>> GetAvailableRoles()
    {
        return await _roleManager.Roles
            .Where(role => role.Name != null)
            .Select(role => role.Name!)
            .OrderBy(role => role)
            .ToListAsync();
    }

    private void ValidateSelectedRoles(
        IEnumerable<string>? selectedRoles,
        IReadOnlyCollection<string> availableRoles,
        string modelStateKey)
    {
        var selected = selectedRoles?.Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
        if (selected.Length == 0)
        {
            ModelState.AddModelError(modelStateKey, "Select at least one access role.");
            return;
        }

        if (selected.Any(role => !availableRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(modelStateKey, "One or more selected access roles are invalid.");
        }
    }

    private async Task<bool> IsLastActiveAdministrator(IdentityUser target)
    {
        var administrators = await _userManager.GetUsersInRoleAsync("ADMIN");
        return administrators.Count(user => !IsUserLocked(user)) <= 1 &&
            administrators.Any(user => user.Id == target.Id && !IsUserLocked(user));
    }

    private static bool IsUserLocked(IdentityUser user)
    {
        return user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
    }

    private void AddIdentityErrors(IdentityResult result, string key)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(key, error.Description);
        }
    }

    private string FirstModelStateError()
    {
        return ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? "The requested change could not be completed.";
    }

    private static string IdentityErrorMessage(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }

    private static string LookupTitle(string type) => type switch
    {
        "referral-sources" => "Referral Sources",
        "nationalities" => "Nationalities",
        "main-problem-types" => "Main Problem Types",
        "cause-reason-types" => "Cause / Reason / Pathology Types",
        _ => type
    };

    private async Task<List<LookupItemViewModel>> GetLookupItems(string type) => type switch
    {
        "referral-sources" => await _context.ReferralSources.Select(r => new LookupItemViewModel { Id = r.Id, Name = r.Name, IsActive = r.IsActive }).ToListAsync(),
        "nationalities" => await _context.Nationalities.Select(n => new LookupItemViewModel { Id = n.Id, Name = n.Name, IsActive = n.IsActive }).ToListAsync(),
        "main-problem-types" => await _context.MainProblemTypes.Select(m => new LookupItemViewModel { Id = m.Id, Name = m.Name, IsActive = m.IsActive }).ToListAsync(),
        "cause-reason-types" => await _context.CauseReasonTypes.Select(c => new LookupItemViewModel { Id = c.Id, Name = c.Name, IsActive = c.IsActive }).ToListAsync(),
        _ => new List<LookupItemViewModel>()
    };

    private async Task AddLookupItem(string type, string name, bool isActive)
    {
        switch (type)
        {
            case "referral-sources": _context.ReferralSources.Add(new ReferralSource { Name = name, IsActive = isActive }); break;
            case "nationalities": _context.Nationalities.Add(new Nationality { Name = name, IsActive = isActive }); break;
            case "main-problem-types": _context.MainProblemTypes.Add(new MainProblemType { Name = name, IsActive = isActive }); break;
            case "cause-reason-types": _context.CauseReasonTypes.Add(new CauseReasonType { Name = name, IsActive = isActive }); break;
        }
        await _context.SaveChangesAsync();
    }

    private async Task UpdateLookupItem(string type, int id, string name, bool isActive)
    {
        switch (type)
        {
            case "referral-sources":
                var rs = await _context.ReferralSources.FindAsync(id);
                if (rs != null) { rs.Name = name; rs.IsActive = isActive; }
                break;
            case "nationalities":
                var n = await _context.Nationalities.FindAsync(id);
                if (n != null) { n.Name = name; n.IsActive = isActive; }
                break;
            case "main-problem-types":
                var m = await _context.MainProblemTypes.FindAsync(id);
                if (m != null) { m.Name = name; m.IsActive = isActive; }
                break;
            case "cause-reason-types":
                var c = await _context.CauseReasonTypes.FindAsync(id);
                if (c != null) { c.Name = name; c.IsActive = isActive; }
                break;
        }
        await _context.SaveChangesAsync();
    }
}
