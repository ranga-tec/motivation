namespace Poms.Reporting.Models;

public record PatientContactPrintRow(string TelephoneNo, DateOnly? DateConfirmed, string? PersonChecked);

public class PatientPrintModel
{
    public string CentreName { get; set; } = "";
    public string PatientNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Address { get; set; } = "";
    public List<PatientContactPrintRow> Contacts { get; set; } = new();
    public DateOnly Dob { get; set; }
    public string IdentificationType { get; set; } = "";
    public string IdentificationNumber { get; set; } = "";
    public string Gender { get; set; } = "";
    public string? Employment { get; set; }
    public string Province { get; set; } = "";
    public string District { get; set; } = "";
    public string City { get; set; } = "";
    public string? Email { get; set; }
    public string ReferralSource { get; set; } = "";
    public string? ReferralPersonName { get; set; }
    public string? ReferralPersonContactNumber { get; set; }
    public string? AssignedClinicianName { get; set; }
    public string GuardianName { get; set; } = "";
    public string GuardianRelationship { get; set; } = "";
    public string? GuardianAddress { get; set; }
    public string? GuardianPhone { get; set; }
    public string? GuardianMobile { get; set; }
    public string? TravelTimeDistance { get; set; }
    public DateOnly RegistrationDate { get; set; }
    public string RegistrationProcessedBy { get; set; } = "";
}

public record PrescriptionPrintRow(string Side, string Label, string? SubType, string? OtherText);

public class AssessmentPrintModel
{
    public string PatientNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateOnly AssessedOn { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string AssessmentType { get; set; } = "";
    public string LimbCategory { get; set; } = "";
    public string MainProblemType { get; set; } = "";
    public string Side { get; set; } = "";
    public string CauseReasonType { get; set; } = "";
    public string? CauseReasonOther { get; set; }
    public string? AdditionalInformation { get; set; }
    public List<PrescriptionPrintRow> Prescriptions { get; set; } = new();
}

public class DeliveryPrintModel
{
    public string PatientNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateOnly DeliveryDate { get; set; }
    public TimeOnly? DeliveryTime { get; set; }
    public string? Notes { get; set; }
    public string? DeviceName { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FollowUpPrintModel
{
    public string PatientNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateOnly FollowUpDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
