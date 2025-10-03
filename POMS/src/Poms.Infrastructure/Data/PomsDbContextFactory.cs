using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Poms.Infrastructure.Data;

public class PomsDbContextFactory : IDesignTimeDbContextFactory<PomsDbContext>
{
    public PomsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PomsDbContext>();

        // Check for DATABASE_URL environment variable (Railway format)
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

        if (!string.IsNullOrEmpty(databaseUrl))
        {
            // Parse PostgreSQL URL format: postgresql://user:password@host:port/database
            var uri = new Uri(databaseUrl);
            var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true";

            optionsBuilder.UseNpgsql(connectionString);
        }
        else
        {
            // Fallback to SQL Server for local development
            optionsBuilder.UseSqlServer("Server=localhost;Database=PomsDb;Trusted_Connection=true;TrustServerCertificate=true;");
        }

        return new PomsDbContext(optionsBuilder.Options);
    }
}
