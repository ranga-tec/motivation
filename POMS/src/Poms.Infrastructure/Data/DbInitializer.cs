using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Poms.Domain.Entities;

namespace Poms.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedUsersAndRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var context = serviceProvider.GetRequiredService<PomsDbContext>();

        // Create Roles - ADMIN, CLINICIAN, DATA_ENTRY, VIEWER, MANAGEMENT
        // (= Admin, Clinical user, Registration user, Report user, Management user per PRD 4.1)
        string[] roleNames = { "ADMIN", "CLINICIAN", "DATA_ENTRY", "VIEWER", "MANAGEMENT" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // Create Default Users
        var defaultUsers = new[]
        {
            new { Email = "admin@poms.lk", Password = "Admin@123", Role = "ADMIN" },
            new { Email = "clinician@poms.lk", Password = "Clinic@123", Role = "CLINICIAN" },
            new { Email = "registrar@poms.lk", Password = "Data@123", Role = "DATA_ENTRY" },
            new { Email = "viewer@poms.lk", Password = "View@123", Role = "VIEWER" },
            new { Email = "management@poms.lk", Password = "Manage@123", Role = "MANAGEMENT" }
        };

        foreach (var userData in defaultUsers)
        {
            var user = await userManager.FindByEmailAsync(userData.Email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, userData.Role);
                }
            }
        }

        var users = await userManager.Users.ToListAsync();
        var existingProfileUserIds = await context.EmployeeProfiles
            .Select(profile => profile.UserId)
            .ToListAsync();

        foreach (var user in users.Where(user => !existingProfileUserIds.Contains(user.Id)))
        {
            var roles = await userManager.GetRolesAsync(user);
            var localPart = (user.Email ?? user.UserName ?? "Staff").Split('@')[0];
            var displayName = string.Join(
                " ",
                localPart.Split(['.', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));

            context.EmployeeProfiles.Add(new EmployeeProfile
            {
                UserId = user.Id,
                EmployeeNumber = $"LEGACY-{user.Id.Replace("-", string.Empty)[..8].ToUpperInvariant()}",
                FullName = string.IsNullOrWhiteSpace(displayName) ? "Staff member" : displayName,
                Designation = roles.FirstOrDefault() ?? "Staff",
                Department = "Not provided",
                MobileNumber = "Not provided",
                CanAccessRestrictedClinicalData = false,
                CreatedBy = "System migration"
            });
        }

        await context.SaveChangesAsync();
    }
}
