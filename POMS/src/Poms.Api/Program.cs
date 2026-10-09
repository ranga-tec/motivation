using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api;
using Poms.Reporting.Services;
using Poms.Api;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var useSqlite = builder.Configuration.GetValue<bool>("UseSQLite");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PomsDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
            throw new InvalidOperationException("DATABASE_URL is missing the expected username and password.");

        var ssl = builder.Environment.IsProduction()
            ? "SSL Mode=Require"
            : "SSL Mode=Prefer;Trust Server Certificate=true";
        var postgres =
            $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
            $"Username={Uri.UnescapeDataString(userInfo[0])};Password={Uri.UnescapeDataString(userInfo[1])};" +
            ssl;
        options.UseNpgsql(postgres);
    }
    else if (useSqlite && !string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        throw new InvalidOperationException(
            "Configure DATABASE_URL, or enable SQLite with ConnectionStrings:DefaultConnection for development.");
    }
});

var authentication = builder.Configuration.GetSection("ApiAuthentication");
var authority = authentication["Authority"];
var audience = authentication["Audience"];
var metadataAddress = authentication["MetadataAddress"];
if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException(
        "Poms.Api requires ApiAuthentication:Authority and ApiAuthentication:Audience.");
}

builder.Services
    .AddAuthentication(ApiAuthenticationDefaults.Scheme)
    .AddJwtBearer(ApiAuthenticationDefaults.Scheme, options =>
    {
        options.Authority = authority;
        if (!string.IsNullOrWhiteSpace(metadataAddress))
            options.MetadataAddress = metadataAddress;
        options.Audience = audience;
        options.RequireHttpsMetadata = authentication.GetValue("RequireHttpsMetadata", true);
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = authentication["Issuer"] ?? authority.TrimEnd('/'),
            NameClaimType = authentication["NameClaimType"] ?? "name",
            RoleClaimType = authentication["RoleClaimType"] ?? "poms_role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("ClinicianOrAdmin", policy => policy.RequireRole("CLINICIAN", "ADMIN"));
    options.AddPolicy("DataEntry", policy => policy.RequireRole("DATA_ENTRY", "ADMIN"));
    options.AddPolicy("ManagementOrAdmin", policy => policy.RequireRole("MANAGEMENT", "ADMIN"));
    options.AddPolicy("ReportOrAdmin", policy => policy.RequireRole("VIEWER", "MANAGEMENT", "ADMIN"));
    options.AddPolicy("AnyAuthenticatedUser", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(ClaimTypes.NameIdentifier));
    options.AddPolicy("ApiWrite", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim(ClaimTypes.NameIdentifier)
        .RequireAssertion(context =>
            context.Resource is HttpContext httpContext &&
            httpContext.Request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)));
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (builder.Environment.IsProduction() && allowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one production frontend origin.");

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddProblemDetails();
builder.Services.AddResponseCompression();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddIdentityCore<IdentityUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<PomsDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IClaimsTransformation, LocalIdentityClaimsTransformation>();
builder.Services.AddScoped<IRestrictedAccessService, RestrictedAccessService>();
builder.Services.AddScoped<IPatientNumberService, PatientNumberService>();
builder.Services.AddScoped<IDuplicateCheckService, DuplicateCheckService>();
builder.Services.AddScoped<IAppointmentAssigneeService, AppointmentAssigneeService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();
var fileStorage = builder.Configuration.GetSection("FileStorage");
var storageRoot = fileStorage["RootPath"];
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(storageRoot))
    throw new InvalidOperationException("FileStorage:RootPath must be a persistent production path.");
storageRoot ??= OperatingSystem.IsWindows() ? @"C:\PomsStorage\api" : "/app/storage";
var maxFileSizeMb = fileStorage.GetValue<long>("MaxFileSizeMB", 10);
var allowedExtensions = fileStorage.GetSection("AllowedExtensions").Get<string[]>();
builder.Services.AddScoped<IFileStorageService>(_ => new FileStorageService(storageRoot, maxFileSizeMb, allowedExtensions));
builder.Services.AddScoped<IPrintFormService, PrintFormService>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddCheck<PomsApiDatabaseHealthCheck>("database");

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<PomsDbContext>();
    var providerName = context.Database.ProviderName ?? string.Empty;
    if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        await context.Database.EnsureCreatedAsync();
        await SqliteSchemaUpgrader.ApplyAsync(context);
    }
    else if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
    {
        await context.Database.EnsureCreatedAsync();
        await PostgresSchemaUpgrader.ApplyAsync(context);
    }
    else
    {
        await context.Database.MigrateAsync();
    }

    await DbInitializer.SeedUsersAndRolesAsync(
        services,
        seedDemoUsers: builder.Configuration.GetValue("SeedDemoUsers", app.Environment.IsDevelopment()),
        bootstrapAdminEmail: builder.Configuration["BootstrapAdmin:Email"],
        bootstrapAdminPassword: builder.Configuration["BootstrapAdmin:Password"]);
    await SampleDataSeeder.SeedLocationsAsync(context);
    await SampleDataSeeder.SeedReferralSourcesAsync(context);
    await SampleDataSeeder.SeedMainProblemTypesAsync(context);
    await SampleDataSeeder.SeedCauseReasonTypesAsync(context);
    await SampleDataSeeder.SeedNationalitiesAsync(context);
    await SampleDataSeeder.SeedDeviceCatalogAsync(context);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

public partial class Program;
