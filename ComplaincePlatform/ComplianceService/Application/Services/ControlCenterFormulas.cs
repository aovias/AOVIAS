namespace ComplianceService.Application.Services;

/// <summary>
/// Pure, deterministic Control Center scoring formulas (risk scoring + audit confidence). Extracted
/// from <see cref="ControlCenterAggregationService"/> so they can be unit-tested in isolation.
/// </summary>
public static class ControlCenterFormulas
{
    public const decimal DefaultAuditThreshold = 80m;

    /// <summary>
    /// Chapter risk (0-100): 0.40*(100-compliance) + 0.20*findings + 0.20*overdue + 0.10*missing + 0.10*auditFailures.
    /// Per-signal components are normalized and capped at 100 before weighting.
    /// </summary>
    public static decimal ChapterRiskScore(decimal compliancePct, int openFindings, int overdueTasks, int missingEvidence, int auditFailures)
    {
        var gap = Math.Clamp(100m - compliancePct, 0m, 100m);
        var findingsNorm = Math.Min(100m, openFindings * 20m);
        var overdueNorm = Math.Min(100m, overdueTasks * 15m);
        var missingNorm = Math.Min(100m, missingEvidence * 10m);
        var failuresNorm = Math.Min(100m, auditFailures * 25m);
        return Math.Clamp(
            0.40m * gap + 0.20m * findingsNorm + 0.20m * overdueNorm + 0.10m * missingNorm + 0.10m * failuresNorm,
            0m, 100m);
    }

    /// <summary>Risk bands: Low &lt;25, Medium &lt;50, High &lt;75, Critical &gt;=75 mapped to UI low/medium/high.</summary>
    public static string ChapterRiskBandToUi(decimal risk)
        => risk >= 50 ? "high" : risk >= 25 ? "medium" : "low";

    /// <summary>Department bands tuned to weighted roll-up: low &lt;40, medium &lt;70, high &gt;=70.</summary>
    public static string DepartmentRiskBandToUi(decimal risk)
        => risk >= 70 ? "high" : risk >= 40 ? "medium" : "low";

    /// <summary>
    /// Audit confidence (0-100): clamp(100 - 6*criticalNC - 3*openFindings - 4*overdueCapa
    /// - 1.5*missingEvidenceClauses + 0.3*evidenceCoverage% + 0.4*avgChapterReadiness%).
    /// </summary>
    public static decimal AuditConfidence(
        decimal avgChapterReadiness, int criticalNc, int openFindings, int overdueCapa, int missingClauses, decimal evidenceCoverage)
        => Math.Clamp(
            100m
            - 6m * criticalNc
            - 3m * openFindings
            - 4m * overdueCapa
            - 1.5m * missingClauses
            + 0.3m * evidenceCoverage
            + 0.4m * avgChapterReadiness,
            0m, 100m);

    /// <summary>Logistic pass probability of confidence relative to the audit threshold (steepness k=10).</summary>
    public static decimal AuditPassProbability(decimal confidence, decimal threshold)
        => Math.Round(100m / (1m + (decimal)Math.Exp((double)(-(confidence - threshold) / 10m))), 2, MidpointRounding.AwayFromZero);

    public static string ConfidenceStatus(decimal confidence)
        => confidence >= 85 ? "Audit Ready"
            : confidence >= 70 ? "On Track"
            : confidence >= 50 ? "Needs Attention"
            : "At Risk";
}
