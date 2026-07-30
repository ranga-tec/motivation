using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;

namespace Poms.Tests;

public class AppointmentAssigneeServiceTests
{
    [Fact]
    public async Task GetOptionsAsync_ListsActiveStaffAndPrioritizesClinicalUsers()
    {
        await using var database = await CreateDatabaseAsync();
        var clinician = await AddStaffAsync(
            database.Context,
            "clinician-id",
            "Clinical User",
            "Clinician",
            "EMP-001",
            role: "CLINICIAN");
        await AddStaffAsync(
            database.Context,
            "admin-id",
            "Admin User",
            "Administrator",
            "EMP-002");
        await AddStaffAsync(
            database.Context,
            "locked-id",
            "Locked User",
            "Prosthetist",
            "EMP-003",
            locked: true);

        var service = new AppointmentAssigneeService(database.Context);

        var options = await service.GetOptionsAsync();

        options.Should().HaveCount(2);
        options[0].UserId.Should().Be(clinician.Id);
        options[0].IsPreferred.Should().BeTrue();
        options.Should().NotContain(option => option.UserId == "locked-id");
    }

    [Fact]
    public async Task ResolveAsync_SelectedExistingUser_ReturnsCanonicalUserAndName()
    {
        await using var database = await CreateDatabaseAsync();
        var user = await AddStaffAsync(
            database.Context,
            "prosthetist-id",
            "Nimal Perera",
            "Prosthetist",
            "EMP-010");
        var service = new AppointmentAssigneeService(database.Context);
        var option = (await service.GetOptionsAsync()).Single();

        var result = await service.ResolveAsync(option.DisplayText, user.Id);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(user.Id);
        result.FullName.Should().Be("Nimal Perera");
    }

    [Fact]
    public async Task ResolveAsync_ExactSearchResultWithoutJavascript_StillLinksUser()
    {
        await using var database = await CreateDatabaseAsync();
        await AddStaffAsync(
            database.Context,
            "orthotist-id",
            "Ayesha Silva",
            "Orthotist",
            "EMP-011");
        var service = new AppointmentAssigneeService(database.Context);
        var option = (await service.GetOptionsAsync()).Single();

        var result = await service.ResolveAsync(option.DisplayText, null);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be("orthotist-id");
        result.FullName.Should().Be("Ayesha Silva");
    }

    [Fact]
    public async Task ResolveAsync_CustomName_AllowsUnlinkedAssignee()
    {
        await using var database = await CreateDatabaseAsync();
        var service = new AppointmentAssigneeService(database.Context);

        var result = await service.ResolveAsync("  Visiting Prosthetist  ", null);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().BeNull();
        result.FullName.Should().Be("Visiting Prosthetist");
    }

    [Fact]
    public async Task ResolveAsync_TamperedUserSelection_IsRejected()
    {
        await using var database = await CreateDatabaseAsync();
        await AddStaffAsync(
            database.Context,
            "user-id",
            "Valid User",
            "Orthotist",
            "EMP-012");
        var service = new AppointmentAssigneeService(database.Context);

        var result = await service.ResolveAsync("Different custom name", "user-id");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("unavailable");
    }

    private static async Task<IdentityUser> AddStaffAsync(
        PomsDbContext context,
        string userId,
        string fullName,
        string designation,
        string employeeNumber,
        string? role = null,
        bool locked = false)
    {
        var user = new IdentityUser
        {
            Id = userId,
            UserName = $"{userId}@poms.test",
            NormalizedUserName = $"{userId}@poms.test".ToUpperInvariant(),
            Email = $"{userId}@poms.test",
            NormalizedEmail = $"{userId}@poms.test".ToUpperInvariant(),
            LockoutEnd = locked ? DateTimeOffset.UtcNow.AddHours(1) : null
        };
        context.Users.Add(user);
        context.EmployeeProfiles.Add(new EmployeeProfile
        {
            UserId = userId,
            EmployeeNumber = employeeNumber,
            FullName = fullName,
            Designation = designation,
            MobileNumber = "0770000000"
        });

        if (role is not null)
        {
            var identityRole = new IdentityRole
            {
                Id = $"{role}-id",
                Name = role,
                NormalizedName = role.ToUpperInvariant()
            };
            context.Roles.Add(identityRole);
            context.UserRoles.Add(new IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = identityRole.Id
            });
        }

        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<TestDatabase> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PomsDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new PomsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new TestDatabase(connection, context);
    }

    private sealed class TestDatabase(
        SqliteConnection connection,
        PomsDbContext context) : IAsyncDisposable
    {
        public PomsDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
