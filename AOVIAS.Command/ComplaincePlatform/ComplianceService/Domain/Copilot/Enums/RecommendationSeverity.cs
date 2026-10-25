namespace ComplianceService.Domain.Copilot.Enums;

/// <summary>
/// Urgency severity of a Copilot recommendation.
/// Drives ranking, visual indicators, and SLA for actioning.
/// </summary>
public enum RecommendationSeverity
{
    Low      = 0,
    Medium   = 1,
    High     = 2,
    Critical = 3,
}
