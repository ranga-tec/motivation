using System.ComponentModel.DataAnnotations;
using Poms.Domain.Enums;

namespace Poms.Web.ViewModels;

public class DocumentListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = "";
    public OcrStatus OcrStatus { get; set; }
    public string? ExtractedText { get; set; }
    public string DocumentType { get; set; } = "patient"; // "patient" or "episode"

    // For linking
    public Guid? PatientId { get; set; }
    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }
    public Guid? EpisodeId { get; set; }

    public string FileSizeFormatted => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class DocumentUploadViewModel
{
    [Required]
    [Display(Name = "Title")]
    public string Title { get; set; } = "";

    [Required]
    [Display(Name = "Document")]
    public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }

    [Display(Name = "Remarks")]
    public string? Remarks { get; set; }

    public Guid? PatientId { get; set; }
    public Guid? EpisodeId { get; set; }
}

public class DocumentDetailsViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; } = "";
    public string? Remarks { get; set; }
    public OcrStatus OcrStatus { get; set; }
    public string? ExtractedText { get; set; }
    public DateTime? OcrProcessedAt { get; set; }
    public string? OcrLanguage { get; set; }
    public string DocumentType { get; set; } = "patient"; // "patient" or "episode"
    public Guid? ParentId { get; set; } // PatientId or EpisodeId

    public Guid? PatientId { get; set; }
    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }
    public Guid? EpisodeId { get; set; }

    public string FileSizeFormatted => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
