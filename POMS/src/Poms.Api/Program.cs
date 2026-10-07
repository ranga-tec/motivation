using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api;

var builder = WebApplication.CreateBuilder(args);

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

        var postgres =
            $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};" +
            $"Username={Uri.UnescapeDataString(userInfo[0])};Password={Uri.UnescapeDataString(userInfo[1])};" +
            "SSL Mode=Prefer;Trust Server Certificate=true";
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
        options.Audience = audience;
        options.RequireHttpsMetadata = authentication.GetValue("RequireHttpsMetadata", true);
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = authentication["NameClaimType"] ?? "name",
            RoleClaimType = authentication["RoleClaimType"] ?? "role"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("ClinicianOrAdmin", policy => policy.RequireRole("CLINICIAN", "ADMIN"));
    options.AddPolicy("DataEntry", policy => policy.RequireRole("DATA_ENTRY", "ADMIN"));
    options.AddPolicy("ManagementOrAdmin", policy => policy.RequireRole("MANAGEMENT", "ADMIN"));
    options.AddPolicy("ReportOrAdmin", policy => policy.RequireRole("VIEWER", "MANAGEMENT", "ADMIN"));
    options.AddPolicy("AnyAuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (builder.Environment.IsProduction() && allowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one production frontend origin.");

builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
}));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRestrictedAccessService, RestrictedAccessService>();
builder.Services.AddControllers();
builder.Services.AddHealthChecks().AddCheck<PomsApiDatabaseHealthCheck>("database");

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

public partial class Program;
