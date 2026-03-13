using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;

namespace Poms.Web.Controllers;

[Authorize(Policy = "DataEntry")]
public class DocumentsController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IOcrService _ocrService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        PomsDbContext context,
        IFileStorageService fileStorage,
        IOcrService ocrService,
        ILogger<DocumentsController> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _ocrService = ocrService;
        _logger = logger;
    }

    // GET: Documents
    public async Task<IActionResult> Index(string searchString, OcrStatus? ocrStatus, int page = 1)
    {
        var patientDocs = _context.PatientDocuments
            .Include(d => d.Patient)
            .Select(d => new DocumentListItem
            {
                Id = d.Id,
                Title = d.Title,
                FileName = d.FileName,
                ContentType = d.ContentType,
                UploadedAt = d.UploadedAt,
                OcrStatus = d.OcrStatus,
                PatientNumber = d.Patient.PatientNumber,
                PatientName = d.Patient.FirstName + " " + d.Patient.LastName,
                DocumentType = "Patient"
            });

        var episodeDocs = _context.EpisodeDocuments
            .Include(d => d.Episode)
            .ThenInclude(e => e.Patient)
            .Select(d => new DocumentListItem
            {
                Id = d.Id,
                Title = d.Title,
                FileName = d.FileName,
                ContentType = d.ContentType,
                UploadedAt = d.UploadedAt,
                OcrStatus = d.OcrStatus,
                PatientNumber = d.Episode.Patient.PatientNumber,
                PatientName = d.Episode.Patient.FirstName + " " + d.Episode.Patient.LastName,
                DocumentType = "Episode"
            });

        var query = patientDocs.Union(episodeDocs);

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(d =>
                d.Title.Contains(searchString) ||
                d.FileName.Contains(searchString) ||
                d.PatientNumber.Contains(searchString));
        }

        if (ocrStatus.HasValue)
            query = query.Where(d => d.OcrStatus == ocrStatus.Value);

        var pageSize = 20;
        var totalCount = await query.CountAsync();
        var documents = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.SearchString = searchString;
        ViewBag.OcrStatus = ocrStatus;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.OcrStatuses = Enum.GetValues<OcrStatus>();

        return View(documents);
    }

    // POST: Documents/UploadPatientDocument
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPatientDocument(Guid patientId, IFormFile file, string title, string? remarks, string ocrLanguage = "eng")
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload";
            return RedirectToAction("Details", "Patients", new { id = patientId });
        }

        try
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            var (storagePath, fileName) = await _fileStorage.SaveFileAsync(file, patient.PatientNumber);

            var document = new PatientDocument
            {
                PatientId = patientId,
                Title = title ?? fileName,
                FileName = fileName,
                StoragePath = storagePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Remarks = remarks,
                UploadedBy = User.Identity?.Name ?? "Unknown",
                UploadedAt = DateTime.UtcNow,
                OcrStatus = _ocrService.IsOcrSupported(file.ContentType) ? OcrStatus.Pending : OcrStatus.NotApplicable,
                CreatedBy = User.Identity?.Name
            };

            _context.PatientDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Process OCR in background
            if (document.OcrStatus == OcrStatus.Pending)
            {
                _ = Task.Run(async () =>
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
                    await ocrService.ProcessPatientDocumentOcrAsync(document.Id, ocrLanguage);
                });
            }

            _logger.LogInformation("Document {FileName} uploaded for patient {PatientNumber}",
                fileName, patient.PatientNumber);

            TempData["Success"] = "Document uploaded successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            TempData["Error"] = "Error uploading document: " + ex.Message;
        }

        return RedirectToAction("Details", "Patients", new { id = patientId });
    }

    // POST: Documents/UploadEpisodeDocument
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEpisodeDocument(Guid episodeId, IFormFile file, string title, string? remarks, string ocrLanguage = "eng")
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload";
            return RedirectToAction("Details", "Episodes", new { id = episodeId });
        }

        try
        {
            var episode = await _context.Episodes.Include(e => e.Patient).FirstOrDefaultAsync(e => e.Id == episodeId);
            if (episode == null) return NotFound();

            var (storagePath, fileName) = await _fileStorage.SaveFileAsync(file, episode.Patient.PatientNumber);

            var document = new EpisodeDocument
            {
                EpisodeId = episodeId,
                Title = title ?? fileName,
                FileName = fileName,
                StoragePath = storagePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                Remarks = remarks,
                UploadedBy = User.Identity?.Name ?? "Unknown",
                UploadedAt = DateTime.UtcNow,
                OcrStatus = _ocrService.IsOcrSupported(file.ContentType) ? OcrStatus.Pending : OcrStatus.NotApplicable,
                CreatedBy = User.Identity?.Name
            };

            _context.EpisodeDocuments.Add(document);
            await _context.SaveChangesAsync();

            // Process OCR in background
            if (document.OcrStatus == OcrStatus.Pending)
            {
                _ = Task.Run(async () =>
                {
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
                    await ocrService.ProcessEpisodeDocumentOcrAsync(document.Id, ocrLanguage);
                });
            }

            _logger.LogInformation("Document {FileName} uploaded for episode {EpisodeId}",
                fileName, episodeId);

            TempData["Success"] = "Document uploaded successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            TempData["Error"] = "Error uploading document: " + ex.Message;
        }

        return RedirectToAction("Details", "Episodes", new { id = episodeId });
    }

    // GET: Documents/ViewPatientDocument/5
    public async Task<IActionResult> ViewPatientDocument(Guid id)
    {
        var document = await _context.PatientDocuments
            .Include(d => d.Patient)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

        var fileBytes = await _fileStorage.GetFileAsync(document.StoragePath);
        return File(fileBytes, document.ContentType, document.FileName);
    }

    // GET: Documents/ViewEpisodeDocument/5
    public async Task<IActionResult> ViewEpisodeDocument(Guid id)
    {
        var document = await _context.EpisodeDocuments
            .Include(d => d.Episode)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

        var fileBytes = await _fileStorage.GetFileAsync(document.StoragePath);
        return File(fileBytes, document.ContentType, document.FileName);
    }

    // GET: Documents/PatientDocumentDetails/5
    public async Task<IActionResult> PatientDocumentDetails(Guid id)
    {
        var document = await _context.PatientDocuments
            .Include(d => d.Patient)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

        return View("DocumentDetails", new DocumentDetailsViewModel
        {
            Id = document.Id,
            Title = document.Title,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            UploadedBy = document.UploadedBy,
            UploadedAt = document.UploadedAt,
            Remarks = document.Remarks,
            ExtractedText = document.ExtractedText,
            OcrStatus = document.OcrStatus,
            OcrLanguage = document.OcrLanguage,
            OcrProcessedAt = document.OcrProcessedAt,
            PatientNumber = document.Patient.PatientNumber,
            PatientName = $"{document.Patient.FirstName} {document.Patient.LastName}",
            DocumentType = "Patient",
            ParentId = document.PatientId
        });
    }

    // GET: Documents/EpisodeDocumentDetails/5
    public async Task<IActionResult> EpisodeDocumentDetails(Guid id)
    {
        var document = await _context.EpisodeDocuments
            .Include(d => d.Episode)
            .ThenInclude(e => e.Patient)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null) return NotFound();

        return View("DocumentDetails", new DocumentDetailsViewModel
        {
            Id = document.Id,
            Title = document.Title,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            UploadedBy = document.UploadedBy,
            UploadedAt = document.UploadedAt,
            Remarks = document.Remarks,
            ExtractedText = document.ExtractedText,
            OcrStatus = document.OcrStatus,
            OcrLanguage = document.OcrLanguage,
            OcrProcessedAt = document.OcrProcessedAt,
            PatientNumber = document.Episode.Patient.PatientNumber,
            PatientName = $"{document.Episode.Patient.FirstName} {document.Episode.Patient.LastName}",
            DocumentType = "Episode",
            ParentId = document.EpisodeId
        });
    }

    // POST: Documents/ReprocessOcr
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReprocessOcr(Guid id, string documentType, string language = "eng")
    {
        if (documentType == "Patient")
        {
            await _ocrService.ProcessPatientDocumentOcrAsync(id, language);
            TempData["Success"] = "OCR processing started";
            return RedirectToAction(nameof(PatientDocumentDetails), new { id });
        }
        else
        {
            await _ocrService.ProcessEpisodeDocumentOcrAsync(id, language);
            TempData["Success"] = "OCR processing started";
            return RedirectToAction(nameof(EpisodeDocumentDetails), new { id });
        }
    }

    // POST: Documents/DeletePatientDocument
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeletePatientDocument(Guid id)
    {
        var document = await _context.PatientDocuments.FindAsync(id);
        if (document == null) return NotFound();

        var patientId = document.PatientId;

        // Soft delete
        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();
        await _fileStorage.DeleteFileAsync(document.StoragePath);

        TempData["Success"] = "Document deleted successfully";
        return RedirectToAction("Details", "Patients", new { id = patientId });
    }

    // POST: Documents/DeleteEpisodeDocument
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteEpisodeDocument(Guid id)
    {
        var document = await _context.EpisodeDocuments.FindAsync(id);
        if (document == null) return NotFound();

        var episodeId = document.EpisodeId;

        // Soft delete
        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = User.Identity?.Name;

        await _context.SaveChangesAsync();
        await _fileStorage.DeleteFileAsync(document.StoragePath);

        TempData["Success"] = "Document deleted successfully";
        return RedirectToAction("Details", "Episodes", new { id = episodeId });
    }
}

#region ViewModels

public class DocumentListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public OcrStatus OcrStatus { get; set; }
    public string PatientNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string DocumentType { get; set; } = "";
}

public class DocumentDetailsViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public string UploadedBy { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public string? Remarks { get; set; }
    public string? ExtractedText { get; set; }
    public OcrStatus OcrStatus { get; set; }
    public string? OcrLanguage { get; set; }
    public DateTime? OcrProcessedAt { get; set; }
    public string PatientNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public Guid ParentId { get; set; }

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024:F1} KB";
            return $"{FileSize / 1024 / 1024:F1} MB";
        }
    }
}

#endregion
