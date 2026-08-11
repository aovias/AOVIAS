namespace ComplianceService.Domain.Enums;

/// <summary>Sprint 10 — deficiency SLA clock state.</summary>
public enum SlaStatus
{
    OnTrack = 0,
    Warning = 1,
    AtRisk = 2,
    Breached = 3,
    Closed = 4,
}
