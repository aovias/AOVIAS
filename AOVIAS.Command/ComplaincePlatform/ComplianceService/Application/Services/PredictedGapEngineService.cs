using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Services;

/// <summary>
/// Rules-based engine that generates predicted gap items for Commitment, Achievement,
/// and Excellence stages based on the current maturity scores and actual gap categories.
///
/// No AI is used. Predictions are derived from NABH framework chapter mappings, risk
/// categories, and static rule tables that mirror the maturity progression thresholds
/// defined in <c>MaturityProgressionService</c>:
///   Core       → Commitment  : CoreScore ≥ 70
///   Commitment → Achievement : CommitmentScore ≥ 75
///   Achievement→ Excellence  : AchievementScore ≥ 85
/// </summary>
public sealed class PredictedGapEngineService
{
    // ── Static rule tables ────────────────────────────────────────────────────

    private static readonly IReadOnlyList<PredictedGapItemDto> CommitmentRules =
    [
        new() { Title = "Staff Training Matrix",          Chapter = "HRM",     RiskCategory = "People" },
        new() { Title = "Committee Effectiveness Review", Chapter = "MOM",     RiskCategory = "Governance" },
        new() { Title = "SOP Version Control System",     Chapter = "DMS",     RiskCategory = "Process" },
        new() { Title = "Competency Assessment Program",  Chapter = "HRM",     RiskCategory = "People" },
        new() { Title = "Departmental Orientation Plan",  Chapter = "HRM",     RiskCategory = "People" },
        new() { Title = "Meeting Documentation Audit",    Chapter = "MOM",     RiskCategory = "Governance" },
    ];

    private static readonly IReadOnlyList<PredictedGapItemDto> AchievementRules =
    [
        new() { Title = "Internal Audit Program",         Chapter = "QMS",     RiskCategory = "Quality" },
        new() { Title = "KPI Monitoring Dashboard",       Chapter = "QMS",     RiskCategory = "Quality" },
        new() { Title = "Quality Indicators Framework",   Chapter = "CQI",     RiskCategory = "Quality" },
        new() { Title = "Department-Level Assessments",   Chapter = "CQI",     RiskCategory = "Quality" },
        new() { Title = "Patient Satisfaction Surveys",   Chapter = "PFC",     RiskCategory = "Patient" },
    ];

    private static readonly IReadOnlyList<PredictedGapItemDto> ExcellenceRules =
    [
        new() { Title = "Digital Transformation Roadmap", Chapter = "IT",      RiskCategory = "Technology" },
        new() { Title = "Benchmarking Against Peers",     Chapter = "QMS",     RiskCategory = "Quality" },
        new() { Title = "Research & Innovation Program",  Chapter = "APEX",    RiskCategory = "Innovation" },
        new() { Title = "Nursing Excellence Framework",   Chapter = "NRS",     RiskCategory = "Clinical" },
        new() { Title = "APEX Accreditation Readiness",   Chapter = "APEX",    RiskCategory = "Accreditation" },
    ];

    // ── Thresholds (aligned with MaturityProgressionService) ─────────────────

    private const decimal CommitmentUnlockThreshold = 70m;
    private const decimal AchievementUnlockThreshold = 75m;
    private const decimal ExcellenceUnlockThreshold  = 85m;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Result containing predicted gap summaries for all three upper stages.
    /// </summary>
    public sealed class PredictionResult
    {
        public PredictedGapSummaryDto Commitment  { get; init; } = new();
        public PredictedGapSummaryDto Achievement { get; init; } = new();
        public PredictedGapSummaryDto Excellence  { get; init; } = new();
    }

    /// <summary>
    /// Generates predicted gaps for Commitment, Achievement, and Excellence stages.
    /// </summary>
    /// <param name="coreScore">Actual core readiness score (0–100).</param>
    /// <param name="commitmentScore">Actual/estimated commitment score (0–100).</param>
    /// <param name="achievementScore">Actual/estimated achievement score (0–100).</param>
    public PredictionResult GeneratePredictions(
        decimal coreScore,
        decimal commitmentScore,
        decimal achievementScore)
    {
        var commitment  = BuildCommitmentSummary(coreScore);
        var achievement = BuildAchievementSummary(commitmentScore);
        var excellence  = BuildExcellenceSummary(achievementScore);

        return new PredictionResult
        {
            Commitment  = commitment,
            Achievement = achievement,
            Excellence  = excellence,
        };
    }

    // ── Private builders ──────────────────────────────────────────────────────

    private static PredictedGapSummaryDto BuildCommitmentSummary(decimal coreScore)
    {
        var isLocked = coreScore < CommitmentUnlockThreshold;

        var items = isLocked
            ? CommitmentRules
            : FilterByScore(CommitmentRules, coreScore, CommitmentUnlockThreshold);

        return new PredictedGapSummaryDto
        {
            IsLocked          = isLocked,
            PredictedGapCount = items.Count,
            UnlockCriteria    = $"Complete Core remediation to a score of {CommitmentUnlockThreshold}% to unlock Commitment Assessment. Current Core score: {coreScore:F0}%.",
            Items             = items,
        };
    }

    private static PredictedGapSummaryDto BuildAchievementSummary(decimal commitmentScore)
    {
        var isLocked = commitmentScore < AchievementUnlockThreshold;

        var items = isLocked
            ? AchievementRules
            : FilterByScore(AchievementRules, commitmentScore, AchievementUnlockThreshold);

        return new PredictedGapSummaryDto
        {
            IsLocked          = isLocked,
            PredictedGapCount = items.Count,
            UnlockCriteria    = $"Commitment score must reach {AchievementUnlockThreshold}% to unlock Achievement Assessment. Current Commitment score: {commitmentScore:F0}%.",
            Items             = items,
        };
    }

    private static PredictedGapSummaryDto BuildExcellenceSummary(decimal achievementScore)
    {
        var isLocked = achievementScore < ExcellenceUnlockThreshold;

        var items = isLocked
            ? ExcellenceRules
            : FilterByScore(ExcellenceRules, achievementScore, ExcellenceUnlockThreshold);

        return new PredictedGapSummaryDto
        {
            IsLocked          = isLocked,
            PredictedGapCount = items.Count,
            UnlockCriteria    = $"Achievement score must reach {ExcellenceUnlockThreshold}% to unlock Excellence Assessment. Current Achievement score: {achievementScore:F0}%.",
            Items             = items,
        };
    }

    /// <summary>
    /// As the score approaches the threshold the number of predicted gaps decreases
    /// (more gaps resolved → fewer remaining predictions). Uses proportional filtering.
    /// </summary>
    private static IReadOnlyList<PredictedGapItemDto> FilterByScore(
        IReadOnlyList<PredictedGapItemDto> rules,
        decimal score,
        decimal threshold)
    {
        // Score has already met/exceeded threshold; show all as remaining work.
        if (rules.Count == 0) return Array.Empty<PredictedGapItemDto>();

        // The fraction of the threshold not yet met drives how many predictions remain.
        var fraction = score >= threshold
            ? 1.0m
            : score / threshold;

        // Show a decreasing subset — at score=0 show all, at score=threshold show ~30%.
        var showCount = (int)Math.Ceiling(rules.Count * (1m - fraction * 0.7m));
        showCount = Math.Max(1, Math.Min(showCount, rules.Count));

        return rules.Take(showCount).ToList();
    }
}
