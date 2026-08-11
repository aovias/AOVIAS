namespace ComplianceService.Domain.Enums;

/// <summary>
/// Controlled document lifecycle states (Sprint 15 QDMS).
/// Draft → UnderReview → Approved → Published → Effective → ReviewDue → Superseded → Archived
/// </summary>
public enum ControlledDocumentStatus
{
    Draft = 0,
    UnderReview = 1,
    Approved = 2,
    Published = 3,
    Effective = 4,
    ReviewDue = 5,
    Superseded = 6,
    Archived = 7,
}
