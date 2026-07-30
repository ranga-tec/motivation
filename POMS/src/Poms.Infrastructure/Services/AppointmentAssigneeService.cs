using Microsoft.EntityFrameworkCore;
using Poms.Infrastructure.Data;

namespace Poms.Infrastructure.Services;

public sealed record AppointmentAssigneeOption(
    string UserId,
    string FullName,
    string Designation,
    string EmployeeNumber,
    bool IsPreferred)
{
    public string DisplayText =>
        $"{FullName} - {Designation} ({EmployeeNumber})";
}

public sealed record AppointmentAssigneeResolution(
    bool IsValid,
    string? UserId,
    string? FullName,
    string? Error)
{
    public static AppointmentAssigneeResolution Valid(
        string? userId,
        string fullName) =>
        new(true, userId, fullName, null);

    public static AppointmentAssigneeResolution Invalid(string error) =>
        new(false, null, null, error);
}

public interface IAppointmentAssigneeService
{
    Task<IReadOnlyList<AppointmentAssigneeOption>> GetOptionsAsync();
    Task<AppointmentAssigneeResolution> ResolveAsync(
        string? entry,
        string? selectedUserId);
}

public sealed class AppointmentAssigneeService : IAppointmentAssigneeService
{
    private const string ClinicianRole = "CLINICIAN";
    private readonly PomsDbContext _context;

    public AppointmentAssigneeService(PomsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppointmentAssigneeOption>> GetOptionsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var staffProfiles = await (
            from profile in _context.EmployeeProfiles.AsNoTracking()
            join user in _context.Users.AsNoTracking()
                on profile.UserId equals user.Id
            select new
            {
                profile.UserId,
                profile.FullName,
                profile.Designation,
                profile.EmployeeNumber,
                user.LockoutEnd
            })
            .ToListAsync();
        var activeProfiles = staffProfiles
            .Where(profile =>
                !profile.LockoutEnd.HasValue ||
                profile.LockoutEnd <= now)
            .ToList();

        var clinicianUserIds = (await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where role.NormalizedName == ClinicianRole
            select userRole.UserId)
            .ToListAsync())
            .ToHashSet();

        return activeProfiles
            .Select(profile => new AppointmentAssigneeOption(
                profile.UserId,
                profile.FullName,
                profile.Designation,
                profile.EmployeeNumber,
                clinicianUserIds.Contains(profile.UserId) ||
                    IsProsthetistOrOrthotist(profile.Designation)))
            .OrderByDescending(option => option.IsPreferred)
            .ThenBy(option => option.FullName)
            .ToList();
    }

    public async Task<AppointmentAssigneeResolution> ResolveAsync(
        string? entry,
        string? selectedUserId)
    {
        var trimmedEntry = entry?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedEntry))
        {
            return AppointmentAssigneeResolution.Invalid(
                "Select a staff member or type a custom Prosthetist / Orthotist name.");
        }

        var options = await GetOptionsAsync();
        if (!string.IsNullOrWhiteSpace(selectedUserId))
        {
            var selected = options.FirstOrDefault(option =>
                option.UserId == selectedUserId);
            if (selected is null ||
                !string.Equals(
                    selected.DisplayText,
                    trimmedEntry,
                    StringComparison.OrdinalIgnoreCase))
            {
                return AppointmentAssigneeResolution.Invalid(
                    "The selected staff account is unavailable. Search and select it again, or type a custom name.");
            }

            return AppointmentAssigneeResolution.Valid(
                selected.UserId,
                selected.FullName);
        }

        var exactOption = options.FirstOrDefault(option =>
            string.Equals(
                option.DisplayText,
                trimmedEntry,
                StringComparison.OrdinalIgnoreCase));
        if (exactOption is not null)
        {
            return AppointmentAssigneeResolution.Valid(
                exactOption.UserId,
                exactOption.FullName);
        }

        if (trimmedEntry.Length > 200)
        {
            return AppointmentAssigneeResolution.Invalid(
                "The custom Prosthetist / Orthotist name cannot exceed 200 characters.");
        }

        return AppointmentAssigneeResolution.Valid(null, trimmedEntry);
    }

    private static bool IsProsthetistOrOrthotist(string designation)
    {
        return designation.Contains("prosthet", StringComparison.OrdinalIgnoreCase) ||
            designation.Contains("orthot", StringComparison.OrdinalIgnoreCase);
    }
}
