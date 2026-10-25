using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.StageUnlock;

/// <summary>
/// Evaluates configurable unlock criteria against live obligation/deficiency/evidence metrics.
/// </summary>
public static class StageUnlockCriteriaEvaluator
{
    public static StageUnlockMetricsSnapshot BuildMetrics(
        ObjectiveElementClassification stage,
        IReadOnlyList<OrganizationComplianceObligation> stageObligations,
        int openCriticalDeficiencies)
    {
        var total = stageObligations.Count;
        if (total == 0)
        {
            return new StageUnlockMetricsSnapshot
            {
                Stage = stage,
                TotalObligations = 0,
                ReadinessPercent = 0,
                OpenCriticalDeficiencies = openCriticalDeficiencies
            };
        }

        var verified = stageObligations.Count(o => o.VerificationStatus == VerificationStatus.Verified);
        var withEvidence = stageObligations.Count(o => o.EvidenceStatus != EvidenceStatus.Missing);
        var pendingVerification = stageObligations.Count(o =>
            o.VerificationStatus == VerificationStatus.Pending
            || o.EvidenceStatus is EvidenceStatus.Submitted or EvidenceStatus.Partial);

        var readiness = ObligationReadinessCalculator.CalculateStageReadiness(stageObligations, stage);
        var verifiedPct = Percent(verified, total);
        var evidencePct = Percent(withEvidence, total);
        var verificationPct = withEvidence == 0 ? 0m : Percent(verified, withEvidence);

        return new StageUnlockMetricsSnapshot
        {
            Stage = stage,
            TotalObligations = total,
            ReadinessPercent = readiness,
            OpenCriticalDeficiencies = openCriticalDeficiencies,
            VerifiedObligationPercent = verifiedPct,
            EvidenceCoveragePercent = evidencePct,
            VerificationSuccessPercent = verificationPct,
            VerifiedObligationCount = verified,
            MissingEvidenceCount = total - withEvidence,
            PendingVerificationCount = pendingVerification
        };
    }

    public static StageUnlockEvaluationResult Evaluate(
        ObjectiveElementClassification stage,
        StageUnlockMetricsSnapshot metrics,
        StageUnlockCompletionCriteria criteria,
        MaturityStageLifecycleState previousState,
        bool prerequisiteComplete)
    {
        var criterionResults = new List<StageUnlockCriterionResult>
        {
            Criterion("readiness", "Stage Readiness", metrics.ReadinessPercent, criteria.MinimumReadinessPercent),
            Criterion("criticalDeficiencies", "Critical Deficiencies",
                metrics.OpenCriticalDeficiencies, criteria.MaxOpenCriticalDeficiencies, invert: true),
            Criterion("verifiedObligations", "Verified Obligations",
                metrics.VerifiedObligationPercent, criteria.MinimumVerifiedObligationPercent),
            Criterion("evidenceCoverage", "Evidence Coverage",
                metrics.EvidenceCoveragePercent, criteria.MinimumEvidenceCoveragePercent),
            Criterion("verificationSuccess", "Verification Success Rate",
                metrics.VerificationSuccessPercent, criteria.MinimumVerificationSuccessPercent)
        };

        var criteriaMet = criterionResults.All(c => c.Passed);
        var unlockReadiness = criterionResults.Count == 0
            ? 0m
            : Math.Round(criterionResults.Average(c => Ratio(c.Actual, c.Required, c.Key == "criticalDeficiencies")) * 100m, 2);

        var progress = criteriaMet
            ? 100m
            : unlockReadiness;

        var state = MaturityStageStateMachine.DeriveStageState(stage, prerequisiteComplete, criteriaMet, progress);

        var blocking = criterionResults
            .Where(c => !c.Passed)
            .Select(c => $"{c.Label}: {FormatActual(c)} (required: {FormatRequired(c)})")
            .ToList();

        if (!prerequisiteComplete && stage != ObjectiveElementClassification.Core)
        {
            var prereq = MaturityStageStateMachine.PrerequisiteStage(stage);
            blocking.Insert(0, $"Complete {prereq} stage before progressing to {stage}.");
        }

        var recommendations = BuildRecommendations(stage, metrics, criterionResults);

        return new StageUnlockEvaluationResult
        {
            Stage = stage,
            State = state,
            PreviousState = previousState,
            CriteriaMet = criteriaMet,
            ProgressPercent = progress,
            ReadinessPercent = metrics.ReadinessPercent,
            UnlockReadinessPercent = unlockReadiness,
            Criteria = criterionResults,
            BlockingReasons = blocking,
            Recommendations = recommendations,
            StateChanged = state != previousState,
            Metrics = metrics
        };
    }

    private static List<string> BuildRecommendations(
        ObjectiveElementClassification stage,
        StageUnlockMetricsSnapshot metrics,
        IReadOnlyList<StageUnlockCriterionResult> criteria)
    {
        var recs = new List<string>();
        var next = MaturityStageStateMachine.NextStage(stage);
        var target = next?.ToString() ?? "Excellence";

        var critical = criteria.FirstOrDefault(c => c.Key == "criticalDeficiencies");
        if (critical is { Passed: false } && metrics.OpenCriticalDeficiencies > 0)
        {
            recs.Add($"Close {metrics.OpenCriticalDeficiencies} critical {stage} deficiencies to unlock {target}.");
        }

        if (metrics.MissingEvidenceCount > 0)
        {
            recs.Add($"Upload evidence for {metrics.MissingEvidenceCount} missing {stage} obligations.");
        }

        if (metrics.PendingVerificationCount > 0)
        {
            recs.Add($"Verify {metrics.PendingVerificationCount} pending evidence records.");
        }

        var verified = criteria.FirstOrDefault(c => c.Key == "verifiedObligations");
        if (verified is { Passed: false } && metrics.TotalObligations > 0)
        {
            var need = (int)Math.Ceiling(
                metrics.TotalObligations * (verified.Required / 100m) - metrics.VerifiedObligationCount);
            if (need > 0)
            {
                recs.Add($"Verify {need} more {stage} obligations to reach {verified.Required:F0}% verified.");
            }
        }

        var readiness = criteria.FirstOrDefault(c => c.Key == "readiness");
        if (readiness is { Passed: false })
        {
            recs.Add($"Increase {stage} readiness from {metrics.ReadinessPercent:F0}% to {readiness.Required:F0}%.");
        }

        return recs;
    }

    private static StageUnlockCriterionResult Criterion(
        string key,
        string label,
        decimal actual,
        decimal required,
        bool invert = false)
    {
        var passed = invert ? actual <= required : actual >= required;
        return new StageUnlockCriterionResult
        {
            Key = key,
            Label = label,
            Actual = actual,
            Required = required,
            Passed = passed
        };
    }

    private static decimal Ratio(decimal actual, decimal required, bool invertCritical)
    {
        if (invertCritical)
        {
            return required == 0 && actual == 0 ? 1m : actual <= required ? 1m : Math.Max(0m, 1m - (actual - required) / Math.Max(1m, actual));
        }

        return required <= 0 ? 1m : Math.Min(1m, actual / required);
    }

    private static decimal Percent(int part, int total) =>
        total == 0 ? 0m : Math.Round(100m * part / total, 2);

    private static string FormatActual(StageUnlockCriterionResult c) =>
        c.Key == "criticalDeficiencies" ? $"{c.Actual:F0}" : $"{c.Actual:F1}%";

    private static string FormatRequired(StageUnlockCriterionResult c) =>
        c.Key == "criticalDeficiencies" ? $"{c.Required:F0}" : $"{c.Required:F0}%";
}
