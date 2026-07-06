using Poms.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Poms.Web.ViewModels;

public class AppointmentViewModel
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public Guid PatientId { get; set; }

    [Display(Name = "Record")]
    public Guid? EpisodeId { get; set; }

    [Required]
    [Display(Name = "Appointment Type")]
    public AppointmentType Type { get; set; }

    [Required]
    [Display(Name = "Appointment Date")]
    public DateOnly AppointmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Appointment Time")]
    public TimeOnly? AppointmentTime { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public string? PatientNumber { get; set; }
    public string? PatientName { get; set; }
}
