using ComplianceService.Application.DTOs;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain.Enums;
using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;

namespace ComplianceService.Application.Maturity;

/// <summary>
/// Maps <see cref="StageUnlockEvaluationResult"/> to wire DTOs for Journey and Compliance Maturity surfaces.
/// Single presentation layer over <see cref="IStageUnlockingService"/> — avoids duplicate client thresholds.
/// </summary>
public static class MaturityStagePresentationMapper
{
    public static MaturityStage DetermineCurrentStage(
        IReadOnlyList<StageUnlockEvaluationResult> evaluations,
        decimal coreScore,
        decimal commitmentScore,
        decimal achievementScore)
    {
        if (evaluations.Count > 0)
        {
            var active = evaluations
                .Where(e => e.State is MaturityStageLifecycleState.InProgress or MaturityStageLifecycleState.Available)
                .OrderByDescending(e => (int)e.Stage)
                .FirstOrDefault();

            if (active != null)
            {
                return MapStage(active.Stage);
            }

            var lastCompleted = evaluations.LastOrDefault(e => e.CriteriaMet);
            if (lastCompleted != null)
            {
                var next = MaturityStageStateMachine.NextStage(lastCompleted.Stage);
                return next.HasValue ? MapStage(next.Value) : MaturityStage.Excellence;
            }
        }

        return DetermineLegacyCurrentStage(coreScore, commitmentScore, achievementScore);
    }

    public static IReadOnlyList<MaturityStageDto> BuildStages(
        IReadOnlyList<StageUnlockEvaluationResult> evaluations,
        decimal coreScore,
        decimal commitmentScore,
        decimal achievementScore,
        decimal excellenceScore)
    {
        if (evaluations.Count == 0)
        {
            return BuildLegacyStages(coreScore, commitmentScore, achievementScore, excellenceScore);
        }

        var scoreByStage = new Dictionary<OeClassification, decimal>
        {
            [OeClassification.Core] = coreScore,
            [OeClassification.Commitment] = commitmentScore,
            [OeClassification.Achievement] = achievementScore,
            [OeClassification.Excellence] = excellenceScore
        };

        return evaluations.Select(eval =>
        {
            var label = eval.Stage.ToString();
            var score = scoreByStage.TryGetValue(eval.Stage, out var s) ? s : eval.ReadinessPercent;
            var next = MaturityStageStateMachine.NextStage(eval.Stage);
            var threshold = next.HasValue
                ? StageUnlockCriteriaRegistry.Get(eval.Stage).MinimumReadinessPercent
                : 100m;

            return new MaturityStageDto
            {
                Stage = MapStage(eval.Stage),
                Label = label,
                Score = score,
                Status = MapLifecycleStatus(eval.State),
                ProgressPercent = eval.ProgressPercent,
                ReadinessPercent = eval.ReadinessPercent,
                UnlockReadinessPercent = eval.UnlockReadinessPercent,
                BlockingReasons = eval.BlockingReasons,
                Recommendations = eval.Recommendations,
                NextMilestone = eval.Recommendations.FirstOrDefault(),
                UnlockThreshold = threshold,
                UnlockMessage = eval.State == MaturityStageLifecycleState.Locked
                    ? eval.BlockingReasons.FirstOrDefault() ?? $"Complete prerequisite stage to unlock {label}."
                    : eval.CriteriaMet
                        ? $"{label} stage complete."
                        : $"Reach {threshold:F0}% readiness and verified evidence targets to unlock {next?.ToString() ?? "completion"}."
            };
        }).ToList();
    }

    /// <summary>Resolves stage scores from journey overview breakdown when available.</summary>
    public static (decimal Core, decimal Commitment, decimal Achievement, decimal Excellence) ScoresFromStageBreakdown(
        IReadOnlyList<StageProgressDto>? breakdown)
    {
        if (breakdown == null || breakdown.Count == 0)
        {
            return (0m, 0m, 0m, 0m);
        }

        decimal Score(string stage) =>
            breakdown.FirstOrDefault(b => string.Equals(b.Stage, stage, StringComparison.OrdinalIgnoreCase))?.Score ?? 0m;

        return (Score("core"), Score("commitment"), Score("achievement"), Score("excellence"));
    }

