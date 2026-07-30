using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;

namespace Poms.Tests;

public class RestrictedAccessScopeTests
{
    private const string CurrentUser = "clinician@poms.lk";
    private const string OtherUser = "other@poms.lk";

    [Fact]
    public void CanAccess_AllowsUnrestrictedRecord()
    {
        var scope = CreateScope();

        scope.CanAccess(isRestricted: false, createdBy: OtherUser).Should().BeTrue();
    }

    [Fact]
    public void CanAccess_DeniesRestrictedRecordCreatedByAnotherUser()
    {
        var scope = CreateScope();

        scope.CanAccess(isRestricted: true, createdBy: OtherUser).Should().BeFalse();
    }

    [Fact]
    public void CanAccess_AllowsRestrictedRecordCreatedByCurrentUser_IgnoringCase()
    {
        var scope = CreateScope();

        scope.CanAccess(isRestricted: true, createdBy: CurrentUser.ToUpperInvariant())
            .Should().BeTrue();
    }

    [Fact]
    public void CanAccess_AllowsAllRecordsWhenScopeHasGlobalPermission()
    {
        var scope = CreateScope(hasGlobalAccess: true);

        scope.CanAccess(isRestricted: true, createdBy: OtherUser).Should().BeTrue();
    }

    [Fact]
    public void FilterEpisodes_ExcludesAnotherUsersRestrictedEpisode()
    {
        var ownRestricted = new Episode { IsRestricted = true, CreatedBy = CurrentUser };
        var otherRestricted = new Episode { IsRestricted = true, CreatedBy = OtherUser };
        var unrestricted = new Episode { IsRestricted = false, CreatedBy = OtherUser };

        var visible = CreateScope()
            .Filter(new[] { ownRestricted, otherRestricted, unrestricted }.AsQueryable())
            .ToList();

        visible.Should().BeEquivalentTo(new[] { ownRestricted, unrestricted });
    }

    [Fact]
    public void FilterAssessment_RequiresAccessToBothAssessmentAndParentEpisode()
    {
        var publicEpisode = new Episode { IsRestricted = false, CreatedBy = OtherUser };
        var otherRestrictedEpisode = new Episode { IsRestricted = true, CreatedBy = OtherUser };
        var ownRestrictedEpisode = new Episode { IsRestricted = true, CreatedBy = CurrentUser };

        var visiblePublic = new Assessment
        {
            IsRestricted = false,
            CreatedBy = OtherUser,
            Episode = publicEpisode
        };
        var hiddenRestrictedAssessment = new Assessment
        {
            IsRestricted = true,
            CreatedBy = OtherUser,
            Episode = publicEpisode
        };
        var hiddenByParent = new Assessment
        {
            IsRestricted = false,
            CreatedBy = OtherUser,
            Episode = otherRestrictedEpisode
        };
        var visibleOwnRestricted = new Assessment
        {
            IsRestricted = true,
            CreatedBy = CurrentUser,
            Episode = ownRestrictedEpisode
        };

        var visible = CreateScope()
            .Filter(new[]
            {
                visiblePublic,
                hiddenRestrictedAssessment,
                hiddenByParent,
                visibleOwnRestricted
            }.AsQueryable())
            .ToList();

        visible.Should().BeEquivalentTo(new[] { visiblePublic, visibleOwnRestricted });
    }

