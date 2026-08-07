using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Poms.Infrastructure.Data;

public static class SqliteSchemaUpgrader
{
    private static readonly (string Name, string Definition)[] AppointmentColumns =
    [
        ("PreviousAppointmentDate", "TEXT NULL"),
        ("PreviousAppointmentTime", "TEXT NULL"),
        ("RescheduleReason", "TEXT NULL"),
        ("RescheduledAt", "TEXT NULL")
    ];

    private static readonly (string Name, string Definition)[] PatientColumns =
    [
        ("ReferralPersonName", "TEXT NULL"),
        ("ReferralPersonContactNumber", "TEXT NULL"),
        ("AssignedClinicianUserId", "TEXT NULL"),
        ("AssignedClinicianName", "TEXT NULL")
    ];

    private static readonly (string Name, string Definition)[] NumberSeriesColumns =
    [
        ("CenterId", "INTEGER NULL")
    ];

    public static async Task ApplyAsync(PomsDbContext context)
    {
        if (!(context.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ?? false))
            return;

        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync();

        try
        {
            await EnsureColumnsAsync(connection, "Appointments", AppointmentColumns);
            await EnsureColumnsAsync(connection, "Patients", PatientColumns);
            await EnsureColumnsAsync(connection, "NumberSeries", NumberSeriesColumns);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static async Task EnsureColumnsAsync(
        System.Data.Common.DbConnection connection,
        string table,
        IReadOnlyList<(string Name, string Definition)> columns)
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info(\"{table}\");";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                existingColumns.Add(reader.GetString(1));
        }

        foreach (var (name, definition) in columns)
        {
            if (existingColumns.Contains(name))
                continue;

            await using var command = connection.CreateCommand();
            command.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{name}\" {definition};";
            await command.ExecuteNonQueryAsync();
        }
    }
}
