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
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(\"Appointments\");";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    existingColumns.Add(reader.GetString(1));
            }

            foreach (var (name, definition) in AppointmentColumns)
            {
                if (existingColumns.Contains(name))
                    continue;

                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER TABLE \"Appointments\" ADD COLUMN \"{name}\" {definition};";
                await command.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
