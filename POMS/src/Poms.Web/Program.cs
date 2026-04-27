using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Respect ASPNETCORE_URLS when it is provided, otherwise fall back to Railway/container PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port) && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/poms-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
// Check for Railway/container DATABASE_URL environment variable first.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;
bool usePostgreSQL;

if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    // Parse PostgreSQL URL format: postgresql://user:password@host:port/database
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    if (userInfo.Length != 2)
    {
        throw new InvalidOperationException("DATABASE_URL is missing the expected username and password.");
    }

    connectionString =
        $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
        $"Username={Uri.UnescapeDataString(userInfo[0])};Password={Uri.UnescapeDataString(userInfo[1])};" +
        "SSL Mode=Prefer;Trust Server Certificate=true";
    usePostgreSQL = true;
}
else
{
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
builder.Services.AddHealthChecks();
builder.Services.Configure<VersionSwitchOptions>(builder.Configuration.GetSection("VersionSwitch"));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;

    // The reverse proxy address is environment-specific, so trust the proxy network
    // that fronts the container instead of hard-coding host IPs here.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Identity Configuration
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
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
var defaultStorageRoot = OperatingSystem.IsWindows() ? @"C:\PomsStorage" : "/app/storage";
var rootPath = fileStorageConfig["RootPath"] ?? defaultStorageRoot;
var maxFileSizeMB = fileStorageConfig.GetValue<long>("MaxFileSizeMB", 10);
var allowedExtensions = fileStorageConfig.GetSection("AllowedExtensions").Get<string[]>();
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? (OperatingSystem.IsWindows() ? Path.Combine(rootPath, "data-protection-keys") : "/app/data-protection-keys");

Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddScoped<IPatientNumberService, PatientNumberService>();
builder.Services.AddScoped<IFileStorageService>(_ =>
    new FileStorageService(rootPath, maxFileSizeMB, allowedExtensions));

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
        var providerName = context.Database.ProviderName ?? string.Empty;

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            // PostgreSQL deployments on Contabo start from a fresh database, so build the schema
            // directly from the current model instead of applying the existing SQL Server migration.
            await context.Database.EnsureCreatedAsync();
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
    }
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

var enforceHttps = builder.Configuration.GetValue<bool?>("Security:ForceHttps")
    ?? (!string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_HTTPS_PORT"])
        || !string.IsNullOrWhiteSpace(builder.Configuration["HTTPS_PORT"]));
if (enforceHttps)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

Log.Information("POMS Application Starting...");
app.Run();
