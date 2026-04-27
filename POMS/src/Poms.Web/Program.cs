using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for Railway reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Configure Railway PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
Console.WriteLine($"[POMS] Starting on port {port}");
Console.WriteLine($"[POMS] Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
Console.WriteLine($"[POMS] Railway: {Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") ?? "Not on Railway"}");

builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/poms-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Check for Railway DATABASE_URL environment variable first
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;
bool usePostgreSQL;

if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("[POMS] DATABASE_URL found - using PostgreSQL");
    // Parse PostgreSQL URL format: postgresql://user:password@host:port/database
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');
        var host = uri.Host;
        var dbPort = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var username = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : "";

        connectionString = $"Host={host};Port={dbPort};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        Console.WriteLine($"[POMS] Database Host: {host}, Port: {dbPort}, Database: {database}");
        usePostgreSQL = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[POMS] Error parsing DATABASE_URL: {ex.Message}");
        throw;
    }
}
else
{
    Console.WriteLine("[POMS] No DATABASE_URL - using DefaultConnection from appsettings");
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    usePostgreSQL = builder.Configuration.GetValue<bool>("UsePostgreSQL", false);
}

builder.Services.AddDbContext<PomsDbContext>(options =>
{
    if (usePostgreSQL)
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity Configuration with ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<PomsDbContext>();

// Configure application services
var fileStorageConfig = builder.Configuration.GetSection("FileStorage");
// Use /app/storage for Railway (Linux), or Windows path for local dev
var defaultStoragePath = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT"))
    ? "/app/storage"
    : "C:\\PomsStorage";
var rootPath = fileStorageConfig["RootPath"] ?? defaultStoragePath;
var maxFileSizeMB = fileStorageConfig.GetValue<long>("MaxFileSizeMB", 10);
var allowedExtensions = fileStorageConfig.GetSection("AllowedExtensions").Get<string[]>();

Console.WriteLine($"[POMS] File storage path: {rootPath}");

builder.Services.AddScoped<IPatientNumberService, PatientNumberService>();
builder.Services.AddScoped<IFileStorageService>(sp =>
    new FileStorageService(rootPath, maxFileSizeMB, allowedExtensions));

// Register new services
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IOcrService, OcrService>();

// Add AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("ClinicianOrAdmin", policy => policy.RequireRole("CLINICIAN", "ADMIN"));
    options.AddPolicy("DataEntry", policy => policy.RequireRole("DATA_ENTRY", "ADMIN"));
    options.AddPolicy("AnyAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PomsDbContext>();

        // Use EnsureCreated for PostgreSQL (Railway) since migrations are SQL Server-specific.
        // Do not drop the database on startup; production data must survive redeploys and restarts.
        if (usePostgreSQL)
        {
            Log.Information("PostgreSQL detected - ensuring database schema exists");
            await context.Database.EnsureCreatedAsync();
            Log.Information("Database schema created successfully");
        }
        else
        {
            await context.Database.MigrateAsync();
        }

        await DbInitializer.SeedUsersAndRolesAsync(services);
        await SampleDataSeeder.SeedSampleConditionsAsync(context);
        Log.Information("Database seeded successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred seeding the database");
        // Don't crash the app - allow it to start even if database setup fails
        // This helps with debugging on Railway
    }
}

// Configure the HTTP request pipeline.
// Handle forwarded headers from Railway reverse proxy (must be first)
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

Log.Information("POMS Application Starting...");
app.Run();
