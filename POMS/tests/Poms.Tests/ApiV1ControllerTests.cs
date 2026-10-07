using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api.V1.Contracts;
using Poms.Web.Api.V1.Controllers;

namespace Poms.Tests;

public sealed class ApiV1ControllerTests
{
    [Fact]
    public async Task PatientsList_ReturnsStablePagedContract()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var controller = new PatientsApiController(database.Context);

        var action = await controller.List(
            new PatientListQuery { Search = patient.PatientNumber, PageSize = 10 },
            CancellationToken.None);

        var response = action.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResponse<PatientSummaryResponse>>().Subject;
        response.TotalCount.Should().Be(1);
        response.Items.Should().ContainSingle()
            .Which.PatientNumber.Should().Be(patient.PatientNumber);
    }

    [Fact]
    public async Task PatientsGet_ReturnsNotFoundForUnknownPatient()
    {
        await using var database = await CreateDatabaseAsync();
        var controller = new PatientsApiController(database.Context);

        var action = await controller.Get(Guid.NewGuid(), CancellationToken.None);

        action.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AppointmentsList_UsesRestrictedAccessScope()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var visibleEpisode = CreateEpisode(patient, isRestricted: false, "other@poms.lk");
        var hiddenEpisode = CreateEpisode(patient, isRestricted: true, "other@poms.lk");
        database.Context.Episodes.AddRange(visibleEpisode, hiddenEpisode);
        database.Context.Appointments.AddRange(
            CreateAppointment(patient, visibleEpisode),
            CreateAppointment(patient, hiddenEpisode));
        await database.Context.SaveChangesAsync();

        var restrictedAccess = new Mock<IRestrictedAccessService>();
        restrictedAccess
            .Setup(service => service.GetScopeAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new RestrictedAccessScope("user-id", "clinician@poms.lk", false));
        var controller = SetUser(new AppointmentsApiController(database.Context, restrictedAccess.Object));

        var action = await controller.List(new AppointmentListQuery(), CancellationToken.None);

        var response = action.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResponse<AppointmentResponse>>().Subject;
        response.TotalCount.Should().Be(1);
        response.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task AppointmentsList_RejectsReversedDateRange()
    {
        await using var database = await CreateDatabaseAsync();
        var restrictedAccess = new Mock<IRestrictedAccessService>();
        var controller = SetUser(new AppointmentsApiController(database.Context, restrictedAccess.Object));

        var action = await controller.List(
            new AppointmentListQuery
            {
                DateFrom = new DateOnly(2026, 10, 8),
                DateTo = new DateOnly(2026, 10, 7)
            },
            CancellationToken.None);

        var problem = action.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Errors.Should().ContainKey(nameof(AppointmentListQuery.DateTo));
    }

    private static TController SetUser<TController>(TController controller)
        where TController : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "clinician@poms.lk")],
                    "Test"))
            }
        };
        return controller;
    }

    private static Episode CreateEpisode(Patient patient, bool isRestricted, string createdBy) => new()
    {
        Patient = patient,
        PatientId = patient.Id,
        CenterId = patient.CenterId,
        RecordDate = new DateOnly(2026, 10, 7),
        IsRestricted = isRestricted,
        CreatedBy = createdBy
    };

    private static Appointment CreateAppointment(Patient patient, Episode episode) => new()
    {
        Patient = patient,
        PatientId = patient.Id,
        Episode = episode,
        EpisodeId = episode.Id,
        Type = AppointmentType.Assessment,
        AppointmentDate = new DateOnly(2026, 10, 8)
    };

    private static async Task<Patient> SeedPatientAsync(PomsDbContext context)
    {
        var province = new Province { Id = 100, Code = "T", Name = "Test Province" };
        var district = new District
        {
            Id = 100,
            ProvinceId = province.Id,
            Province = province,
            Code = "TD",
            Name = "Test District"
        };
        var center = new Center
        {
            Id = 100,
            DistrictId = district.Id,
            District = district,
            Code = "TC",
            Name = "Test Center"
        };
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            PatientNumber = $"TEST-{Guid.NewGuid():N}",
            FullName = "API Test Patient",
            NameWithInitials = "A. Patient",
            Dob = new DateOnly(1990, 1, 1),
            Sex = Sex.Other,
            Category = PatientCategory.Local,
            IdentificationType = IdentificationType.NotApplicable,
            IdentificationNumber = string.Empty,
            Address1 = "Test address",
            ProvinceId = province.Id,
            Province = province,
            DistrictId = district.Id,
            District = district,
            CenterId = center.Id,
            Center = center,
            RegistrationDate = new DateOnly(2026, 10, 7),
            RegistrationProcessedBy = "test",
            GuardianName = "N/A",
            GuardianRelationship = "N/A"
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        return patient;
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
