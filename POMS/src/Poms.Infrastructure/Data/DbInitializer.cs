using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Poms.Domain.Entities;

namespace Poms.Infrastructure.Data;

public static class DbInitializer
{
    private static readonly SeedUser[] DemoUsers =
    {
        new("admin@poms.lk", "Admin@123", "ADMIN"),
        new("clinician@poms.lk", "Clinic@123", "CLINICIAN"),
        new("registrar@poms.lk", "Data@123", "DATA_ENTRY"),
        new("viewer@poms.lk", "View@123", "VIEWER"),
        new("management@poms.lk", "Manage@123", "MANAGEMENT")
    };

    public static async Task SeedUsersAndRolesAsync(
        IServiceProvider serviceProvider,
        bool seedDemoUsers = true,
        string? bootstrapAdminEmail = null,
        string? bootstrapAdminPassword = null)
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
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(FormatIdentityErrors($"Could not create role {roleName}", roleResult));
            }
        }

        var usersToCreate = seedDemoUsers
            ? DemoUsers
            : BuildProductionBootstrapUsers(bootstrapAdminEmail, bootstrapAdminPassword);

        if (!seedDemoUsers)
            await RejectKnownDemoPasswordsAsync(userManager);

        if (!seedDemoUsers && usersToCreate.Length == 0 && !await userManager.Users.AnyAsync())
        {
            throw new InvalidOperationException(
                "A fresh production database requires BootstrapAdmin:Email and BootstrapAdmin:Password.");
        }

        foreach (var userData in usersToCreate)
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
                if (!result.Succeeded)
                    throw new InvalidOperationException(FormatIdentityErrors("Could not create bootstrap user", result));

            }

            if (!await userManager.IsInRoleAsync(user, userData.Role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, userData.Role);
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(FormatIdentityErrors("Could not assign bootstrap role", roleResult));
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

    private static SeedUser[] BuildProductionBootstrapUsers(string? email, string? password)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPassword = !string.IsNullOrWhiteSpace(password);
        if (hasEmail != hasPassword)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Email and BootstrapAdmin:Password must either both be configured or both be omitted.");
        }

        return hasEmail && hasPassword
            ? new[] { new SeedUser(email!, password!, "ADMIN") }
            : Array.Empty<SeedUser>();
    }

    private static async Task RejectKnownDemoPasswordsAsync(UserManager<IdentityUser> userManager)
    {
        foreach (var demoUser in DemoUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(demoUser.Email);
            if (existingUser is not null && await userManager.CheckPasswordAsync(existingUser, demoUser.Password))
            {
                throw new InvalidOperationException(
                    $"Production account {demoUser.Email} still uses its public development password. " +
                    "Reset or remove that account before deployment.");
            }
        }
    }

    private static string FormatIdentityErrors(string message, IdentityResult result) =>
        $"{message}: {string.Join("; ", result.Errors.Select(error => error.Description))}";

    private sealed record SeedUser(string Email, string Password, string Role);
}
