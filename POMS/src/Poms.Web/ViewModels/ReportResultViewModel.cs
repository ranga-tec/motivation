using Poms.Infrastructure.Services;

namespace Poms.Web.ViewModels;

public class ReportResultViewModel
{
    public string Title { get; set; } = default!;
    public string ActionName { get; set; } = default!;
    public string[] Headers { get; set; } = Array.Empty<string>();
    public List<string[]> Rows { get; set; } = new();
    public ReportFilter Filter { get; set; } = new();
}