    [Fact]
    public void FilterDocuments_EnforcesRecordAndParentRestrictions()
    {
        var publicPatientDocument = new PatientDocument
        {
            IsRestricted = false,
            CreatedBy = OtherUser
        };
        var hiddenPatientDocument = new PatientDocument
        {
            IsRestricted = true,
            CreatedBy = OtherUser
        };
        var ownPatientDocument = new PatientDocument
        {
            IsRestricted = true,
            CreatedBy = CurrentUser
        };

        var visiblePatientDocuments = CreateScope()
            .Filter(new[]
            {
                publicPatientDocument,
                hiddenPatientDocument,
                ownPatientDocument
            }.AsQueryable())
            .ToList();

        visiblePatientDocuments.Should().BeEquivalentTo(new[]
        {
            publicPatientDocument,
            ownPatientDocument
        });

        var hiddenEpisodeDocument = new EpisodeDocument
        {
            IsRestricted = false,
            CreatedBy = OtherUser,
            Episode = new Episode { IsRestricted = true, CreatedBy = OtherUser }
        };
        var ownEpisodeDocument = new EpisodeDocument
        {
            IsRestricted = true,
            CreatedBy = CurrentUser,
            Episode = new Episode { IsRestricted = true, CreatedBy = CurrentUser }
        };

        var visibleEpisodeDocuments = CreateScope()
            .Filter(new[] { hiddenEpisodeDocument, ownEpisodeDocument }.AsQueryable())
            .ToList();

        visibleEpisodeDocuments.Should().ContainSingle()
            .Which.Should().BeSameAs(ownEpisodeDocument);
    }

    [Fact]
    public void FilterAppointments_HidesAppointmentsLinkedToInaccessibleEpisode()
    {
        var generalAppointment = new Appointment();
        var hiddenAppointment = new Appointment
        {
            Episode = new Episode { IsRestricted = true, CreatedBy = OtherUser }
        };
        var ownRestrictedAppointment = new Appointment
        {
            Episode = new Episode
            {
                IsRestricted = true,
                CreatedBy = CurrentUser.ToUpperInvariant()
            }
        };

        var visible = CreateScope()
            .Filter(new[]
            {
                generalAppointment,
                hiddenAppointment,
                ownRestrictedAppointment
            }.AsQueryable())
            .ToList();

        visible.Should().BeEquivalentTo(new[]
        {
            generalAppointment,
            ownRestrictedAppointment
        });
    }

    [Fact]
    public async Task GetScopeAsync_UsesExplicitEmployeeRestrictedDataPermission()
    {
        await using var database = await CreateDatabaseAsync();
        const string userId = "permitted-user";
        database.Context.Users.Add(new IdentityUser
        {
            Id = userId,
            UserName = CurrentUser,
            NormalizedUserName = CurrentUser.ToUpperInvariant(),
            Email = CurrentUser,
            NormalizedEmail = CurrentUser.ToUpperInvariant()
        });
        database.Context.EmployeeProfiles.Add(new EmployeeProfile
        {
            UserId = userId,
            EmployeeNumber = "EMP-001",
            FullName = "Test Clinician",
            Designation = "Clinician",
            MobileNumber = "0770000000",
            CanAccessRestrictedClinicalData = true
        });
        await database.Context.SaveChangesAsync();

        var principal = CreatePrincipal(userId, CurrentUser);
        var service = new RestrictedAccessService(
            database.Context,
            new HttpContextAccessor());

        var scope = await service.GetScopeAsync(principal);

        scope.HasGlobalAccess.Should().BeTrue();
    }

    [Fact]
    public async Task AuditAsync_OnlyStoresEventsForRestrictedContent()
    {
        await using var database = await CreateDatabaseAsync();
        var service = new RestrictedAccessService(
            database.Context,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            });
        var scope = CreateScope();

        await service.AuditAsync(
            scope, "View", nameof(Episode), Guid.NewGuid(),
            isRestricted: false, allowed: true);
        await service.AuditAsync(
            scope, "ViewDenied", nameof(Episode), Guid.NewGuid(),
            isRestricted: true, allowed: false);

        var audit = await database.Context.AuditLogs.SingleAsync();
        audit.Action.Should().Be("ViewDenied");
        audit.Changes.Should().Contain("\"Allowed\":false");
    }

    private static RestrictedAccessScope CreateScope(bool hasGlobalAccess = false)
    {
        return new RestrictedAccessScope(
            UserId: "test-user-id",
            UserName: CurrentUser,
            HasGlobalAccess: hasGlobalAccess);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, string userName)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName)
        ], "Test");
        return new ClaimsPrincipal(identity);
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
