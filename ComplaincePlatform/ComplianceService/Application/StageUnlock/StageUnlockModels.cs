using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.StageUnlock;

/// <summary>
/// Configurable completion criteria for a maturity stage (earned progression).
/// When all criteria pass, the stage is marked complete and the next stage unlocks.
/// </summary>
public sealed class StageUnlockCompletionCriteria
{
    public decimal MinimumReadinessPercent { get; init; }
    public int MaxOpenCriticalDeficiencies { get; init; }
    public decimal MinimumVerifiedObligationPercent { get; init; }
    public decimal MinimumEvidenceCoveragePercent { get; init; }
    public decimal MinimumVerificationSuccessPercent { get; init; }
}

/// <summary>
/// Default PRAMAN thresholds — extensible via <see cref="StageUnlockCriteriaRegistry"/>.
/// </summary>
public static class StageUnlockCriteriaDefaults
{
    public static StageUnlockCompletionCriteria ForStage(ObjectiveElementClassification stage) =>
        stage switch
        {
            ObjectiveElementClassification.Core => new StageUnlockCompletionCriteria
            {
                MinimumReadinessPercent = 70m,
                MaxOpenCriticalDeficiencies = 0,
                MinimumVerifiedObligationPercent = 60m,
                MinimumEvidenceCoveragePercent = 60m,
                MinimumVerificationSuccessPercent = 60m
            },
            ObjectiveElementClassification.Commitment => new StageUnlockCompletionCriteria
            {
                MinimumReadinessPercent = 75m,
                MaxOpenCriticalDeficiencies = 0,
                MinimumVerifiedObligationPercent = 70m,
                MinimumEvidenceCoveragePercent = 70m,
                MinimumVerificationSuccessPercent = 70m
            },
            ObjectiveElementClassification.Achievement => new StageUnlockCompletionCriteria
            {
                MinimumReadinessPercent = 85m,
                MaxOpenCriticalDeficiencies = 0,
                MinimumVerifiedObligationPercent = 80m,
                MinimumEvidenceCoveragePercent = 80m,
                MinimumVerificationSuccessPercent = 80m
            },
            ObjectiveElementClassification.Excellence => new StageUnlockCompletionCriteria
            {
                MinimumReadinessPercent = 90m,
                MaxOpenCriticalDeficiencies = 0,
                MinimumVerifiedObligationPercent = 90m,
                MinimumEvidenceCoveragePercent = 90m,
                MinimumVerificationSuccessPercent = 90m
            },
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };
}

public static class StageUnlockCriteriaRegistry
{
    private static readonly Dictionary<ObjectiveElementClassification, StageUnlockCompletionCriteria> Custom =
        new();

    public static StageUnlockCompletionCriteria Get(ObjectiveElementClassification stage) =>
        Custom.TryGetValue(stage, out var c) ? c : StageUnlockCriteriaDefaults.ForStage(stage);

    public static void Register(ObjectiveElementClassification stage, StageUnlockCompletionCriteria criteria) =>
        Custom[stage] = criteria;
}

public sealed class StageUnlockMetricsSnapshot
{
    public ObjectiveElementClassification Stage { get; init; }
    public int TotalObligations { get; init; }
    public decimal ReadinessPercent { get; init; }
    public int OpenCriticalDeficiencies { get; init; }
    public decimal VerifiedObligationPercent { get; init; }
    public decimal EvidenceCoveragePercent { get; init; }
    public decimal VerificationSuccessPercent { get; init; }
    public int VerifiedObligationCount { get; init; }
    public int MissingEvidenceCount { get; init; }
    public int PendingVerificationCount { get; init; }
}

public sealed class StageUnlockCriterionResult
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public decimal Actual { get; init; }
    public decimal Required { get; init; }
    public bool Passed { get; init; }
}

public sealed class StageUnlockEvaluationResult
{
    public ObjectiveElementClassification Stage { get; init; }
    public MaturityStageLifecycleState State { get; init; }
    public MaturityStageLifecycleState PreviousState { get; init; }
    public bool CriteriaMet { get; init; }
    public decimal ProgressPercent { get; init; }
    public decimal ReadinessPercent { get; init; }
    public decimal UnlockReadinessPercent { get; init; }
    public IReadOnlyList<StageUnlockCriterionResult> Criteria { get; init; } = Array.Empty<StageUnlockCriterionResult>();
    public IReadOnlyList<string> BlockingReasons { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public bool StateChanged { get; init; }
    /// <summary>Live obligation/deficiency metrics used for this evaluation (governance surfaces).</summary>
    public StageUnlockMetricsSnapshot? Metrics { get; init; }
}
