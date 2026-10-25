namespace ComplianceService.Domain.Enums;

/// <summary>Sprint 7B / Phase 2 — provenance of a compliance deficiency (CAPA SSOT).</summary>
public enum DeficiencyOriginType
{
    EvaluationEngine = 0,
    Manual = 1,
    AuditFinding = 2,
    Incident = 3,
    Risk = 4,
    Complaint = 5,
    ObjectiveElement = 6,
    PolicyDocument = 7,
    Training = 8,
}
