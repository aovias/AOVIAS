using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Static helper that produces consistent frontend deep-link URLs for Copilot recommendations.
/// Links are relative to the frontend root and use the Angular router structure.
/// </summary>
public static class DeepLinkBuilder
{
    // ── Compliance Domain ────────────────────────────────────────────────────
    public const string DeficiencyCommandCenter = "/compliance/deficiency-command-center";
    public const string DeficiencyCommandCenterCritical = "/compliance/deficiency-command-center?filter=critical";
    public const string DeficiencyCommandCenterRecurring = "/compliance/deficiency-command-center?filter=recurring";
    public const string DeficiencyCommandCenterEscalated = "/compliance/deficiency-command-center?filter=escalated";
    public const string DeficiencyCommandCenterAudit = "/compliance/deficiency-command-center?filter=audit";
    public const string DeficiencyCommandCenterCapaOverdue = "/compliance/deficiency-command-center?tab=capa&filter=overdue";
    public const string DeficiencyCommandCenterRca = "/compliance/deficiency-command-center?tab=rca";
    public const string DeficiencyCommandCenterSlat = "/compliance/deficiency-command-center?filter=sla-at-risk";

    // ── Evidence ─────────────────────────────────────────────────────────────
    public const string Evidence = "/compliance/evidence";
    public const string EvidenceMissing = "/compliance/evidence?filter=missing";
    public const string EvidencePendingReview = "/compliance/evidence?filter=pending-review";
    public const string EvidencePending = "/compliance/evidence?filter=pending";

    // ── Maturity / Stage Unlock ───────────────────────────────────────────────
    public const string Maturity = "/compliance/maturity";

    // ── Analytics ────────────────────────────────────────────────────────────
    public const string AnalyticsReadiness = "/analytics/readiness";
    public const string AnalyticsExecutive = "/analytics/executive";
    public const string AnalyticsDepartments = "/analytics/departments";

    // ── Helpers ───────────────────────────────────────────────────────────────
    public static string ForCategory(RecommendationCategory category) => category switch
    {
        RecommendationCategory.Accreditation => Maturity,
        RecommendationCategory.Deficiency    => DeficiencyCommandCenter,
        RecommendationCategory.CAPA          => DeficiencyCommandCenter,
        RecommendationCategory.SLA           => DeficiencyCommandCenterSlat,
        RecommendationCategory.Escalation    => DeficiencyCommandCenterEscalated,
        RecommendationCategory.Evidence      => Evidence,
        RecommendationCategory.Verification  => EvidencePendingReview,
        RecommendationCategory.Executive     => AnalyticsExecutive,
        RecommendationCategory.Department    => AnalyticsDepartments,
        RecommendationCategory.Risk          => AnalyticsExecutive,
        RecommendationCategory.Analytics     => AnalyticsReadiness,
        _                                    => "/dashboard",
    };
}