    private static MaturityStage DetermineLegacyCurrentStage(
        decimal coreScore,
        decimal commitmentScore,
        decimal achievementScore)
    {
        var coreThreshold = StageUnlockCriteriaRegistry.Get(OeClassification.Core).MinimumReadinessPercent;
        var commitmentThreshold = StageUnlockCriteriaRegistry.Get(OeClassification.Commitment).MinimumReadinessPercent;
        var achievementThreshold = StageUnlockCriteriaRegistry.Get(OeClassification.Achievement).MinimumReadinessPercent;

        if (coreScore >= coreThreshold)
        {
            if (commitmentScore >= commitmentThreshold)
            {
                return achievementScore >= achievementThreshold
                    ? MaturityStage.Excellence
                    : MaturityStage.Achievement;
            }

            return MaturityStage.Commitment;
        }

        return MaturityStage.Core;
    }

    private static IReadOnlyList<MaturityStageDto> BuildLegacyStages(
        decimal coreScore,
        decimal commitmentScore,
        decimal achievementScore,
        decimal excellenceScore)
    {
        var currentStage = DetermineLegacyCurrentStage(coreScore, commitmentScore, achievementScore);
        var coreThreshold = StageUnlockCriteriaRegistry.Get(OeClassification.Core).MinimumReadinessPercent;
        var commitmentThreshold = StageUnlockCriteriaRegistry.Get(OeClassification.Commitment).MinimumReadinessPercent;
        var achievementThreshold = StageUnlockCriteriaRegistry.Get(OeClassification.Achievement).MinimumReadinessPercent;

        return
        [
            new MaturityStageDto
            {
                Stage = MaturityStage.Core,
                Label = "Core",
                Score = coreScore,
                ReadinessPercent = coreScore,
                Status = coreScore >= coreThreshold ? MaturityStageStatus.Completed : MaturityStageStatus.InProgress,
                UnlockThreshold = coreThreshold
            },
            new MaturityStageDto
            {
                Stage = MaturityStage.Commitment,
                Label = "Commitment",
                Score = commitmentScore,
                ReadinessPercent = commitmentScore,
                Status = currentStage == MaturityStage.Core ? MaturityStageStatus.Locked : MaturityStageStatus.InProgress,
                UnlockThreshold = commitmentThreshold
            },
            new MaturityStageDto
            {
                Stage = MaturityStage.Achievement,
                Label = "Achievement",
                Score = achievementScore,
                ReadinessPercent = achievementScore,
                Status = currentStage < MaturityStage.Achievement ? MaturityStageStatus.Locked : MaturityStageStatus.InProgress,
                UnlockThreshold = achievementThreshold
            },
            new MaturityStageDto
            {
                Stage = MaturityStage.Excellence,
                Label = "Excellence",
                Score = excellenceScore,
                ReadinessPercent = excellenceScore,
                Status = currentStage < MaturityStage.Excellence ? MaturityStageStatus.Locked : MaturityStageStatus.InProgress,
                UnlockThreshold = 100m
            }
        ];
    }

    private static MaturityStageStatus MapLifecycleStatus(MaturityStageLifecycleState state) =>
        state switch
        {
            MaturityStageLifecycleState.Completed => MaturityStageStatus.Completed,
            MaturityStageLifecycleState.InProgress => MaturityStageStatus.InProgress,
            MaturityStageLifecycleState.Available => MaturityStageStatus.Available,
            _ => MaturityStageStatus.Locked
        };

    private static MaturityStage MapStage(OeClassification stage) =>
        stage switch
        {
            OeClassification.Commitment => MaturityStage.Commitment,
            OeClassification.Achievement => MaturityStage.Achievement,
            OeClassification.Excellence => MaturityStage.Excellence,
            _ => MaturityStage.Core
        };
}
