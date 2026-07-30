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

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }
}
