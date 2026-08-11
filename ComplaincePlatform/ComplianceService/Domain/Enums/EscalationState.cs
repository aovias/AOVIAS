namespace ComplianceService.Domain.Enums;

/// <summary>Sprint 10 — escalation business state (no delivery channel).</summary>
public enum EscalationState
{
    Pending = 0,
    Escalated = 1,
    Acknowledged = 2,
    Resolved = 3,
}
