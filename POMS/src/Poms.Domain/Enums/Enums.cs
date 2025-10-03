// ============================================================================
// Poms.Domain/Enums/Enums.cs
// ============================================================================
namespace Poms.Domain.Enums;

public enum EpisodeType { Prosthetic, Orthotic, SpinalOrthosis }
public enum Sex { Male, Female, Other }
public enum BodyRegion { UpperLimb, LowerLimb, Spine, Other }
public enum Side { Left, Right, Bilateral, NotApplicable }
public enum ConditionType { Primary, Secondary }
public enum AmputationType { BelowKnee, AboveKnee, BelowElbow, AboveElbow, PartialHand, PartialFoot, Other }
public enum Reason { Disease, Trauma, Vascular, Diabetic, Cancer, Congenital, Other }
public enum RepairCategory { FootRep, SocketRep, LinerRep, BraceRep, JointRep, Other }
