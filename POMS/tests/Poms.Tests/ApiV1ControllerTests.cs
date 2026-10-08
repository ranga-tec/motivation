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
        var controller = CreatePatientsController(database.Context);

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
        var controller = CreatePatientsController(database.Context);

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
        var controller = SetUser(CreateAppointmentsController(database.Context, restrictedAccess.Object));

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
        var controller = SetUser(CreateAppointmentsController(database.Context, restrictedAccess.Object));

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

    [Fact]
    public async Task AppointmentsCreate_PersistsScheduledAppointment()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var controller = SetUser(CreateAppointmentsController(database.Context, Mock.Of<IRestrictedAccessService>()));

        var action = await controller.Create(new CreateAppointmentRequest
        {
            PatientId = patient.Id,
            Type = AppointmentType.Assessment,
            AppointmentDate = new DateOnly(2026, 10, 12),
            AppointmentTime = new TimeOnly(9, 30),
            AssignedClinicianEntry = "Test Prosthetist"
        }, CancellationToken.None);

        var response = action.Result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeOfType<AppointmentResponse>().Subject;
        response.Status.Should().Be("Scheduled");
        (await database.Context.Appointments.FindAsync(response.Id))!.CreatedBy.Should().Be("clinician@poms.lk");
    }

    [Fact]
    public async Task AppointmentsCreate_RejectsUnknownPatient()
    {
        await using var database = await CreateDatabaseAsync();
        var controller = SetUser(CreateAppointmentsController(database.Context, Mock.Of<IRestrictedAccessService>()));

        var action = await controller.Create(new CreateAppointmentRequest
        {
            PatientId = Guid.NewGuid(), AppointmentDate = new DateOnly(2026, 10, 12),
            AssignedClinicianEntry = "Test Prosthetist"
        }, CancellationToken.None);

        var problem = action.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey(nameof(CreateAppointmentRequest.PatientId));
    }

    [Fact]
    public async Task AppointmentsReschedule_PreservesPreviousSchedule()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var appointment = CreateAppointment(patient, null);
        database.Context.Appointments.Add(appointment);
        await database.Context.SaveChangesAsync();
        var controller = SetUser(CreateAppointmentsController(database.Context, Mock.Of<IRestrictedAccessService>()));

        var action = await controller.Reschedule(appointment.Id, new RescheduleAppointmentRequest
        {
            AppointmentDate = new DateOnly(2026, 10, 15), AppointmentTime = new TimeOnly(14, 0), Reason = "Patient requested a later date"
        }, CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>();
        appointment.PreviousAppointmentDate.Should().Be(new DateOnly(2026, 10, 8));
        appointment.AppointmentDate.Should().Be(new DateOnly(2026, 10, 15));
    }

    [Fact]
    public async Task AppointmentsCancel_RejectsCompletedAppointment()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var appointment = CreateAppointment(patient, null);
        appointment.Status = AppointmentStatus.Completed;
        database.Context.Appointments.Add(appointment);
        await database.Context.SaveChangesAsync();
        var controller = SetUser(CreateAppointmentsController(database.Context, Mock.Of<IRestrictedAccessService>()));

        var action = await controller.Cancel(appointment.Id, new CancelAppointmentRequest { Reason = "No longer required" }, CancellationToken.None);

        action.Result.Should().BeOfType<ConflictObjectResult>();
        appointment.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public async Task PatientsCreate_PersistsPatientAndReturnsCreatedContract()
    {
        await using var database = await CreateDatabaseAsync();
        var existing = await SeedPatientAsync(database.Context);
        var controller = SetUser(CreatePatientsController(database.Context));
        var request = ValidCreateRequest(existing) with
        {
            FullName = "New API Patient",
            NameWithInitials = "N. Patient",
            IdentificationType = IdentificationType.NIC,
            IdentificationNumber = "NEW-API-NIC"
        };

        var action = await controller.Create(request, CancellationToken.None);

        var created = action.Result.Should().BeOfType<CreatedAtActionResult>();
        var response = created.Which.Value.Should().BeOfType<PatientDetailResponse>().Subject;
        response.PatientNumber.Should().MatchRegex(@"^2026/\d{4}$");
        response.FullName.Should().Be("New API Patient");
        var stored = await database.Context.Patients.SingleAsync(item => item.Id == response.Id);
        stored.CreatedBy.Should().Be("clinician@poms.lk");
        stored.AssignedClinicianName.Should().Be("Test Prosthetist");
    }

    [Fact]
    public async Task PatientsCreate_RejectsExactIdentificationDuplicate()
    {
        await using var database = await CreateDatabaseAsync();
        var existing = await SeedPatientAsync(database.Context, IdentificationType.NIC, "EXISTING-NIC");
        var controller = SetUser(CreatePatientsController(database.Context));
        var request = ValidCreateRequest(existing) with
        {
            FullName = "Different Name",
            IdentificationType = IdentificationType.NIC,
            IdentificationNumber = "EXISTING-NIC"
        };

        var action = await controller.Create(request, CancellationToken.None);

        var conflict = action.Result.Should().BeOfType<ConflictObjectResult>();
        var problem = conflict.Which.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["duplicateType"].Should().Be("exact");
    }

    [Fact]
    public async Task PatientsCreate_RequiresConfirmationForPossibleDuplicate()
    {
        await using var database = await CreateDatabaseAsync();
        var existing = await SeedPatientAsync(database.Context);
        var controller = SetUser(CreatePatientsController(database.Context));
        var request = ValidCreateRequest(existing) with
        {
            IdentificationType = IdentificationType.NIC,
            IdentificationNumber = "UNIQUE-NIC"
        };

        var action = await controller.Create(request, CancellationToken.None);

        var conflict = action.Result.Should().BeOfType<ConflictObjectResult>();
        var problem = conflict.Which.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["duplicateType"].Should().Be("possible");
    }

    [Fact]
    public async Task PatientsCreate_AllowsReviewedPossibleDuplicate()
    {
        await using var database = await CreateDatabaseAsync();
        var existing = await SeedPatientAsync(database.Context);
        var controller = SetUser(CreatePatientsController(database.Context));
        var request = ValidCreateRequest(existing) with
        {
            IdentificationType = IdentificationType.NIC,
            IdentificationNumber = "REVIEWED-UNIQUE-NIC",
            ConfirmPossibleDuplicate = true
        };

        var action = await controller.Create(request, CancellationToken.None);

        action.Result.Should().BeOfType<CreatedAtActionResult>();
        (await database.Context.Patients.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task PatientsCreate_RejectsDistrictFromAnotherProvince()
    {
        await using var database = await CreateDatabaseAsync();
        var existing = await SeedPatientAsync(database.Context);
        var controller = SetUser(CreatePatientsController(database.Context));
        var request = ValidCreateRequest(existing) with { ProvinceId = existing.ProvinceId + 999 };

        var action = await controller.Create(request, CancellationToken.None);

        var badRequest = action.Result.Should().BeOfType<BadRequestObjectResult>();
        var problem = badRequest.Which.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey(nameof(CreatePatientRequest.DistrictId));
    }

    [Fact]
    public async Task EpisodesCreate_PersistsPatientRecord()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var controller = SetUser(new EpisodesApiController(database.Context, AllowedRestrictedAccess()));

        var action = await controller.Create(new SaveEpisodeRequest
        {
            PatientId = patient.Id, CenterId = patient.CenterId, Status = RecordStatus.Active,
            RecordDate = new DateOnly(2026, 10, 8), RecordTime = new TimeOnly(9, 15), Remarks = "API record"
        }, CancellationToken.None);

        var response = action.Result.Should().BeOfType<CreatedAtActionResult>()
            .Which.Value.Should().BeOfType<EpisodeResponse>().Subject;
        response.PatientId.Should().Be(patient.Id);
        response.CenterName.Should().Be("Test Center");
        (await database.Context.Episodes.SingleAsync()).CreatedBy.Should().Be("clinician@poms.lk");
    }

    [Fact]
    public async Task EpisodesList_HidesRestrictedRecordsOutsideScope()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        database.Context.Episodes.AddRange(CreateEpisode(patient, false, "other@poms.lk"), CreateEpisode(patient, true, "other@poms.lk"));
        await database.Context.SaveChangesAsync();
        var controller = SetUser(new EpisodesApiController(database.Context, AllowedRestrictedAccess()));

        var action = await controller.List(patient.Id, CancellationToken.None);

        action.Result.Should().BeOfType<OkObjectResult>().Which.Value
            .Should().BeAssignableTo<IReadOnlyList<EpisodeResponse>>().Which.Should().ContainSingle();
    }

    [Fact]
    public async Task EpisodesUpdate_RejectsChangingPatient()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var episode = CreateEpisode(patient, false, "clinician@poms.lk");
        database.Context.Episodes.Add(episode); await database.Context.SaveChangesAsync();
        var controller = SetUser(new EpisodesApiController(database.Context, AllowedRestrictedAccess()));

        var action = await controller.Update(episode.Id, new SaveEpisodeRequest
        {
            PatientId = Guid.NewGuid(), CenterId = patient.CenterId, RecordDate = episode.RecordDate,
            RecordTime = new TimeOnly(10, 0)
        }, CancellationToken.None);

        var problem = action.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Errors.Should().ContainKey(nameof(SaveEpisodeRequest.PatientId));
    }

    private static PatientsApiController CreatePatientsController(PomsDbContext context) => new(
        context,
        new PatientNumberService(context),
        new DuplicateCheckService(context),
        new AppointmentAssigneeService(context));

    private static AppointmentsApiController CreateAppointmentsController(
        PomsDbContext context,
        IRestrictedAccessService restrictedAccess) => new(
            context,
            restrictedAccess,
            new AppointmentAssigneeService(context));

    private static IRestrictedAccessService AllowedRestrictedAccess()
    {
        var service = new Mock<IRestrictedAccessService>();
        service.Setup(item => item.GetScopeAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new RestrictedAccessScope("user-id", "clinician@poms.lk", false));
        service.Setup(item => item.AuditAsync(It.IsAny<RestrictedAccessScope>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);
        return service.Object;
    }

    private static CreatePatientRequest ValidCreateRequest(Patient reference) => new()
    {
        FullName = reference.FullName,
        NameWithInitials = reference.NameWithInitials,
        DateOfBirth = reference.Dob,
        Sex = reference.Sex,
        Category = reference.Category,
        IdentificationType = IdentificationType.NotApplicable,
        Address1 = "New API address",
        ProvinceId = reference.ProvinceId,
        DistrictId = reference.DistrictId,
        CityOther = "Test City",
        CenterId = reference.CenterId,
        RegistrationDate = new DateOnly(2026, 10, 7),
        AssignedClinicianEntry = "Test Prosthetist",
        GuardianName = "Test Guardian",
        GuardianRelationship = "Parent",
        Contacts = [new CreatePatientContactRequest("0771234567", null, null)]
    };

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

    private static Appointment CreateAppointment(Patient patient, Episode? episode) => new()
    {
        Patient = patient,
        PatientId = patient.Id,
        Episode = episode,
        EpisodeId = episode?.Id,
        Type = AppointmentType.Assessment,
        AppointmentDate = new DateOnly(2026, 10, 8)
    };

    private static async Task<Patient> SeedPatientAsync(
        PomsDbContext context,
        IdentificationType identificationType = IdentificationType.NotApplicable,
        string identificationNumber = "")
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
            IdentificationType = identificationType,
            IdentificationNumber = identificationNumber,
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
