using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly PomsDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        PomsDbContext context,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    // GET: Users
    public async Task<IActionResult> Index(string searchString, bool? isActive)
    {
        var query = _userManager.Users.Include(u => u.Center).AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(u =>
                u.Email!.Contains(searchString) ||
                u.FirstName.Contains(searchString) ||
                u.LastName.Contains(searchString));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var users = await query.OrderBy(u => u.Email).ToListAsync();

        // Get roles for each user
        var userViewModels = new List<UserListViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserListViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CenterName = user.Center?.Name,
                Roles = roles.ToList(),
                LastLoginAt = user.LastLoginAt
            });
        }

        ViewBag.SearchString = searchString;
        ViewBag.IsActive = isActive;
        return View(userViewModels);
    }

    // GET: Users/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View(new CreateUserViewModel());
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                IsActive = model.IsActive,
                CenterId = model.CenterId,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                _logger.LogInformation("User {Email} created by {Admin}", model.Email, User.Identity?.Name);
                TempData["Success"] = "User created successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.Users.Include(u => u.Center)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CenterId = user.CenterId,
            Role = roles.FirstOrDefault() ?? ""
        };

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.IsActive = model.IsActive;
            user.CenterId = model.CenterId;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update role
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!string.IsNullOrEmpty(model.Role))
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                }

                _logger.LogInformation("User {Email} updated by {Admin}", user.Email, User.Identity?.Name);
                TempData["Success"] = "User updated successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        await PopulateDropdowns();
        return View(model);
    }

    // POST: Users/ToggleStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")} successfully";
        return RedirectToAction(nameof(Index));
    }

    // GET: Users/AssignRoles/5
    public async Task<IActionResult> AssignRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var allRoles = await _roleManager.Roles.ToListAsync();
        var userRoles = await _userManager.GetRolesAsync(user);

        var model = new AssignRolesViewModel
        {
            UserId = user.Id,
            Email = user.Email!,
            FullName = user.FullName,
            Roles = allRoles.Select(r => new RoleSelection
            {
                RoleName = r.Name!,
                IsSelected = userRoles.Contains(r.Name!)
            }).ToList()
        };

        return View(model);
    }

    // POST: Users/AssignRoles
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoles(AssignRolesViewModel model)
    {
        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName);
        await _userManager.AddToRolesAsync(user, selectedRoles);

        TempData["Success"] = "Roles updated successfully";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Centers = new SelectList(
            await _context.Centers.Where(c => c.IsActive).ToListAsync(),
            "Id", "Name");

        ViewBag.Roles = new SelectList(
            await _roleManager.Roles.ToListAsync(),
            "Name", "Name");
    }
}
