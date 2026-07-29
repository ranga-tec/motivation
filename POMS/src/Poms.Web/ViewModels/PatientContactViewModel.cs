using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class PatientContactViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Telephone No.")]
    [Phone]
    public string? TelephoneNo { get; set; }

    [Display(Name = "Date Confirmed")]
    [DataType(DataType.Date)]
    public DateOnly? DateConfirmed { get; set; }

    [Display(Name = "Person Checked")]
    public string? PersonChecked { get; set; }
}
