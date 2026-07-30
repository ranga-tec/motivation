using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;

namespace Poms.Infrastructure.Services;

public sealed record RestrictedAccessScope(
    string? UserId,
    string? UserName,
    bool HasGlobalAccess)
{
    public bool CanAccess(bool isRestricted, string? createdBy)
    {
        if (!isRestricted || HasGlobalAccess)
            return true;

        return !string.IsNullOrWhiteSpace(UserName) &&
            string.Equals(createdBy, UserName, StringComparison.OrdinalIgnoreCase);
    }

    public IQueryable<Episode> Filter(IQueryable<Episode> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            !record.IsRestricted ||
            (record.CreatedBy != null && record.CreatedBy.ToUpper() == userName));
    }

    public IQueryable<Assessment> Filter(IQueryable<Assessment> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            (!record.IsRestricted ||
                (record.CreatedBy != null && record.CreatedBy.ToUpper() == userName)) &&
            (!record.Episode.IsRestricted ||
                (record.Episode.CreatedBy != null &&
                    record.Episode.CreatedBy.ToUpper() == userName)));
    }

    public IQueryable<Fitting> Filter(IQueryable<Fitting> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            (!record.IsRestricted ||
                (record.CreatedBy != null && record.CreatedBy.ToUpper() == userName)) &&
            (!record.Episode.IsRestricted ||
                (record.Episode.CreatedBy != null &&
                    record.Episode.CreatedBy.ToUpper() == userName)));
    }

    public IQueryable<Delivery> Filter(IQueryable<Delivery> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            (!record.IsRestricted ||
                (record.CreatedBy != null && record.CreatedBy.ToUpper() == userName)) &&
            (!record.Episode.IsRestricted ||
                (record.Episode.CreatedBy != null &&
                    record.Episode.CreatedBy.ToUpper() == userName)));
    }

    public IQueryable<FollowUp> Filter(IQueryable<FollowUp> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            (!record.IsRestricted ||
                (record.CreatedBy != null && record.CreatedBy.ToUpper() == userName)) &&
            (!record.Episode.IsRestricted ||
                (record.Episode.CreatedBy != null &&
                    record.Episode.CreatedBy.ToUpper() == userName)));
    }

    public IQueryable<PatientDocument> Filter(IQueryable<PatientDocument> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            !record.IsRestricted ||
            (record.CreatedBy != null && record.CreatedBy.ToUpper() == userName));
    }

    public IQueryable<EpisodeDocument> Filter(IQueryable<EpisodeDocument> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            (!record.IsRestricted ||
                (record.CreatedBy != null && record.CreatedBy.ToUpper() == userName)) &&
            (!record.Episode.IsRestricted ||
                (record.Episode.CreatedBy != null &&
                    record.Episode.CreatedBy.ToUpper() == userName)));
    }

    public IQueryable<Appointment> Filter(IQueryable<Appointment> query)
    {
        if (HasGlobalAccess) return query;
        var userName = (UserName ?? string.Empty).ToUpper();
        return query.Where(record =>
            record.Episode == null ||
            !record.Episode.IsRestricted ||
            (record.Episode.CreatedBy != null &&
                record.Episode.CreatedBy.ToUpper() == userName));
    }
}

public interface IRestrictedAccessService
{
    Task<RestrictedAccessScope> GetScopeAsync(ClaimsPrincipal user);
    Task AuditAsync(
        RestrictedAccessScope scope,
        string action,
        string entityType,
        Guid entityId,
        bool isRestricted,
        bool allowed,
        object? metadata = null);
}

public sealed class RestrictedAccessService : IRestrictedAccessService
{
    private readonly PomsDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RestrictedAccessService(
        PomsDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RestrictedAccessScope> GetScopeAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = user.Identity?.Name;
        var hasGlobalAccess = user.IsInRole("ADMIN");

        if (!hasGlobalAccess && !string.IsNullOrWhiteSpace(userId))
        {
            hasGlobalAccess = await _context.EmployeeProfiles
                .AsNoTracking()
                .AnyAsync(profile =>
                    profile.UserId == userId &&
                    profile.CanAccessRestrictedClinicalData);
        }

        return new RestrictedAccessScope(userId, userName, hasGlobalAccess);
    }

    public async Task AuditAsync(
        RestrictedAccessScope scope,
        string action,
        string entityType,
        Guid entityId,
        bool isRestricted,
        bool allowed,
        object? metadata = null)
    {
        if (!isRestricted)
            return;

        object details = metadata is null
            ? new { Allowed = allowed }
            : new { Allowed = allowed, Metadata = metadata };

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = scope.UserId ?? scope.UserName ?? "unknown",
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            Changes = JsonSerializer.Serialize(details),
            Timestamp = DateTime.UtcNow,
            IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await _context.SaveChangesAsync();
    }
}
