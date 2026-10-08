using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/episodes")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "ClinicianOrAdmin")]
public sealed class EpisodesApiController(PomsDbContext context, IRestrictedAccessService restrictedAccess) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<EpisodeOptionsResponse>> Options(CancellationToken cancellationToken)
    {
        var centers = await context.Centers.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.Name)
            .Select(item => new EpisodeOption(item.Id, item.Name)).ToListAsync(cancellationToken);
        return Ok(new EpisodeOptionsResponse(centers, Enum.GetNames<RecordStatus>()));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EpisodeResponse>>> List(
        [FromQuery] Guid patientId,
        CancellationToken cancellationToken)
    {
        if (patientId == Guid.Empty)
            return Validation(nameof(patientId), "Select a patient.");
        if (!await context.Patients.AnyAsync(item => item.Id == patientId, cancellationToken))
            return NotFound();

        var access = await restrictedAccess.GetScopeAsync(User);
        var episodes = await access.Filter(Query().AsNoTracking())
            .Where(item => item.PatientId == patientId)
            .OrderByDescending(item => item.RecordDate).ThenByDescending(item => item.RecordTime)
            .ToListAsync(cancellationToken);
        return Ok(episodes.Select(Map).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EpisodeResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var episode = await Query().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (episode is null || !await CanAccessAsync(episode, "ApiViewEpisode"))
            return NotFound();
        return Ok(Map(episode));
    }

    [HttpPost]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<EpisodeResponse>> Create(SaveEpisodeRequest request, CancellationToken cancellationToken)
    {
        if (!await context.Patients.AnyAsync(item => item.Id == request.PatientId, cancellationToken))
            return Validation(nameof(request.PatientId), "Select an existing patient.");
        if (!await ActiveCenterExists(request.CenterId, cancellationToken))
            return Validation(nameof(request.CenterId), "Select an active treatment centre.");

        var episode = new Episode
        {
            PatientId = request.PatientId,
            CenterId = request.CenterId,
            Status = request.Status,
            RecordDate = request.RecordDate,
            RecordTime = request.RecordTime,
            Remarks = request.Remarks?.Trim(),
            IsRestricted = request.IsRestricted,
            CreatedBy = Actor()
        };
        context.Episodes.Add(episode);
        await context.SaveChangesAsync(cancellationToken);
        var saved = await Query().SingleAsync(item => item.Id == episode.Id, cancellationToken);
        await AuditAsync(saved, "ApiCreateEpisode", true);
        return CreatedAtAction(nameof(Get), new { id = saved.Id }, Map(saved));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<EpisodeResponse>> Update(Guid id, SaveEpisodeRequest request, CancellationToken cancellationToken)
    {
        var episode = await Query().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (episode is null || !await CanAccessAsync(episode, "ApiUpdateEpisode"))
            return NotFound();
        if (request.PatientId != episode.PatientId)
            return Validation(nameof(request.PatientId), "The patient for an existing record cannot be changed.");
        if (!await ActiveCenterExists(request.CenterId, cancellationToken))
            return Validation(nameof(request.CenterId), "Select an active treatment centre.");

        var wasRestricted = episode.IsRestricted;
        episode.CenterId = request.CenterId;
        episode.Status = request.Status;
        episode.RecordDate = request.RecordDate;
        episode.RecordTime = request.RecordTime;
        episode.Remarks = request.Remarks?.Trim();
        episode.IsRestricted = request.IsRestricted;
        episode.UpdatedBy = Actor();
        episode.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        await restrictedAccess.AuditAsync(await restrictedAccess.GetScopeAsync(User), "ApiUpdateEpisode", nameof(Episode), episode.Id, wasRestricted || episode.IsRestricted, true);
        var saved = await Query().AsNoTracking().SingleAsync(item => item.Id == id, cancellationToken);
        return Ok(Map(saved));
    }

    private IQueryable<Episode> Query() => context.Episodes
        .Include(item => item.Patient).Include(item => item.Center)
        .Include(item => item.Assessments).Include(item => item.Fittings)
        .Include(item => item.Deliveries).Include(item => item.FollowUps).Include(item => item.Documents);

    private async Task<bool> ActiveCenterExists(int id, CancellationToken cancellationToken) =>
        await context.Centers.AnyAsync(item => item.Id == id && item.IsActive, cancellationToken);

    private async Task<bool> CanAccessAsync(Episode episode, string action)
    {
        var access = await restrictedAccess.GetScopeAsync(User);
        var allowed = access.CanAccess(episode.IsRestricted, episode.CreatedBy);
        await restrictedAccess.AuditAsync(access, allowed ? action : $"{action}Denied", nameof(Episode), episode.Id, episode.IsRestricted, allowed);
        return allowed;
    }

    private async Task AuditAsync(Episode episode, string action, bool succeeded) =>
        await restrictedAccess.AuditAsync(await restrictedAccess.GetScopeAsync(User), action, nameof(Episode), episode.Id, episode.IsRestricted, succeeded);

    private BadRequestObjectResult Validation(string field, string message) => BadRequest(
        new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] })
        { Status = StatusCodes.Status400BadRequest });

    private string Actor() => User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "API user";

    private static EpisodeResponse Map(Episode item) => new(
        item.Id, item.PatientId, item.Patient.PatientNumber, item.Patient.FullName,
        item.CenterId, item.Center.Name, item.Status.ToString(), item.RecordDate, item.RecordTime,
        item.Remarks, item.IsRestricted, item.Assessments.Count, item.Fittings.Count,
        item.Deliveries.Count, item.FollowUps.Count, item.Documents.Count);
}
