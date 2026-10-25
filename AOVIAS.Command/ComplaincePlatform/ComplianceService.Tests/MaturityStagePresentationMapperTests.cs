using ComplianceService.Application.DTOs;
using ComplianceService.Application.Maturity;
using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Xunit;
using OeClassification = ComplianceService.Domain.ObjectiveElementClassification;

namespace ComplianceService.Tests;

public sealed class MaturityStagePresentationMapperTests
{
    [Fact]
    public void BuildStages_UsesUnlockEngineState_NotClientThresholds()
    {
        var evaluations = new List<StageUnlockEvaluationResult>
        {
            new()
            {
                Stage = OeClassification.Core,
                State = MaturityStageLifecycleState.InProgress,
                ReadinessPercent = 82m,
                ProgressPercent = 82m,
                CriteriaMet = false
            },
            new()
            {
                Stage = OeClassification.Commitment,
                State = MaturityStageLifecycleState.Locked,
                ReadinessPercent = 40m,
                BlockingReasons = ["Complete Core stage first."]
            }
        };

        var stages = MaturityStagePresentationMapper.BuildStages(
            evaluations, 82m, 40m, 0m, 0m);

        Assert.Equal(2, stages.Count);
        Assert.Equal(MaturityStageStatus.InProgress, stages[0].Status);
        Assert.Equal(82m, stages[0].ReadinessPercent);
        Assert.Equal(MaturityStageStatus.Locked, stages[1].Status);
        Assert.Contains("Complete Core stage first.", stages[1].BlockingReasons);
    }

    [Fact]
    public void DetermineCurrentStage_PrefersUnlockEngineActiveStage()
    {
        var evaluations = new List<StageUnlockEvaluationResult>
        {
            new()
            {
                Stage = OeClassification.Core,
                State = MaturityStageLifecycleState.InProgress,
                CriteriaMet = false
            }
        };

        var current = MaturityStagePresentationMapper.DetermineCurrentStage(
            evaluations, 90m, 90m, 90m);

        Assert.Equal(MaturityStage.Core, current);
    }
}
