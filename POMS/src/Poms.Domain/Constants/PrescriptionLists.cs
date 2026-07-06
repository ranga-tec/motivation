// ============================================================================
// Poms.Domain/Constants/PrescriptionLists.cs
// ============================================================================
namespace Poms.Domain.Constants;

using Poms.Domain.Enums;

public record PrescriptionOption(string Code, string Label, string[]? SubTypes = null);

public static class PrescriptionLists
{
    public static readonly PrescriptionOption[] UpperLimbProsthetic =
    {
        new("FINGER_PROSTHESIS", "Finger Prosthesis"),
        new("PARTIAL_HAND_PROSTHESIS", "Partial Hand Prosthesis"),
        new("HAND_PROSTHESIS", "Hand Prosthesis"),
        new("WRIST_DISARTICULATION_PROSTHESIS", "Wrist Disarticulation Prosthesis"),
        new("TRANS_RADIAL_PROSTHESIS", "Trans-Radial Prosthesis"),
        new("ELBOW_DISARTICULATION_PROSTHESIS", "Elbow Disarticulation Prosthesis"),
        new("TRANS_HUMERAL_PROSTHESIS", "Trans-Humeral Prosthesis"),
        new("SHOULDER_DISARTICULATION_PROSTHESIS", "Shoulder Disarticulation Prosthesis"),
        new("FOREQUARTER_PROSTHESIS", "Forequarter Prosthesis"),
        new("OTHER", "Other"),
    };

    public static readonly PrescriptionOption[] LowerLimbProsthetic =
    {
        new("TOE_PROSTHESIS", "Toe Prosthesis"),
        new("PARTIAL_FOOT_PROSTHESIS_INSOLE", "Partial Foot Prosthesis, Insole Type"),
        new("PARTIAL_FOOT_PROSTHESIS_SLIPPER", "Partial Foot Prosthesis, Slipper Type"),
        new("PARTIAL_FOOT_PROSTHESIS_AFO", "Partial Foot Prosthesis, AFO Type"),
        new("ANKLE_DISARTICULATION_PROSTHESIS", "Ankle Disarticulation Prosthesis"),
        new("TRANS_TIBIAL_PROSTHESIS", "Trans-Tibial Prosthesis"),
        new("KNEE_DISARTICULATION_PROSTHESIS", "Knee Disarticulation Prosthesis"),
        new("TRANSFEMORAL_PROSTHESIS", "Transfemoral Prosthesis"),
        new("HIP_DISARTICULATION_PROSTHESIS", "Hip Disarticulation Prosthesis"),
        new("OTHER", "Other"),
    };

    public static readonly PrescriptionOption[] UpperLimbOrthotic =
    {
        new("FINGER_ORTHOSIS", "Finger Orthosis"),
        new("WRIST_HAND_ORTHOSIS", "Wrist Hand Orthosis, WHO"),
        new("ELBOW_ORTHOSIS", "Elbow Orthosis"),
        new("ELBOW_WRIST_HAND_ORTHOSIS", "Elbow Wrist Hand Orthosis"),
        new("SHOULDER_ORTHOSIS", "Shoulder Orthosis"),
        new("SHOULDER_ELBOW_WRIST_HAND_ORTHOSIS", "Shoulder Elbow Wrist Hand Orthosis"),
        new("OTHER", "Other"),
    };

    public static readonly PrescriptionOption[] LowerLimbOrthotic =
    {
        new("SOFT_INSOLE_CASTED", "Soft Insole, Casted"),
        new("INSOLE_WITH_FOOT_PRINT", "Insole with Foot Print"),
        new("RIGID_INSOLE", "Rigid Insole"),
        new("AFO", "Ankle Foot Orthosis, AFO", new[]
        {
            "Supra Malleolus Orthosis, SMO", "Flexible AFO", "Rigid AFO", "Jointed AFO"
        }),
        new("KO", "Knee Orthosis, KO", new[] { "KO with Side Bars", "KO Splint Type" }),
        new("KAFO", "Knee Ankle Foot Orthosis, KAFO", new[]
        {
            "KAFO with Side Bars", "KAFO Splint Type"
        }),
        new("HKAFO", "Hip Knee Ankle Foot Orthosis, HKAFO"),
        new("HIP_ABDUCTION_BRACE", "Hip Abduction Brace", new[]
        {
            "Pavlik Harness", "Soft Hip Abduction Brace", "Rigid Hip Abduction Brace"
        }),
        new("OTHER", "Other"),
    };

    // Spinal: no confirmed dropdown values yet (PRD 11.4) - Other/Type free text only.
    public static readonly PrescriptionOption[] SpinalOrthotic =
    {
        new("OTHER", "Other / Type (free text)"),
    };

    public static PrescriptionOption[] For(AssessmentType type, LimbCategory limb) =>
        (type, limb) switch
        {
            (AssessmentType.Prosthetic, LimbCategory.UpperLimb) => UpperLimbProsthetic,
            (AssessmentType.Prosthetic, LimbCategory.LowerLimb) => LowerLimbProsthetic,
            (AssessmentType.Orthotic, LimbCategory.UpperLimb) => UpperLimbOrthotic,
            (AssessmentType.Orthotic, LimbCategory.LowerLimb) => LowerLimbOrthotic,
            (AssessmentType.Orthotic, LimbCategory.Spinal) => SpinalOrthotic,
            _ => throw new ArgumentOutOfRangeException(nameof(limb), "Invalid AssessmentType/LimbCategory combination"),
        };
}
