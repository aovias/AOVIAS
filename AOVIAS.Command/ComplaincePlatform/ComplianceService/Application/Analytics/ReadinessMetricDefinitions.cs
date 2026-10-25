namespace ComplianceService.Application.Analytics;

/// <summary>
/// Readiness calculation authority (Sprint 12B-0 SSOT).
/// </summary>
public static class ReadinessMetricDefinitions
{
    /// <summary>
    /// Headline accreditation readiness for a journey — authoritative for Journey UI and executive views.
    /// Source: <see cref="Contracts.IComplianceScoreService.GetJourneyScoreAsync"/>.
    /// </summary>
    public const string JourneyAccreditationReadiness = "IComplianceScoreService.GetJourneyScoreAsync";

    /// <summary>
    /// Framework-level readiness from assessment scoring — used in Compliance Maturity core summary.
    /// Source: <see cref="Contracts.IComplianceScoringService.CalculateFrameworkScoreAsync"/>.
    /// </summary>
    public const string FrameworkAssessmentReadiness = "IComplianceScoringService.CalculateFrameworkScoreAsync";

    /// <summary>
    /// Per-requirement readiness projection — SSOT for obligation-level state.
    /// Source: <see cref="Contracts.IObligationQueryService.GetSummaryAsync"/>.
    /// </summary>
    public const string ObligationReadiness = "OrganizationComplianceObligations via IObligationQueryService";

    /// <summary>
    /// Historical readiness trend — append-only time series.
    /// Source: <c>ComplianceSnapshots.OverallScore</c> / <c>AuditConfidenceScore</c>.
    /// </summary>
    public const string HistoricalReadinessTrend = "ComplianceSnapshots";

    /// <summary>
    /// Stage unlock readiness percent — band-filtered obligation readiness for maturity stages.
    /// Source: <see cref="StageUnlock.StageUnlockingService"/>.
    /// </summary>
    public const string StageUnlockReadiness = "StageUnlockingService.StageUnlockMetricsSnapshot";
}
