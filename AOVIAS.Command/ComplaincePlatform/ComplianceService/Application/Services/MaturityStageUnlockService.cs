using ComplianceService.Application.Contracts;
using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;

namespace ComplianceService.Application.Services;

/// <summary>
/// Evaluates whether a maturity stage has been unlocked based on live OE scores.
///
/// Unlock thresholds (aligned with ComplianceMaturityAggregationService):
///   Core       ≥ 70% → unlocks Commitment
///   Commitment ≥ 75% → unlocks Achievement
///   Achievement ≥ 85% → unlocks Excellence
/// </summary>
public sealed class MaturityStageUnlockService
{
    private readonly IComplianceScoreService _scoreService;
    private readonly ILogger<MaturityStageUnlockService> _logger;

    // Mirror of thresholds in ComplianceMaturityAggregationService
    public const decimal CoreUnlockThreshold       = 70m;
    public const decimal CommitmentUnlockThreshold = 75m;
    public const decimal AchievementUnlockThreshold = 85m;

    public MaturityStageUnlockService(
        IComplianceScoreService scoreService,
        ILogger<MaturityStageUnlockService> logger)
    {
        _scoreService = scoreService;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates unlock status for all four maturity stages for the given journey.
    /// Returns a <see cref="StageUnlockResult"/> for each stage.
    /// </summary>
    public async Task<IReadOnlyList<StageUnlockResult>> EvaluateAllStagesAsync(
        Guid journeyId,
        CancellationToken cancellationToken = default)
    {
        if (journeyId == Guid.Empty) return Array.Empty<StageUnlockResult>();

        try
        {
            var coreScore = await _scoreService.GetStageScoreAsync(journeyId, OeClassification.Core, cancellationToken);
            var commitmentScore = await _scoreService.GetStageScoreAsync(journeyId, OeClassification.Commitment, cancellationToken);
            var achievementScore = await _scoreService.GetStageScoreAsync(journeyId, OeClassification.Achievement, cancellationToken);
            var excellenceScore = await _scoreService.GetStageScoreAsync(journeyId, OeClassification.Excellence, cancellationToken);

            return new[]
            {
                new StageUnlockResult(OeClassification.Core,       coreScore,        null,                        true),
                new StageUnlockResult(OeClassification.Commitment,  commitmentScore,  CoreUnlockThreshold,         coreScore       >= CoreUnlockThreshold),
                new StageUnlockResult(OeClassification.Achievement, achievementScore, CommitmentUnlockThreshold,   commitmentScore  >= CommitmentUnlockThreshold),
                new StageUnlockResult(OeClassification.Excellence,  excellenceScore,  AchievementUnlockThreshold,  achievementScore >= AchievementUnlockThreshold),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stage unlock evaluation failed for JourneyId={JourneyId}", journeyId);
            return Array.Empty<StageUnlockResult>();
        }
    }

    /// <summary>
    /// Evaluates unlock status for the next stage after OE wizard completion.
    /// Returns null if no stage transition is triggered.
    /// </summary>
    public async Task<StageUnlockResult?> EvaluatePostCompletionAsync(
        Guid journeyId,
        OeClassification completedOeStage,
        CancellationToken cancellationToken = default)
    {
        if (journeyId == Guid.Empty) return null;

        var score = await _scoreService.GetStageScoreAsync(journeyId, completedOeStage, cancellationToken);
        var threshold = completedOeStage switch
        {
            OeClassification.Core       => CoreUnlockThreshold,
            OeClassification.Commitment => CommitmentUnlockThreshold,
            OeClassification.Achievement => AchievementUnlockThreshold,
            _                            => (decimal?)null,
        };

        if (threshold == null) return null;

        var unlocked = score >= threshold.Value;

        if (unlocked)
        {
            _logger.LogInformation(
                "Stage unlock threshold met: Stage={Stage}, Score={Score:F1}%, Threshold={Threshold}%, JourneyId={JourneyId}",
                completedOeStage, score, threshold, journeyId);
        }

        return new StageUnlockResult(completedOeStage, score, threshold.Value, unlocked);
    }
}

/// <summary>
/// Result of a maturity stage unlock evaluation for a single stage.
/// </summary>
/// <param name="Stage">The stage evaluated.</param>
/// <param name="Score">Current 0-100 compliance score for this stage.</param>
/// <param name="Threshold">Unlock threshold for the NEXT stage (null for Excellence/Core).</param>
/// <param name="IsUnlocked">True when the score meets or exceeds the threshold.</param>
public sealed record StageUnlockResult(
    OeClassification Stage,
    decimal Score,
    decimal? Threshold,
    bool IsUnlocked);
