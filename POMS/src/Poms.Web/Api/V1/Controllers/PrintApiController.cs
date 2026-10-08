using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Reporting.Models;
using Poms.Reporting.Services;
using Poms.Web.Api;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/print")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "AnyAuthenticatedUser")]
public sealed class PrintApiController(PomsDbContext context, IPrintFormService printForms, IRestrictedAccessService restrictedAccess) : ControllerBase
{
    [HttpGet("patients/{patientId:guid}/registration")]
    public async Task<IActionResult> Registration(Guid patientId, CancellationToken token)
    {
        var p = await context.Patients.Include(x => x.Province).Include(x => x.District).Include(x => x.City).Include(x => x.Center).Include(x => x.ReferralSource).Include(x => x.Contacts).SingleOrDefaultAsync(x => x.Id == patientId, token); if (p is null) return NotFound();
        var model = new PatientPrintModel { CentreName = p.Center.Name, PatientNumber = p.PatientNumber, FullName = p.FullName, Address = $"{p.Address1} {p.Address2}".Trim(), Contacts = p.Contacts.Select(x => new PatientContactPrintRow(x.TelephoneNo, x.DateConfirmed, x.PersonChecked)).ToList(), Dob = p.Dob, IdentificationType = p.IdentificationType.ToString(), IdentificationNumber = p.IdentificationNumber, Gender = p.Sex.ToString(), Employment = p.Employment, Province = p.Province.Name, District = p.District.Name, City = p.City?.Name ?? p.CityOther ?? "", Email = p.Email, ReferralSource = p.ReferralSource?.Name ?? p.ReferralSourceOther ?? "", ReferralPersonName = p.ReferralPersonName, ReferralPersonContactNumber = p.ReferralPersonContactNumber, AssignedClinicianName = p.AssignedClinicianName, GuardianName = p.GuardianName, GuardianRelationship = p.GuardianRelationship, GuardianAddress = p.GuardianAddress, GuardianPhone = p.GuardianPhone, GuardianMobile = p.GuardianMobile, TravelTimeDistance = p.TravelTimeDistance, RegistrationDate = p.RegistrationDate, RegistrationProcessedBy = p.RegistrationProcessedBy };
        return Pdf(printForms.GenerateRegistrationForm(model), $"RegistrationForm_{p.PatientNumber}.pdf", false);
    }

    [HttpGet("assessments/{assessmentId:guid}")]
    public async Task<IActionResult> Assessment(Guid assessmentId, CancellationToken token) { var model = await AssessmentModel(assessmentId, token); return model is null ? NotFound() : Pdf(printForms.GenerateAssessmentForm(model), $"AssessmentForm_{model.PatientNumber}.pdf", true); }

    [HttpGet("assessments/{assessmentId:guid}/prescription")]
    public async Task<IActionResult> Prescription(Guid assessmentId, CancellationToken token) { var model = await AssessmentModel(assessmentId, token); return model is null ? NotFound() : Pdf(printForms.GeneratePrescriptionForm(model), $"PrescriptionForm_{model.PatientNumber}.pdf", true); }

    [HttpGet("deliveries/{deliveryId:guid}")]
    public async Task<IActionResult> Delivery(Guid deliveryId, CancellationToken token)
    {
        var item = await context.Deliveries.Include(x => x.Episode).ThenInclude(x => x.Patient).Include(x => x.Device).SingleOrDefaultAsync(x => x.Id == deliveryId, token); if (item is null || !await Allowed(item.Episode, item, "ApiPrint")) return NotFound();
        var model = new DeliveryPrintModel { PatientNumber = item.Episode.Patient.PatientNumber, PatientName = item.Episode.Patient.FullName, DeliveryDate = item.DeliveryDate, DeliveryTime = item.DeliveryTime, Notes = item.Notes, DeviceName = item.Device?.Name, CreatedBy = item.CreatedBy, CreatedAt = item.CreatedAt };
        return Pdf(printForms.GenerateDeliveryNote(model), $"DeliveryNote_{model.PatientNumber}.pdf", true);
    }

    [HttpGet("follow-ups/{followUpId:guid}")]
    public async Task<IActionResult> FollowUp(Guid followUpId, CancellationToken token)
    {
        var item = await context.FollowUps.Include(x => x.Episode).ThenInclude(x => x.Patient).SingleOrDefaultAsync(x => x.Id == followUpId, token); if (item is null || !await Allowed(item.Episode, item, "ApiPrint")) return NotFound();
        var model = new FollowUpPrintModel { PatientNumber = item.Episode.Patient.PatientNumber, PatientName = item.Episode.Patient.FullName, FollowUpDate = item.FollowUpDate, StartTime = item.StartTime, EndTime = item.EndTime, Notes = item.Notes, CreatedBy = item.CreatedBy, CreatedAt = item.CreatedAt };
        return Pdf(printForms.GenerateFollowUpNote(model), $"FollowUpNote_{model.PatientNumber}.pdf", true);
    }

    private async Task<AssessmentPrintModel?> AssessmentModel(Guid id, CancellationToken token)
    {
        var item = await context.Assessments.Include(x => x.Episode).ThenInclude(x => x.Patient).Include(x => x.MainProblemType).Include(x => x.CauseReasonType).Include(x => x.Prescriptions).SingleOrDefaultAsync(x => x.Id == id, token); if (item is null || !await Allowed(item.Episode, item, "ApiPrint")) return null;
        return new AssessmentPrintModel { PatientNumber = item.Episode.Patient.PatientNumber, PatientName = item.Episode.Patient.FullName, AssessedOn = item.AssessedOn, StartTime = item.StartTime, EndTime = item.EndTime, AssessmentType = item.AssessmentType.ToString(), LimbCategory = item.LimbCategory.ToString(), MainProblemType = item.MainProblemType.Name, Side = item.Side.ToString(), CauseReasonType = item.CauseReasonType.Name, CauseReasonOther = item.CauseReasonOther, AdditionalInformation = item.AdditionalInformation, Prescriptions = item.Prescriptions.Select(x => new PrescriptionPrintRow(x.Side.ToString(), x.PrescriptionLabel, x.SubType, x.OtherText)).ToList() };
    }
    private async Task<bool> Allowed(Episode episode, Poms.Domain.Common.BaseEntity child, string action) { var access = await restrictedAccess.GetScopeAsync(User); var restricted = child switch { Assessment x => x.IsRestricted, Delivery x => x.IsRestricted, FollowUp x => x.IsRestricted, _ => false }; var allowed = access.CanAccess(episode.IsRestricted, episode.CreatedBy) && access.CanAccess(restricted, child.CreatedBy); await restrictedAccess.AuditAsync(access, allowed ? action : $"{action}Denied", child.GetType().Name, child.Id, episode.IsRestricted || restricted, allowed); return allowed; }
    private FileContentResult Pdf(byte[] bytes, string name, bool privateContent) { if (privateContent) Response.Headers.CacheControl = "no-store, private"; return File(bytes, "application/pdf", name); }
}
