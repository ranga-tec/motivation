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
[Route("api/v1/documents")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "AnyAuthenticatedUser")]
public sealed class DocumentsApiController(PomsDbContext context, IFileStorageService storage, IRestrictedAccessService restrictedAccess, IConfiguration configuration) : ControllerBase
{
    [HttpGet("options")]
    public ActionResult<DocumentOptionsResponse> Options() => Ok(new DocumentOptionsResponse(
        Enum.GetNames<DocumentType>(), configuration.GetValue<long>("FileStorage:MaxFileSizeMB", 10),
        configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>() ?? [".pdf", ".jpg", ".jpeg", ".png", ".docx"]));

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> List(Guid? patientId, Guid? episodeId, CancellationToken token)
    {
        if (patientId.HasValue == episodeId.HasValue) return Validation("owner", "Specify either patientId or episodeId.");
        var scope = await restrictedAccess.GetScopeAsync(User);
        if (patientId.HasValue)
        {
            if (!await context.Patients.AnyAsync(x => x.Id == patientId, token)) return NotFound();
            var docs = await scope.Filter(context.PatientDocuments.AsNoTracking()).Where(x => x.PatientId == patientId).OrderByDescending(x => x.UploadedAt).ToListAsync(token);
            return Ok(docs.Select(Map).ToList());
        }
        var episode = await context.Episodes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == episodeId, token);
        if (episode is null || !scope.CanAccess(episode.IsRestricted, episode.CreatedBy)) return NotFound();
        var episodeDocs = await scope.Filter(context.EpisodeDocuments.AsNoTracking()).Where(x => x.EpisodeId == episodeId).OrderByDescending(x => x.UploadedAt).ToListAsync(token);
        return Ok(episodeDocs.Select(Map).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "ApiWrite")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentResponse>> Upload([FromForm] UploadDocumentRequest request, CancellationToken token)
    {
        if (request.PatientId.HasValue == request.EpisodeId.HasValue) return Validation("owner", "Specify either patientId or episodeId.");
        if (request.File is null || request.File.Length == 0) return Validation(nameof(request.File), "Select a non-empty file.");
        var actor = Actor(); string patientNumber;
        Episode? episode = null;
        if (request.PatientId.HasValue)
        {
            var patient = await context.Patients.SingleOrDefaultAsync(x => x.Id == request.PatientId, token); if (patient is null) return NotFound(); patientNumber = patient.PatientNumber;
        }
        else
        {
            episode = await context.Episodes.Include(x => x.Patient).SingleOrDefaultAsync(x => x.Id == request.EpisodeId, token); if (episode is null || !await CanAccess(episode, null, "ApiUploadDocument")) return NotFound(); patientNumber = episode.Patient.PatientNumber;
        }
        try
        {
            var stored = await storage.SaveFileAsync(request.File, patientNumber);
            if (episode is null)
            {
                var item = new PatientDocument { PatientId = request.PatientId!.Value, DocumentType = request.DocumentType, FileName = stored.FileName, StoragePath = stored.StoragePath, ContentType = request.File.ContentType, Notes = Clean(request.Notes), UploadedBy = actor, UploadedAt = DateTime.UtcNow, IsRestricted = request.IsRestricted, CreatedBy = actor };
                context.PatientDocuments.Add(item); await context.SaveChangesAsync(token); return Created($"/api/v1/documents/{item.Id}?scope=patient", Map(item));
            }
            var episodeItem = new EpisodeDocument { EpisodeId = episode.Id, DocumentType = request.DocumentType, FileName = stored.FileName, StoragePath = stored.StoragePath, ContentType = request.File.ContentType, FileSize = request.File.Length, Notes = Clean(request.Notes), UploadedBy = actor, UploadedAt = DateTime.UtcNow, IsRestricted = request.IsRestricted, CreatedBy = actor };
            context.EpisodeDocuments.Add(episodeItem); await context.SaveChangesAsync(token); return Created($"/api/v1/documents/{episodeItem.Id}?scope=episode", Map(episodeItem));
        }
        catch (InvalidOperationException error) { return Validation(nameof(request.File), error.Message); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, string scope, CancellationToken token)
    {
        var access = await restrictedAccess.GetScopeAsync(User); string path; string name; string contentType;
        if (scope.Equals("episode", StringComparison.OrdinalIgnoreCase))
        {
            var item = await context.EpisodeDocuments.Include(x => x.Episode).SingleOrDefaultAsync(x => x.Id == id, token); if (item is null) return NotFound();
            var allowed = access.CanAccess(item.Episode.IsRestricted, item.Episode.CreatedBy) && access.CanAccess(item.IsRestricted, item.CreatedBy); await Audit(access, allowed, "Download", item, item.Episode.IsRestricted); if (!allowed) return NotFound(); path = item.StoragePath; name = item.FileName; contentType = item.ContentType;
        }
        else
        {
            var item = await context.PatientDocuments.SingleOrDefaultAsync(x => x.Id == id, token); if (item is null) return NotFound(); var allowed = access.CanAccess(item.IsRestricted, item.CreatedBy); await Audit(access, allowed, "Download", item, false); if (!allowed) return NotFound(); path = item.StoragePath; name = item.FileName; contentType = item.ContentType;
        }
        Response.Headers.CacheControl = "no-store, private";
        try { return File(await storage.GetFileAsync(path), contentType, name); } catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpGet("patient-photo/{patientId:guid}")]
    public async Task<IActionResult> PatientPhoto(Guid patientId, CancellationToken token)
    {
        var access = await restrictedAccess.GetScopeAsync(User);
        var item = await access.Filter(context.PatientDocuments.AsNoTracking())
            .Where(x => x.PatientId == patientId && x.DocumentType == DocumentType.PatientPhoto)
            .OrderByDescending(x => x.UploadedAt)
            .FirstOrDefaultAsync(token);
        if (item is null) return NotFound();

        var allowed = access.CanAccess(item.IsRestricted, item.CreatedBy);
        await Audit(access, allowed, "ViewPatientPhoto", item, false);
        if (!allowed) return NotFound();

        Response.Headers.CacheControl = "no-store, private";
        try { return File(await storage.GetFileAsync(item.StoragePath), item.ContentType); }
        catch (FileNotFoundException) { return NotFound(); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ApiWrite")]
    public async Task<IActionResult> Delete(Guid id, string scope, CancellationToken token)
    {
        var access = await restrictedAccess.GetScopeAsync(User);
        if (scope.Equals("episode", StringComparison.OrdinalIgnoreCase))
        {
            var item = await context.EpisodeDocuments.Include(x => x.Episode).SingleOrDefaultAsync(x => x.Id == id, token); if (item is null) return NotFound(); var allowed = access.CanAccess(item.Episode.IsRestricted, item.Episode.CreatedBy) && access.CanAccess(item.IsRestricted, item.CreatedBy); await Audit(access, allowed, "Delete", item, item.Episode.IsRestricted); if (!allowed) return NotFound(); SoftDelete(item);
        }
        else
        {
            var item = await context.PatientDocuments.SingleOrDefaultAsync(x => x.Id == id, token); if (item is null) return NotFound(); var allowed = access.CanAccess(item.IsRestricted, item.CreatedBy); await Audit(access, allowed, "Delete", item, false); if (!allowed) return NotFound(); SoftDelete(item);
        }
        await context.SaveChangesAsync(token); return NoContent();
    }

    private async Task<bool> CanAccess(Episode episode, EpisodeDocument? document, string action) { var access = await restrictedAccess.GetScopeAsync(User); var allowed = access.CanAccess(episode.IsRestricted, episode.CreatedBy) && (document is null || access.CanAccess(document.IsRestricted, document.CreatedBy)); await restrictedAccess.AuditAsync(access, allowed ? action : $"{action}Denied", document is null ? nameof(Episode) : nameof(EpisodeDocument), document?.Id ?? episode.Id, episode.IsRestricted || document?.IsRestricted == true, allowed); return allowed; }
    private Task Audit(RestrictedAccessScope access, bool allowed, string action, Poms.Domain.Common.BaseEntity item, bool parentRestricted) => restrictedAccess.AuditAsync(access, allowed ? action : $"{action}Denied", item.GetType().Name, item.Id, parentRestricted || item is PatientDocument { IsRestricted: true } || item is EpisodeDocument { IsRestricted: true }, allowed);
    private void SoftDelete(Poms.Domain.Common.BaseEntity item) { item.IsDeleted = true; item.DeletedAt = DateTime.UtcNow; item.DeletedBy = Actor(); }
    private BadRequestObjectResult Validation(string field, string message) => BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] }) { Status = 400 });
    private string Actor() => User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "API user";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static DocumentResponse Map(PatientDocument x) => new(x.Id, "patient", x.PatientId, x.DocumentType.ToString(), x.FileName, x.ContentType, null, x.Notes, x.UploadedBy, x.UploadedAt, x.IsRestricted);
    private static DocumentResponse Map(EpisodeDocument x) => new(x.Id, "episode", x.EpisodeId, x.DocumentType.ToString(), x.FileName, x.ContentType, x.FileSize, x.Notes, x.UploadedBy, x.UploadedAt, x.IsRestricted);
}
