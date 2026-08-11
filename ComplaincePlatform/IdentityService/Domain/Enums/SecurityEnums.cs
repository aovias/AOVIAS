namespace IdentityService.Domain.Enums;

public enum RiskLevel { Low, Medium, High, Critical }

public enum SecurityEventSeverity { Info, Warning, High, Critical }

public enum DeviceVerificationStatus { Pending, Verified, Revoked }

public enum SecurityIncidentStatus { Open, Investigating, Resolved, Dismissed }

public enum AdaptiveAuthDecision { Allow, StepUpMfa, Block }

public enum SessionHistoryAction
{
    Created,
    Refreshed,
    Revoked,
    RiskChanged,
    MarkedSuspicious
}
