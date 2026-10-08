using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Reporting.Services;
using Poms.Web.Api.V1.Contracts;
using Poms.Web.Api.V1.Controllers;

namespace Poms.Tests;

public sealed class ApiV1ControllerTests
{
    static ApiV1ControllerTests() =>
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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

    [Fact]
    public async Task ClinicalAssessmentCreate_PersistsPrescriptionLabel()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var episode = CreateEpisode(patient, false, "clinician@poms.lk");
        var problemType = new MainProblemType { Id = 501, Name = "Mobility" };
        var causeType = new CauseReasonType { Id = 501, Name = "Trauma" };
        database.Context.AddRange(episode, problemType, causeType); await database.Context.SaveChangesAsync();
        var controller = SetUser(new ClinicalRecordsApiController(database.Context, AllowedRestrictedAccess()));

        var action = await controller.CreateAssessment(new SaveAssessmentRequest
        {
            EpisodeId = episode.Id, AssessmentType = AssessmentType.Prosthetic, LimbCategory = LimbCategory.LowerLimb,
            AssessedOn = new DateOnly(2026, 10, 8), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0),
            MainProblemTypeId = problemType.Id, Side = Side.Left, CauseReasonTypeId = causeType.Id,
            Prescriptions = [new PrescriptionRequest(Side.Left, "TRANS_TIBIAL_PROSTHESIS", null, null)]
        }, CancellationToken.None);

        var response = action.Result.Should().BeOfType<CreatedResult>().Which.Value.Should().BeOfType<AssessmentResponse>().Subject;
        response.Prescriptions.Should().ContainSingle().Which.Label.Should().Be("Trans-Tibial Prosthesis");
        (await database.Context.Prescriptions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ClinicalAssessmentCreate_RejectsPrescriptionFromWrongCatalog()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context); var episode = CreateEpisode(patient, false, "clinician@poms.lk");
        database.Context.AddRange(episode, new MainProblemType { Id = 502, Name = "Mobility" }, new CauseReasonType { Id = 502, Name = "Trauma" }); await database.Context.SaveChangesAsync();
        var controller = SetUser(new ClinicalRecordsApiController(database.Context, AllowedRestrictedAccess()));

        var action = await controller.CreateAssessment(new SaveAssessmentRequest { EpisodeId = episode.Id, AssessmentType = AssessmentType.Prosthetic, LimbCategory = LimbCategory.LowerLimb, AssessedOn = new DateOnly(2026, 10, 8), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), MainProblemTypeId = 502, Side = Side.Left, CauseReasonTypeId = 502, Prescriptions = [new PrescriptionRequest(Side.Left, "AFO", null, null)] }, CancellationToken.None);

        action.Result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeOfType<ValidationProblemDetails>().Which.Errors.Should().ContainKey(nameof(SaveAssessmentRequest.Prescriptions));
    }

    [Fact]
    public async Task ClinicalRecordsCreate_AllSimpleRecordTypes()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context); var episode = CreateEpisode(patient, false, "clinician@poms.lk"); database.Context.Add(episode); await database.Context.SaveChangesAsync();
        var controller = SetUser(new ClinicalRecordsApiController(database.Context, AllowedRestrictedAccess()));

        (await controller.CreateFitting(new SaveFittingRequest(episode.Id, new DateOnly(2026, 10, 8), "Fit", false), CancellationToken.None)).Result.Should().BeOfType<CreatedResult>();
        (await controller.CreateDelivery(new SaveDeliveryRequest(episode.Id, new DateOnly(2026, 10, 8), new TimeOnly(11, 0), "Delivered", null, false), CancellationToken.None)).Result.Should().BeOfType<CreatedResult>();
        (await controller.CreateFollowUp(new SaveFollowUpRequest { EpisodeId = episode.Id, FollowUpDate = new DateOnly(2026, 10, 9), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(9, 30), Notes = "Review" }, CancellationToken.None)).Result.Should().BeOfType<CreatedResult>();
        (await database.Context.Fittings.CountAsync()).Should().Be(1); (await database.Context.Deliveries.CountAsync()).Should().Be(1); (await database.Context.FollowUps.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DocumentsUploadDownloadDelete_CompletesPatientDocumentLifecycle()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var storage = new MemoryFileStorage();
        var controller = SetUser(CreateDocumentsController(database.Context, storage, AllowedRestrictedAccess()));
        var bytes = "test document"u8.ToArray();
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "assessment.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var uploaded = await controller.Upload(new UploadDocumentRequest
        {
            PatientId = patient.Id,
            DocumentType = DocumentType.MedicalDocuments,
            Notes = "Clinical attachment",
            File = file
        }, CancellationToken.None);

        var created = uploaded.Result.Should().BeOfType<CreatedResult>().Which.Value
            .Should().BeOfType<DocumentResponse>().Subject;
        created.FileName.Should().Be("assessment.pdf");

        var downloaded = await controller.Download(created.Id, "patient", CancellationToken.None);
        downloaded.Should().BeOfType<FileContentResult>().Which.FileContents.Should().Equal(bytes);

        (await controller.Delete(created.Id, "patient", CancellationToken.None))
            .Should().BeOfType<NoContentResult>();
        (await database.Context.PatientDocuments.CountAsync()).Should().Be(0);
        (await database.Context.PatientDocuments.IgnoreQueryFilters().SingleAsync()).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task PatientPhoto_ReturnsLatestAuthorizedPhotoWithoutCaching()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var storage = new MemoryFileStorage();
        var saved = storage.Add("photo.jpg", [1, 2, 3]);
        database.Context.PatientDocuments.Add(new PatientDocument
        {
            PatientId = patient.Id,
            DocumentType = DocumentType.PatientPhoto,
            FileName = "photo.jpg",
            StoragePath = saved,
            ContentType = "image/jpeg",
            UploadedBy = "clinician@poms.lk",
            UploadedAt = DateTime.UtcNow,
            CreatedBy = "clinician@poms.lk"
        });
        await database.Context.SaveChangesAsync();
        var controller = SetUser(CreateDocumentsController(database.Context, storage, AllowedRestrictedAccess()));

        var result = await controller.PatientPhoto(patient.Id, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>().Which.FileContents.Should().Equal(1, 2, 3);
        controller.Response.Headers.CacheControl.ToString().Should().Be("no-store, private");
    }

    [Fact]
    public async Task PrintRegistration_ReturnsPdfDocument()
    {
        await using var database = await CreateDatabaseAsync();
        var patient = await SeedPatientAsync(database.Context);
        var controller = SetUser(new PrintApiController(database.Context, new PrintFormService(), AllowedRestrictedAccess()));

        var result = await controller.Registration(patient.Id, CancellationToken.None);

        var pdf = result.Should().BeOfType<FileContentResult>().Subject;
        pdf.ContentType.Should().Be("application/pdf");
        pdf.FileContents.Take(4).Should().Equal("%PDF"u8.ToArray());
        pdf.FileDownloadName.Should().Contain(patient.PatientNumber);
    }

    [Fact]
    public async Task AdminCatalog_ReturnsLocationsLookupsAndDevices()
    {
        await using var database = await CreateDatabaseAsync();
        await SeedPatientAsync(database.Context);
        database.Context.AddRange(
            new ReferralSource { Name = "Hospital", IsActive = true },
            new MainProblemType { Name = "Mobility", IsActive = true },
            new CauseReasonType { Name = "Trauma", IsActive = true });
        await database.Context.SaveChangesAsync();
        var controller = CreateAdminController(database.Context);

        var result = await controller.Catalog(CancellationToken.None);

        result.Provinces.Should().ContainSingle();
        result.Centers.Should().ContainSingle();
        result.Lookups["referral-sources"].Should().ContainSingle();
        result.Lookups["main-problem-types"].Should().ContainSingle();
    }

    [Fact]
    public async Task AdminLookupCreateAndUpdate_PersistsValidatedReferenceData()
    {
        await using var database = await CreateDatabaseAsync();
        var controller = SetUser(CreateAdminController(database.Context));

        var created = await controller.CreateLookup("nationalities", new SaveNamedItemRequest { Name = "Sri Lankan" }, CancellationToken.None);
        var item = created.Result.Should().BeOfType<CreatedResult>().Which.Value.Should().BeOfType<AdminItem>().Subject;
        var updated = await controller.UpdateLookup("nationalities", item.Id, new SaveNamedItemRequest { Name = "Sri Lankan citizen", IsActive = false }, CancellationToken.None);

        updated.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<AdminItem>().Which.IsActive.Should().BeFalse();
        (await database.Context.Nationalities.SingleAsync()).Name.Should().Be("Sri Lankan citizen");
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

    private static DocumentsApiController CreateDocumentsController(
        PomsDbContext context,
        IFileStorageService storage,
        IRestrictedAccessService restrictedAccess) => new(
            context,
            storage,
            restrictedAccess,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:MaxFileSizeMB"] = "10",
                ["FileStorage:AllowedExtensions:0"] = ".pdf",
                ["FileStorage:AllowedExtensions:1"] = ".jpg"
            }).Build());

    private static AdminApiController CreateAdminController(PomsDbContext context) => new(
        context,
        new Mock<UserManager<IdentityUser>>(
            Mock.Of<IUserStore<IdentityUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!).Object,
        new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(),
            null!, null!, null!, null!).Object);

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

    private sealed class MemoryFileStorage : IFileStorageService
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.OrdinalIgnoreCase);

        public string Add(string path, byte[] bytes)
        {
            _files[path] = bytes;
            return path;
        }

        public async Task<(string StoragePath, string FileName)> SaveFileAsync(IFormFile file, string patientNumber)
        {
            var path = $"patients/{patientNumber}/{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            _files[path] = stream.ToArray();
            return (path, Path.GetFileName(file.FileName));
        }

        public Task<byte[]> GetFileAsync(string storagePath) =>
            Task.FromResult(_files.TryGetValue(storagePath, out var bytes)
                ? bytes
                : throw new FileNotFoundException("File not found", storagePath));

        public Task DeleteFileAsync(string storagePath) { _files.Remove(storagePath); return Task.CompletedTask; }
        public Task<StagedOcrImport> StageOcrImportAsync(IFormFile file, string contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StoredOcrImport> MaterializeOcrImportAsync(string token, string patientNumber, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteStagedOcrImportAsync(string token) => Task.CompletedTask;
        public Task CleanupExpiredOcrImportsAsync() => Task.CompletedTask;
    }
}
