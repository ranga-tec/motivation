using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/poms-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Determine which database provider to use
var usePostgreSQL = builder.Configuration.GetValue<bool>("UsePostgreSQL", false);

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

// Identity Configuration
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
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
var rootPath = fileStorageConfig["RootPath"] ?? "C:\\PomsStorage";
var maxFileSizeMB = fileStorageConfig.GetValue<long>("MaxFileSizeMB", 10);
var allowedExtensions = fileStorageConfig.GetSection("AllowedExtensions").Get<string[]>();

builder.Services.AddScoped<IPatientNumberService, PatientNumberService>();
builder.Services.AddScoped<IFileStorageService>(sp => 
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
        await context.Database.MigrateAsync();
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
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
