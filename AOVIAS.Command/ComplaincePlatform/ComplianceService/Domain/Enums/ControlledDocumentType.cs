namespace ComplianceService.Domain.Enums;

/// <summary>
/// Controlled document classification — extensible for future regulatory types.
/// </summary>
public enum ControlledDocumentType
{
    Policy = 0,
    Sop = 1,
    Manual = 2,
    Guideline = 3,
    Protocol = 4,
    Procedure = 5,
    Checklist = 6,
    Form = 7,
    Template = 8,
    CommitteeDocument = 9,
    ExternalReference = 10,
}
