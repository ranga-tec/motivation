using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Poms.Infrastructure.Data;
using Poms.Web.Services;

namespace Poms.Tests;

public sealed class PomsDatabaseHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenDatabaseIsReachable()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var result = await new PomsDatabaseHealthCheck(context)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenDatabaseIsUnavailable()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new DbContextOptionsBuilder<PomsDbContext>()
            .UseSqlite($"Data Source={Path.Combine(missingDirectory, "poms.db")};Mode=ReadWrite")
            .Options;
        await using var context = new PomsDbContext(options);

        var result = await new PomsDatabaseHealthCheck(context)
            .CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    private static PomsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<PomsDbContext>()
            .UseSqlite(connection)
            .Options;
        return new PomsDbContext(options);
    }
}
