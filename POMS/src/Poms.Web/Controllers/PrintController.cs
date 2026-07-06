using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Infrastructure.Data;
using Poms.Reporting.Models;
using Poms.Reporting.Services;

namespace Poms.Web.Controllers;

[Authorize(Policy = "AnyAuthenticatedUser")]
public class PrintController : Controller
{
    private readonly PomsDbContext _context;
    private readonly IPrintFormService _printFormService;

    public PrintController(PomsDbContext context, IPrintFormService printFormService)
    {
        _context = context;
        _printFormService = printFormService;
    }

    // GET: Print/RegistrationForm/{patientId}
    public async Task<IActionResult> RegistrationForm(Guid patientId)
    {
        var patient = await _context.Patients
            .Include(p => p.Province).Include(p => p.District).Include(p => p.City)
            .Include(p => p.Center).Include(p => p.ReferralSource).Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient == null) return NotFound();

        var model = new PatientPrintModel
        {
            CentreName = patient.Center.Name,
            PatientNumber = patient.PatientNumber,
            FullName = patient.FullName,
            Address = $"{patient.Address1} {patient.Address2}".Trim(),
            Contacts = patient.Contacts.Select(c => new PatientContactPrintRow(c.TelephoneNo, c.DateConfirmed, c.PersonChecked)).ToList(),
            Dob = patient.Dob,
            IdentificationType = patient.IdentificationType.ToString(),
            IdentificationNumber = patient.IdentificationNumber,
            Gender = patient.Sex.ToString(),
            Employment = patient.Employment,
            Province = patient.Province.Name,
            District = patient.District.Name,
            City = patient.City?.Name ?? patient.CityOther ?? "",
            Email = patient.Email,
            ReferralSource = patient.ReferralSource?.Name ?? patient.ReferralSourceOther ?? "",
            GuardianName = patient.GuardianName,
            GuardianRelationship = patient.GuardianRelationship,
            GuardianAddress = patient.GuardianAddress,
            GuardianPhone = patient.GuardianPhone,
            GuardianMobile = patient.GuardianMobile,
            TravelTimeDistance = patient.TravelTimeDistance,
            RegistrationDate = patient.RegistrationDate,
            RegistrationProcessedBy = patient.RegistrationProcessedBy
        };

        var pdf = _printFormService.GenerateRegistrationForm(model);
        return File(pdf, "application/pdf", $"RegistrationForm_{patient.PatientNumber}.pdf");
    }

    // GET: Print/AssessmentForm/{assessmentId}
    public async Task<IActionResult> AssessmentForm(Guid assessmentId)
    {
        var model = await BuildAssessmentPrintModel(assessmentId);
        if (model == null) return NotFound();

        var pdf = _printFormService.GenerateAssessmentForm(model);
        return File(pdf, "application/pdf", $"AssessmentForm_{model.PatientNumber}.pdf");
    }

    // GET: Print/PrescriptionForm/{assessmentId}
    public async Task<IActionResult> PrescriptionForm(Guid assessmentId)
    {
        var model = await BuildAssessmentPrintModel(assessmentId);
        if (model == null) return NotFound();

        var pdf = _printFormService.GeneratePrescriptionForm(model);
        return File(pdf, "application/pdf", $"PrescriptionForm_{model.PatientNumber}.pdf");
    }

    // GET: Print/DeliveryNote/{deliveryId}
    public async Task<IActionResult> DeliveryNote(Guid deliveryId)
    {
        var delivery = await _context.Deliveries
            .Include(d => d.Episode).ThenInclude(e => e.Patient)
            .Include(d => d.Device)
            .FirstOrDefaultAsync(d => d.Id == deliveryId);
        if (delivery == null) return NotFound();

        var model = new DeliveryPrintModel
        {
            PatientNumber = delivery.Episode.Patient.PatientNumber,
            PatientName = delivery.Episode.Patient.FullName,
            DeliveryDate = delivery.DeliveryDate,
            Notes = delivery.Notes,
            DeviceName = delivery.Device?.Name,
            CreatedBy = delivery.CreatedBy,
            CreatedAt = delivery.CreatedAt
        };

        var pdf = _printFormService.GenerateDeliveryNote(model);
        return File(pdf, "application/pdf", $"DeliveryNote_{model.PatientNumber}.pdf");
    }

    // GET: Print/FollowUpNote/{followUpId}
    public async Task<IActionResult> FollowUpNote(Guid followUpId)
    {
        var followUp = await _context.FollowUps
            .Include(f => f.Episode).ThenInclude(e => e.Patient)
            .FirstOrDefaultAsync(f => f.Id == followUpId);
        if (followUp == null) return NotFound();

        var model = new FollowUpPrintModel
        {
            PatientNumber = followUp.Episode.Patient.PatientNumber,
            PatientName = followUp.Episode.Patient.FullName,
            FollowUpDate = followUp.FollowUpDate,
            Notes = followUp.Notes,
            CreatedBy = followUp.CreatedBy,
            CreatedAt = followUp.CreatedAt
        };

        var pdf = _printFormService.GenerateFollowUpNote(model);
        return File(pdf, "application/pdf", $"FollowUpNote_{model.PatientNumber}.pdf");
    }

    private async Task<AssessmentPrintModel?> BuildAssessmentPrintModel(Guid assessmentId)
    {
        var assessment = await _context.Assessments
            .Include(a => a.Episode).ThenInclude(e => e.Patient)
            .Include(a => a.MainProblemType)
            .Include(a => a.CauseReasonType)
            .Include(a => a.Prescriptions)
            .FirstOrDefaultAsync(a => a.Id == assessmentId);
        if (assessment == null) return null;

        return new AssessmentPrintModel
        {
            PatientNumber = assessment.Episode.Patient.PatientNumber,
            PatientName = assessment.Episode.Patient.FullName,
            AssessedOn = assessment.AssessedOn,
            AssessmentType = assessment.AssessmentType.ToString(),
            LimbCategory = assessment.LimbCategory.ToString(),
            MainProblemType = assessment.MainProblemType.Name,
            Side = assessment.Side.ToString(),
            CauseReasonType = assessment.CauseReasonType.Name,
            CauseReasonOther = assessment.CauseReasonOther,
            AdditionalInformation = assessment.AdditionalInformation,
            Prescriptions = assessment.Prescriptions.Select(p =>
                new PrescriptionPrintRow(p.Side.ToString(), p.PrescriptionLabel, p.SubType, p.OtherText)).ToList()
        };
    }
}
