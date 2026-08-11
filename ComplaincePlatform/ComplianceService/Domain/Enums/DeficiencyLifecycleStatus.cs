namespace ComplianceService.Domain.Enums;

/// <summary>Sprint 8 — canonical deficiency lifecycle labels for UI and analytics.</summary>
public enum DeficiencyLifecycleStatus
{
    Identified = 0,
    Assigned = 1,
    InProgress = 2,
    AwaitingEvidence = 3,
    VerificationPending = 4,
    Verified = 5,
    Closed = 6,
    Cancelled = 7,
}
