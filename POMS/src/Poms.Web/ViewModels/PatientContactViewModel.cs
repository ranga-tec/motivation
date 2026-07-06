using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class PatientContactViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [Display(Name = "Telephone No.")]
    [Phone]
    public string TelephoneNo { get; set; } = default!;

    [Display(Name = "Date Confirmed")]
    [DataType(DataType.Date)]
    public DateOnly? DateConfirmed { get; set; }

    [Display(Name = "Person Checked")]
    public string? PersonChecked { get; set; }
}
