// ============================================================================
// Poms.Domain/Enums/Enums.cs
// ============================================================================
namespace Poms.Domain.Enums;

public enum Sex { Male, Female, Other }
public enum Side { Left, Right, Bilateral }
public enum PatientCategory { Local, Foreign }
public enum IdentificationType { NIC, DrivingLicense, Passport }
public enum AssessmentType { Prosthetic, Orthotic }
public enum LimbCategory { UpperLimb, LowerLimb, Spinal }
public enum RecordStatus { Active, Completed, Cancelled }
public enum AppointmentType { Assessment, Fitting, Delivery }
public enum AppointmentStatus { Scheduled, Completed, Cancelled }

public enum DocumentType
{
    ScannedRegistrationForm,
    NicCopy,
    PassportCopy,
    DrivingLicenseCopy,
    MedicalDocuments,
    PrescriptionDocuments,
    AssessmentPhotos,
    DeliveryConfirmation,
    Other
}
