using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class ReportFilterViewModel
{
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "Center")]
    public int? CenterId { get; set; }

    public string? Format { get; set; } // pdf or excel
}

public class EpisodeReportFilterViewModel : ReportFilterViewModel
{
    [Display(Name = "Episode Type")]
    public string? EpisodeType { get; set; }

    [Display(Name = "Status")]
    public string? Status { get; set; }
}

public class PatientListFilterViewModel
{
    [Display(Name = "Center")]
    public int? CenterId { get; set; }

    [Display(Name = "Status")]
    public bool? IsActive { get; set; }

    public string? Format { get; set; } // pdf or excel
}
