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

    public AdminController(PomsDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
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
        var users = await _userManager.Users.ToListAsync();
        var rows = new List<(IdentityUser User, IList<string> Roles)>();
        foreach (var user in users)
        {
            rows.Add((user, await _userManager.GetRolesAsync(user)));
        }

        ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserCreate(string email, string password, string role)
    {
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, role);
            TempData["Success"] = "User created.";
        }
        else
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserChangeRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, role);
            TempData["Success"] = "Role updated.";
        }
        return RedirectToAction(nameof(Users));
    }

    // ---------------- Helpers ----------------

    private async Task PopulateDistricts()
    {
        ViewBag.Districts = new SelectList(await _context.Districts.ToListAsync(), "Id", "Name");
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
