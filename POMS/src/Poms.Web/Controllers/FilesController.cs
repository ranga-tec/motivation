using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.ViewModels;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AnyAuthenticatedUser")]
public class FilesController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public FilesController(PomsDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    // GET: Files/UploadForPatient?patientId=
    public async Task<IActionResult> UploadForPatient(Guid patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) return NotFound();

        PopulateDocumentTypes();
        return View(new FileUploadViewModel
        {
            PatientId = patientId,
            PatientNumber = patient.PatientNumber,
            PatientName = patient.FullName
        });
    }

    // POST: Files/UploadForPatient
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadForPatient(FileUploadViewModel model)
    {
        if (model.PatientId == null) return NotFound();

        var patient = await _context.Patients.FindAsync(model.PatientId.Value);
        if (patient == null) return NotFound();

        if (ModelState.IsValid)
        {
            var (storagePath, fileName) = await _fileStorageService.SaveFileAsync(model.File, patient.PatientNumber);
            _context.PatientDocuments.Add(new PatientDocument
            {
                PatientId = patient.Id,
                DocumentType = model.DocumentType,
                FileName = fileName,
                StoragePath = storagePath,
                ContentType = model.File.ContentType,
                Notes = model.Notes,
                UploadedBy = User.Identity?.Name ?? "",
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "File uploaded.";
            return RedirectToAction("Folder", "Patients", new { id = patient.Id });
        }

        PopulateDocumentTypes();
        return View(model);
    }

    // GET: Files/UploadForEpisode?episodeId=
    public async Task<IActionResult> UploadForEpisode(Guid episodeId)
    {
        var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == episodeId);
        if (episode == null) return NotFound();

        PopulateDocumentTypes();
        return View(new FileUploadViewModel
        {
            EpisodeId = episodeId,
            PatientNumber = episode.Patient.PatientNumber,
            PatientName = episode.Patient.FullName
        });
    }

    // POST: Files/UploadForEpisode
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadForEpisode(FileUploadViewModel model)
    {
        if (model.EpisodeId == null) return NotFound();

        var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == model.EpisodeId.Value);
        if (episode == null) return NotFound();

        if (ModelState.IsValid)
        {
            var (storagePath, fileName) = await _fileStorageService.SaveFileAsync(model.File, episode.Patient.PatientNumber);
            _context.EpisodeDocuments.Add(new EpisodeDocument
            {
                EpisodeId = episode.Id,
                DocumentType = model.DocumentType,
                FileName = fileName,
                StoragePath = storagePath,
                ContentType = model.File.ContentType,
                FileSize = model.File.Length,
                Notes = model.Notes,
                UploadedBy = User.Identity?.Name ?? "",
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "File uploaded.";
            return RedirectToAction("Details", "Episodes", new { id = episode.Id });
        }

        PopulateDocumentTypes();
        return View(model);
    }

    // GET: Files/Download/5?scope=patient|episode
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> PatientPhoto(Guid patientId)
    {
        var photo = await _context.PatientDocuments
            .Where(d => d.PatientId == patientId && d.DocumentType == DocumentType.PatientPhoto)
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefaultAsync();
        if (photo == null) return NotFound();

        try
        {
            var bytes = await _fileStorageService.GetFileAsync(photo.StoragePath);
            return File(bytes, photo.ContentType);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    // GET: Files/Download/5?scope=patient|episode
    public async Task<IActionResult> Download(Guid id, string scope)
    {
        string storagePath, fileName, contentType;

        if (scope == "episode")
        {
            var doc = await _context.EpisodeDocuments.FindAsync(id);
            if (doc == null) return NotFound();
            storagePath = doc.StoragePath; fileName = doc.FileName; contentType = doc.ContentType;
        }
        else
        {
            var doc = await _context.PatientDocuments.FindAsync(id);
            if (doc == null) return NotFound();
            storagePath = doc.StoragePath; fileName = doc.FileName; contentType = doc.ContentType;
        }

        var bytes = await _fileStorageService.GetFileAsync(storagePath);
        return File(bytes, contentType, fileName);
    }

    // POST: Files/Delete/5?scope=patient|episode
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, string scope, Guid returnId)
    {
        if (scope == "episode")
        {
            var doc = await _context.EpisodeDocuments.FindAsync(id);
            if (doc != null)
            {
                doc.IsDeleted = true;
                doc.DeletedAt = DateTime.UtcNow;
                doc.DeletedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", "Episodes", new { id = returnId });
        }
        else
        {
            var doc = await _context.PatientDocuments.FindAsync(id);
            if (doc != null)
            {
                doc.IsDeleted = true;
                doc.DeletedAt = DateTime.UtcNow;
                doc.DeletedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Folder", "Patients", new { id = returnId });
        }
    }

    private void PopulateDocumentTypes()
    {
        ViewBag.DocumentTypes = new SelectList(
            Enum.GetValues(typeof(DocumentType))
                .Cast<DocumentType>()
                .Where(type => type != DocumentType.PatientPhoto));
    }
}
