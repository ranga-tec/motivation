using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Poms.Domain.Entities;

namespace Poms.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedUsersAndRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create Roles
        string[] roleNames = { "ADMIN", "CLINICIAN", "DATA_ENTRY", "VIEWER" };
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
            new { Email = "admin@poms.lk", Password = "Admin@123", Role = "ADMIN", FirstName = "System", LastName = "Administrator" },
            new { Email = "clinician@poms.lk", Password = "Clinic@123", Role = "CLINICIAN", FirstName = "Default", LastName = "Clinician" },
            new { Email = "registrar@poms.lk", Password = "Data@123", Role = "DATA_ENTRY", FirstName = "Data", LastName = "Entry" },
            new { Email = "viewer@poms.lk", Password = "View@123", Role = "VIEWER", FirstName = "Report", LastName = "Viewer" }
        };

        foreach (var userData in defaultUsers)
        {
            var user = await userManager.FindByEmailAsync(userData.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userData.Email,
                    Email = userData.Email,
                    EmailConfirmed = true,
                    FirstName = userData.FirstName,
                    LastName = userData.LastName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, userData.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, userData.Role);
                }
            }
        }
    }
}
