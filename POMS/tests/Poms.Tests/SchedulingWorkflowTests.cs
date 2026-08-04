using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Web.ViewModels;

namespace Poms.Tests;

public class SchedulingWorkflowTests
{
    [Fact]
    public void AppointmentTypes_ContainAllSupportedWorkflows()
    {
        Enum.GetValues<AppointmentType>().Should().BeEquivalentTo(new[]
        {
            AppointmentType.Assessment,
            AppointmentType.Fitting,
            AppointmentType.Delivery,
            AppointmentType.FollowUp,
            AppointmentType.GaitTraining
        });
    }

    [Fact]
    public void NewPatientRecord_DefaultsToARecordTime()
    {
        new EpisodeViewModel().RecordTime.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_RetainsAppointmentAndCapturesAuditReason()
    {
        var appointment = new Appointment
        {
            Status = AppointmentStatus.Scheduled,
            IsDeleted = false
        };
        var cancelledAt = new DateTime(2026, 7, 30, 10, 15, 0, DateTimeKind.Utc);

        appointment.Cancel("  Patient unavailable  ", "admin@poms.lk", cancelledAt);

        appointment.Status.Should().Be(AppointmentStatus.Cancelled);
        appointment.CancellationReason.Should().Be("Patient unavailable");
        appointment.CancelledAt.Should().Be(cancelledAt);
        appointment.UpdatedBy.Should().Be("admin@poms.lk");
        appointment.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Cancel_WithoutReason_IsRejected()
    {
        var appointment = new Appointment();

        var act = () => appointment.Cancel("  ", "admin@poms.lk");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cancellation reason is required*");
    }

    [Fact]
    public void Reschedule_ChangesScheduleAndCapturesPreviousScheduleAndReason()
    {
        var originalDate = new DateOnly(2026, 8, 4);
        var originalTime = new TimeOnly(9, 30);
        var appointment = new Appointment
        {
            AppointmentDate = originalDate,
            AppointmentTime = originalTime,
            Status = AppointmentStatus.Scheduled
        };
        var rescheduledAt = new DateTime(2026, 8, 4, 5, 0, 0, DateTimeKind.Utc);

        appointment.Reschedule(
            new DateOnly(2026, 8, 8),
            new TimeOnly(11, 0),
            "  Clinician unavailable  ",
            "admin@poms.lk",
            rescheduledAt);

        appointment.AppointmentDate.Should().Be(new DateOnly(2026, 8, 8));
        appointment.AppointmentTime.Should().Be(new TimeOnly(11, 0));
        appointment.PreviousAppointmentDate.Should().Be(originalDate);
        appointment.PreviousAppointmentTime.Should().Be(originalTime);
        appointment.RescheduleReason.Should().Be("Clinician unavailable");
        appointment.RescheduledAt.Should().Be(rescheduledAt);
        appointment.UpdatedBy.Should().Be("admin@poms.lk");
        appointment.Status.Should().Be(AppointmentStatus.Scheduled);
    }

    [Fact]
    public void Reschedule_WithoutAChangedSchedule_IsRejected()
    {
        var appointment = new Appointment
        {
            AppointmentDate = new DateOnly(2026, 8, 4),
            AppointmentTime = new TimeOnly(9, 30),
            Status = AppointmentStatus.Scheduled
        };

        var act = () => appointment.Reschedule(
            appointment.AppointmentDate,
            appointment.AppointmentTime,
            "Reason",
            "admin@poms.lk");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*new appointment date or time*");
    }

    [Fact]
    public void PatientDob_MustBeAtLeastThreeDaysBeforeToday()
    {
        var model = new PatientViewModel
        {
            Dob = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
            FullName = "Test Patient",
            NameWithInitials = "T Patient",
            IdentificationNumber = "TEST-001",
            Address1 = "Test address",
            CityOther = "Test city",
            RegistrationProcessedBy = "tester",
            GuardianName = "Test Guardian",
            GuardianRelationship = "Parent"
        };

        Validate(model).Should().Contain(result =>
            result.MemberNames.Contains(nameof(PatientViewModel.Dob)));
    }

    [Theory]
    [InlineData(10, 0, 9, 0)]
    [InlineData(9, 0, 9, 0)]
    public void Assessment_EndTimeMustBeLaterThanStartTime(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute)
    {
        var model = new AssessmentViewModel
        {
            StartTime = new TimeOnly(startHour, startMinute),
            EndTime = new TimeOnly(endHour, endMinute)
        };

        Validate(model).Should().Contain(result =>
            result.MemberNames.Contains(nameof(AssessmentViewModel.EndTime)));
    }

    [Theory]
    [InlineData(10, 0, 9, 0)]
    [InlineData(9, 0, 9, 0)]
    public void FollowUp_EndTimeMustBeLaterThanStartTime(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute)
    {
        var model = new FollowUpViewModel
        {
            StartTime = new TimeOnly(startHour, startMinute),
            EndTime = new TimeOnly(endHour, endMinute)
        };

        Validate(model).Should().Contain(result =>
            result.MemberNames.Contains(nameof(FollowUpViewModel.EndTime)));
    }

    [Fact]
    public async Task LocationSeeder_AddsCompleteOfficialPostalLocalitySetIdempotently()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PomsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new PomsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await SampleDataSeeder.SeedLocationsAsync(context);
        await SampleDataSeeder.SeedLocationsAsync(context);

        (await context.Provinces.CountAsync()).Should().Be(9);
        (await context.Districts.CountAsync()).Should().Be(25);
        (await context.Cities.CountAsync()).Should().Be(2111);
        (await context.Cities.AnyAsync(city => city.Name == "Achchuvely")).Should().BeTrue();
        (await context.Cities.AnyAsync(city => city.Name == "Yudaganawa")).Should().BeTrue();
    }

    [Fact]
    public async Task SqliteSchemaUpgrader_IsIdempotentForAppointmentReschedulingColumns()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<PomsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new PomsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await SqliteSchemaUpgrader.ApplyAsync(context);
        await SqliteSchemaUpgrader.ApplyAsync(context);

        var columns = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(\"Appointments\");";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            columns.Add(reader.GetString(1));

        columns.Should().Contain(new[]
        {
            "PreviousAppointmentDate",
            "PreviousAppointmentTime",
            "RescheduleReason",
            "RescheduledAt"
        });
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }
}
