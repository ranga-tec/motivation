using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Poms.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedUsersAndRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();

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
            new { Email = "admin@poms.lk", Password = "Admin@123", Role = "ADMIN" },
            new { Email = "clinician@poms.lk", Password = "Clinic@123", Role = "CLINICIAN" },
            new { Email = "registrar@poms.lk", Password = "Data@123", Role = "DATA_ENTRY" },
            new { Email = "viewer@poms.lk", Password = "View@123", Role = "VIEWER" }
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
    }
}
