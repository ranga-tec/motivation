// ============================================================================
// Poms.Infrastructure/Services/OcrService.cs
// ============================================================================
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;

namespace Poms.Infrastructure.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(string filePath, string language = "eng");
    Task ProcessPatientDocumentOcrAsync(Guid documentId, string language = "eng");
    Task ProcessEpisodeDocumentOcrAsync(Guid documentId, string language = "eng");
    bool IsOcrSupported(string contentType);
}

public class OcrService : IOcrService
{
    private readonly PomsDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly string[] _supportedContentTypes = new[]
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/tiff", "image/bmp",
        "application/pdf"
    };

    public OcrService(PomsDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public bool IsOcrSupported(string contentType)
    {
        return _supportedContentTypes.Contains(contentType.ToLowerInvariant());
    }

    public async Task<string> ExtractTextAsync(string filePath, string language = "eng")
    {
        try
        {
            var fullPath = _fileStorage.GetFullPath(filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Document file not found", filePath);
            }

            var extension = Path.GetExtension(fullPath).ToLowerInvariant();

            // For PDF files, use PdfPig to extract text
            if (extension == ".pdf")
            {
                return await ExtractTextFromPdfAsync(fullPath);
            }

            // For images, use Tesseract OCR
            return await ExtractTextFromImageAsync(fullPath, language);
        }
        catch (Exception ex)
        {
            // Log error and return empty string instead of throwing
            return $"[OCR Error: {ex.Message}]";
        }
    }

    private async Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var document = UglyToad.PdfPig.PdfDocument.Open(filePath);
                var text = new System.Text.StringBuilder();

                foreach (var page in document.GetPages())
                {
                    text.AppendLine(page.Text);
                }

                return text.ToString().Trim();
            }
            catch (Exception ex)
            {
                return $"[PDF Text Extraction Error: {ex.Message}]";
            }
        });
    }

    private async Task<string> ExtractTextFromImageAsync(string filePath, string language)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Note: Tesseract requires tessdata folder with language files
                // For production, configure the tessdata path appropriately
                var tessDataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX")
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                if (!Directory.Exists(tessDataPath))
                {
                    return "[OCR not available: tessdata folder not found. Please install Tesseract language data.]";
                }

                using var engine = new Tesseract.TesseractEngine(tessDataPath, language, Tesseract.EngineMode.Default);
                using var img = Tesseract.Pix.LoadFromFile(filePath);
                using var page = engine.Process(img);

                return page.GetText().Trim();
            }
            catch (Exception ex)
            {
                return $"[OCR Error: {ex.Message}]";
            }
        });
    }

    public async Task ProcessPatientDocumentOcrAsync(Guid documentId, string language = "eng")
    {
        var document = await _context.PatientDocuments.FindAsync(documentId);
        if (document == null) return;

        if (!IsOcrSupported(document.ContentType))
        {
            document.OcrStatus = OcrStatus.NotApplicable;
            await _context.SaveChangesAsync();
            return;
        }

        try
        {
            document.OcrStatus = OcrStatus.Processing;
            await _context.SaveChangesAsync();

            var extractedText = await ExtractTextAsync(document.StoragePath, language);

            document.ExtractedText = extractedText;
            document.OcrProcessedAt = DateTime.UtcNow;
            document.OcrLanguage = language;
            document.OcrStatus = string.IsNullOrEmpty(extractedText) || extractedText.StartsWith("[")
                ? OcrStatus.Failed
                : OcrStatus.Completed;

            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            document.OcrStatus = OcrStatus.Failed;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ProcessEpisodeDocumentOcrAsync(Guid documentId, string language = "eng")
    {
        var document = await _context.EpisodeDocuments.FindAsync(documentId);
        if (document == null) return;

        if (!IsOcrSupported(document.ContentType))
        {
            document.OcrStatus = OcrStatus.NotApplicable;
            await _context.SaveChangesAsync();
            return;
        }

        try
        {
            document.OcrStatus = OcrStatus.Processing;
            await _context.SaveChangesAsync();

            var extractedText = await ExtractTextAsync(document.StoragePath, language);

            document.ExtractedText = extractedText;
            document.OcrProcessedAt = DateTime.UtcNow;
            document.OcrLanguage = language;
            document.OcrStatus = string.IsNullOrEmpty(extractedText) || extractedText.StartsWith("[")
                ? OcrStatus.Failed
                : OcrStatus.Completed;

            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            document.OcrStatus = OcrStatus.Failed;
            await _context.SaveChangesAsync();
        }
    }
}
