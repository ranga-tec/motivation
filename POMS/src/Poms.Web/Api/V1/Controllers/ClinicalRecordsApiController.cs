using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Constants;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/clinical-records")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "ClinicianOrAdmin")]
public sealed class ClinicalRecordsApiController(PomsDbContext context, IRestrictedAccessService restrictedAccess) : ControllerBase
{
    [HttpGet("options")]
    public async Task<ActionResult<ClinicalOptionsResponse>> Options(CancellationToken token) => Ok(new ClinicalOptionsResponse(
        await context.MainProblemTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new ClinicalOption(x.Id, x.Name)).ToListAsync(token),
        await context.CauseReasonTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new ClinicalOption(x.Id, x.Name)).ToListAsync(token),
        await context.DeviceCatalogs.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new ClinicalOption(x.Id, x.Name)).ToListAsync(token),
        Enum.GetNames<AssessmentType>(), Enum.GetNames<LimbCategory>(), Enum.GetNames<Side>()));

    [HttpGet("prescription-options")]
    public ActionResult<IReadOnlyList<PrescriptionOptionResponse>> PrescriptionOptions(AssessmentType assessmentType, LimbCategory limbCategory)
    {
        if (assessmentType == AssessmentType.Prosthetic && limbCategory == LimbCategory.Spinal)
            return Validation(nameof(limbCategory), "Spinal is only valid for Orthotic assessments.");
        return Ok(PrescriptionLists.For(assessmentType, limbCategory).Select(x => new PrescriptionOptionResponse(x.Code, x.Label, x.SubTypes ?? [])).ToList());
    }

    [HttpGet("episodes/{episodeId:guid}")]
    public async Task<ActionResult<EpisodeClinicalRecordsResponse>> List(Guid episodeId, CancellationToken token)
    {
        var episode = await context.Episodes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == episodeId, token);
        if (episode is null || !await CanAccess(episode, null, "ApiViewClinicalRecords")) return NotFound();
        var scope = await restrictedAccess.GetScopeAsync(User);
        var assessments = (await context.Assessments.AsNoTracking().Where(x => x.EpisodeId == episodeId).Include(x => x.MainProblemType).Include(x => x.CauseReasonType).Include(x => x.Prescriptions).OrderByDescending(x => x.AssessedOn).ToListAsync(token)).Where(x => scope.CanAccess(x.IsRestricted, x.CreatedBy)).Select(Map).ToList();
        var fittings = (await context.Fittings.AsNoTracking().Where(x => x.EpisodeId == episodeId).OrderByDescending(x => x.FittingDate).ToListAsync(token)).Where(x => scope.CanAccess(x.IsRestricted, x.CreatedBy)).Select(Map).ToList();
        var deliveries = (await context.Deliveries.AsNoTracking().Where(x => x.EpisodeId == episodeId).Include(x => x.Device).OrderByDescending(x => x.DeliveryDate).ToListAsync(token)).Where(x => scope.CanAccess(x.IsRestricted, x.CreatedBy)).Select(Map).ToList();
        var followUps = (await context.FollowUps.AsNoTracking().Where(x => x.EpisodeId == episodeId).OrderByDescending(x => x.FollowUpDate).ToListAsync(token)).Where(x => scope.CanAccess(x.IsRestricted, x.CreatedBy)).Select(Map).ToList();
        return Ok(new EpisodeClinicalRecordsResponse(assessments, fittings, deliveries, followUps));
    }

    [HttpPost("assessments")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<AssessmentResponse>> CreateAssessment(SaveAssessmentRequest request, CancellationToken token)
    {
        var episode = await FindEpisode(request.EpisodeId, token); if (episode is null || !await CanAccess(episode, null, "ApiCreateAssessment")) return NotFound();
        var problem = await ValidateAssessmentReferences(request, token); if (problem is not null) return problem;
        var item = new Assessment { EpisodeId = episode.Id, AssessmentType = request.AssessmentType, LimbCategory = request.LimbCategory, AssessedOn = request.AssessedOn, StartTime = request.StartTime, EndTime = request.EndTime, MainProblemTypeId = request.MainProblemTypeId, Side = request.Side, CauseReasonTypeId = request.CauseReasonTypeId, CauseReasonOther = Clean(request.CauseReasonOther), AdditionalInformation = Clean(request.AdditionalInformation), IsRestricted = request.IsRestricted, CreatedBy = Actor() };
        context.Assessments.Add(item); await context.SaveChangesAsync(token); AddPrescriptions(item, request); await context.SaveChangesAsync(token);
        return Created($"/api/v1/clinical-records/assessments/{item.Id}", Map(await AssessmentQuery().SingleAsync(x => x.Id == item.Id, token)));
    }

    [HttpPut("assessments/{id:guid}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<AssessmentResponse>> UpdateAssessment(Guid id, SaveAssessmentRequest request, CancellationToken token)
    {
        var item = await AssessmentQuery().SingleOrDefaultAsync(x => x.Id == id, token); if (item is null || request.EpisodeId != item.EpisodeId || !await CanAccess(item.Episode, item, "ApiUpdateAssessment")) return NotFound();
        var problem = await ValidateAssessmentReferences(request, token); if (problem is not null) return problem;
        item.AssessmentType = request.AssessmentType; item.LimbCategory = request.LimbCategory; item.AssessedOn = request.AssessedOn; item.StartTime = request.StartTime; item.EndTime = request.EndTime; item.MainProblemTypeId = request.MainProblemTypeId; item.Side = request.Side; item.CauseReasonTypeId = request.CauseReasonTypeId; item.CauseReasonOther = Clean(request.CauseReasonOther); item.AdditionalInformation = Clean(request.AdditionalInformation); item.IsRestricted = request.IsRestricted; Touch(item);
        context.Prescriptions.RemoveRange(item.Prescriptions); AddPrescriptions(item, request); await context.SaveChangesAsync(token);
        return Ok(Map(await AssessmentQuery().AsNoTracking().SingleAsync(x => x.Id == id, token)));
    }

    [HttpPost("fittings")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<FittingResponse>> CreateFitting(SaveFittingRequest request, CancellationToken token)
    {
        var episode = await FindEpisode(request.EpisodeId, token); if (episode is null || !await CanAccess(episode, null, "ApiCreateFitting")) return NotFound();
        if (request.FittingDate == default) return Validation(nameof(request.FittingDate), "Select a fitting date.");
        var item = new Fitting { EpisodeId = episode.Id, FittingDate = request.FittingDate, Notes = Clean(request.Notes), IsRestricted = request.IsRestricted, CreatedBy = Actor() }; context.Fittings.Add(item); await context.SaveChangesAsync(token); return Created($"/api/v1/clinical-records/fittings/{item.Id}", Map(item));
    }

    [HttpPut("fittings/{id:guid}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<FittingResponse>> UpdateFitting(Guid id, SaveFittingRequest request, CancellationToken token)
    {
        var item = await context.Fittings.Include(x => x.Episode).SingleOrDefaultAsync(x => x.Id == id, token); if (item is null || request.EpisodeId != item.EpisodeId || !await CanAccess(item.Episode, item, "ApiUpdateFitting")) return NotFound();
        if (request.FittingDate == default) return Validation(nameof(request.FittingDate), "Select a fitting date.");
        item.FittingDate = request.FittingDate; item.Notes = Clean(request.Notes); item.IsRestricted = request.IsRestricted; Touch(item); await context.SaveChangesAsync(token); return Ok(Map(item));
    }

    [HttpPost("deliveries")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<DeliveryResponse>> CreateDelivery(SaveDeliveryRequest request, CancellationToken token)
    {
        var episode = await FindEpisode(request.EpisodeId, token); if (episode is null || !await CanAccess(episode, null, "ApiCreateDelivery")) return NotFound();
        var problem = await ValidateDelivery(request, token); if (problem is not null) return problem;
        var item = new Delivery { EpisodeId = episode.Id, DeliveryDate = request.DeliveryDate, DeliveryTime = request.DeliveryTime, Notes = Clean(request.Notes), DeviceId = request.DeviceId, IsRestricted = request.IsRestricted, CreatedBy = Actor() }; context.Deliveries.Add(item); await context.SaveChangesAsync(token); return Created($"/api/v1/clinical-records/deliveries/{item.Id}", Map(await context.Deliveries.AsNoTracking().Include(x => x.Device).SingleAsync(x => x.Id == item.Id, token)));
    }

    [HttpPut("deliveries/{id:guid}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<DeliveryResponse>> UpdateDelivery(Guid id, SaveDeliveryRequest request, CancellationToken token)
    {
        var item = await context.Deliveries.Include(x => x.Episode).Include(x => x.Device).SingleOrDefaultAsync(x => x.Id == id, token); if (item is null || request.EpisodeId != item.EpisodeId || !await CanAccess(item.Episode, item, "ApiUpdateDelivery")) return NotFound();
        var problem = await ValidateDelivery(request, token); if (problem is not null) return problem;
        item.DeliveryDate = request.DeliveryDate; item.DeliveryTime = request.DeliveryTime; item.Notes = Clean(request.Notes); item.DeviceId = request.DeviceId; item.IsRestricted = request.IsRestricted; Touch(item); await context.SaveChangesAsync(token); return Ok(Map(await context.Deliveries.AsNoTracking().Include(x => x.Device).SingleAsync(x => x.Id == id, token)));
    }

    [HttpPost("follow-ups")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<FollowUpResponse>> CreateFollowUp(SaveFollowUpRequest request, CancellationToken token)
    {
        var episode = await FindEpisode(request.EpisodeId, token); if (episode is null || !await CanAccess(episode, null, "ApiCreateFollowUp")) return NotFound();
        var item = new FollowUp { EpisodeId = episode.Id, FollowUpDate = request.FollowUpDate, StartTime = request.StartTime, EndTime = request.EndTime, Notes = Clean(request.Notes), IsRestricted = request.IsRestricted, CreatedBy = Actor() }; context.FollowUps.Add(item); await context.SaveChangesAsync(token); return Created($"/api/v1/clinical-records/follow-ups/{item.Id}", Map(item));
    }

    [HttpPut("follow-ups/{id:guid}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<ActionResult<FollowUpResponse>> UpdateFollowUp(Guid id, SaveFollowUpRequest request, CancellationToken token)
    {
        var item = await context.FollowUps.Include(x => x.Episode).SingleOrDefaultAsync(x => x.Id == id, token); if (item is null || request.EpisodeId != item.EpisodeId || !await CanAccess(item.Episode, item, "ApiUpdateFollowUp")) return NotFound();
        item.FollowUpDate = request.FollowUpDate; item.StartTime = request.StartTime; item.EndTime = request.EndTime; item.Notes = Clean(request.Notes); item.IsRestricted = request.IsRestricted; Touch(item); await context.SaveChangesAsync(token); return Ok(Map(item));
    }

    private IQueryable<Assessment> AssessmentQuery() => context.Assessments.Include(x => x.Episode).Include(x => x.MainProblemType).Include(x => x.CauseReasonType).Include(x => x.Prescriptions);
    private async Task<Episode?> FindEpisode(Guid id, CancellationToken token) => await context.Episodes.SingleOrDefaultAsync(x => x.Id == id, token);
    private async Task<ActionResult?> ValidateAssessmentReferences(SaveAssessmentRequest request, CancellationToken token)
    {
        if (!await context.MainProblemTypes.AnyAsync(x => x.Id == request.MainProblemTypeId && x.IsActive, token)) return Validation(nameof(request.MainProblemTypeId), "Select an active main problem type.");
        if (!await context.CauseReasonTypes.AnyAsync(x => x.Id == request.CauseReasonTypeId && x.IsActive, token)) return Validation(nameof(request.CauseReasonTypeId), "Select an active cause or reason type.");
        var options = PrescriptionLists.For(request.AssessmentType, request.LimbCategory);
        if (request.Prescriptions.Any(x => options.All(o => o.Code != x.Code))) return Validation(nameof(request.Prescriptions), "Select valid prescriptions for the assessment type and limb category.");
        return null;
    }
    private async Task<ActionResult?> ValidateDelivery(SaveDeliveryRequest request, CancellationToken token)
    {
        if (request.DeliveryDate == default) return Validation(nameof(request.DeliveryDate), "Select a delivery date.");
        if (!request.DeliveryTime.HasValue) return Validation(nameof(request.DeliveryTime), "Select a delivery time.");
        if (request.DeviceId.HasValue && !await context.DeviceCatalogs.AnyAsync(x => x.Id == request.DeviceId && x.IsActive, token)) return Validation(nameof(request.DeviceId), "Select an active device.");
        return null;
    }
    private void AddPrescriptions(Assessment item, SaveAssessmentRequest request)
    {
        var options = PrescriptionLists.For(request.AssessmentType, request.LimbCategory);
        foreach (var row in request.Prescriptions) { var option = options.Single(x => x.Code == row.Code); context.Prescriptions.Add(new Prescription { AssessmentId = item.Id, Side = row.Side, PrescriptionCode = row.Code, PrescriptionLabel = option.Label, SubType = Clean(row.SubType), OtherText = Clean(row.OtherText), CreatedBy = Actor() }); }
    }
    private async Task<bool> CanAccess(Episode episode, Poms.Domain.Common.BaseEntity? child, string action)
    {
        var scope = await restrictedAccess.GetScopeAsync(User); var allowed = scope.CanAccess(episode.IsRestricted, episode.CreatedBy) && (child is null || scope.CanAccess(child switch { Assessment x => x.IsRestricted, Fitting x => x.IsRestricted, Delivery x => x.IsRestricted, FollowUp x => x.IsRestricted, _ => false }, child.CreatedBy));
        await restrictedAccess.AuditAsync(scope, allowed ? action : $"{action}Denied", child?.GetType().Name ?? nameof(Episode), child?.Id ?? episode.Id, episode.IsRestricted || child is Assessment { IsRestricted: true } || child is Fitting { IsRestricted: true } || child is Delivery { IsRestricted: true } || child is FollowUp { IsRestricted: true }, allowed); return allowed;
    }
    private BadRequestObjectResult Validation(string field, string message) => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] }) { Status = 400 });
    private string Actor() => User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "API user";
    private void Touch(Poms.Domain.Common.BaseEntity item) { item.UpdatedBy = Actor(); item.UpdatedAt = DateTime.UtcNow; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AssessmentResponse Map(Assessment x) => new(x.Id, x.EpisodeId, x.AssessmentType.ToString(), x.LimbCategory.ToString(), x.AssessedOn, x.StartTime, x.EndTime, x.MainProblemTypeId, x.MainProblemType.Name, x.Side.ToString(), x.CauseReasonTypeId, x.CauseReasonType.Name, x.CauseReasonOther, x.AdditionalInformation, x.IsRestricted, x.Prescriptions.OrderBy(p => p.Side).Select(p => new PrescriptionResponse(p.Id, p.Side.ToString(), p.PrescriptionCode, p.PrescriptionLabel, p.SubType, p.OtherText)).ToList());
    private static FittingResponse Map(Fitting x) => new(x.Id, x.EpisodeId, x.FittingDate, x.Notes, x.IsRestricted);
    private static DeliveryResponse Map(Delivery x) => new(x.Id, x.EpisodeId, x.DeliveryDate, x.DeliveryTime, x.Notes, x.DeviceId, x.Device?.Name, x.IsRestricted);
    private static FollowUpResponse Map(FollowUp x) => new(x.Id, x.EpisodeId, x.FollowUpDate, x.StartTime, x.EndTime, x.Notes, x.IsRestricted);
}
