using Poms.Reporting.Models;

namespace Poms.Reporting.Services;

public interface IPrintFormService
{
    byte[] GenerateRegistrationForm(PatientPrintModel model);
    byte[] GenerateAssessmentForm(AssessmentPrintModel model);
    byte[] GeneratePrescriptionForm(AssessmentPrintModel model);
    byte[] GenerateDeliveryNote(DeliveryPrintModel model);
    byte[] GenerateFollowUpNote(FollowUpPrintModel model);
    byte[] GenerateReportPdf(string title, string[] columnHeaders, IEnumerable<string[]> rows);
}
