namespace ComplianceService.Domain.Enums;

/// <summary>
/// Enterprise risk register lifecycle status (Phase 4 Sprint 4).
/// Legacy Open→Identified, Mitigated→Mitigating; Closed remapped to 5 in migration.
/// </summary>
public enum RiskStatus : int
{
    Identified = 1,
    Assessed = 2,
    Mitigating = 3,
    UnderReview = 4,
    Closed = 5,
}

public static class RiskStatusExtensions
{
    /// <summary>Active register risks (anything not Closed).</summary>
    public static bool IsActive(this RiskStatus status) => status != RiskStatus.Closed;

    public static bool CanTransitionTo(this RiskStatus from, RiskStatus to)
    {
        if (from == to) return true;
        if (from == RiskStatus.Closed) return false;

        return (from, to) switch
        {
            (RiskStatus.Identified, RiskStatus.Assessed) => true,
            (RiskStatus.Identified, RiskStatus.UnderReview) => true,
            (RiskStatus.Identified, RiskStatus.Closed) => true,
            (RiskStatus.Assessed, RiskStatus.Mitigating) => true,
            (RiskStatus.Assessed, RiskStatus.UnderReview) => true,
            (RiskStatus.Assessed, RiskStatus.Closed) => true,
            (RiskStatus.Mitigating, RiskStatus.UnderReview) => true,
            (RiskStatus.Mitigating, RiskStatus.Assessed) => true,
            (RiskStatus.Mitigating, RiskStatus.Closed) => true,
            (RiskStatus.UnderReview, RiskStatus.Mitigating) => true,
            (RiskStatus.UnderReview, RiskStatus.Assessed) => true,
            (RiskStatus.UnderReview, RiskStatus.Closed) => true,
            _ => false,
        };
    }
}
