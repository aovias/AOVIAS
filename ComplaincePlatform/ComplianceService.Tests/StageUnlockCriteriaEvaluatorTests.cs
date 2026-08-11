using ComplianceService.Application.StageUnlock;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class StageUnlockCriteriaEvaluatorTests
{
    [Fact]
    public void Evaluate_CoreComplete_WhenAllCriteriaMet()
    {
        var metrics = new StageUnlockMetricsSnapshot
        {
            Stage = ObjectiveElementClassification.Core,
            TotalObligations = 100,
            ReadinessPercent = 75m,
            OpenCriticalDeficiencies = 0,
            VerifiedObligationPercent = 65m,
            EvidenceCoveragePercent = 70m,
            VerificationSuccessPercent = 62m,
            VerifiedObligationCount = 65
        };

        var criteria = StageUnlockCriteriaDefaults.ForStage(ObjectiveElementClassification.Core);
        var result = StageUnlockCriteriaEvaluator.Evaluate(
            ObjectiveElementClassification.Core,
            metrics,
            criteria,
            MaturityStageLifecycleState.InProgress,
            prerequisiteComplete: true);

        Assert.True(result.CriteriaMet);
        Assert.Equal(MaturityStageLifecycleState.Completed, result.State);
    }

    [Fact]
    public void Evaluate_Blocked_WhenCriticalDeficienciesExist()
    {
        var metrics = new StageUnlockMetricsSnapshot
        {
            Stage = ObjectiveElementClassification.Core,
            TotalObligations = 10,
            ReadinessPercent = 90m,
            OpenCriticalDeficiencies = 2,
            VerifiedObligationPercent = 90m,
            EvidenceCoveragePercent = 90m,
            VerificationSuccessPercent = 90m
        };

        var criteria = StageUnlockCriteriaDefaults.ForStage(ObjectiveElementClassification.Core);
        var result = StageUnlockCriteriaEvaluator.Evaluate(
            ObjectiveElementClassification.Core,
            metrics,
            criteria,
            MaturityStageLifecycleState.InProgress,
            prerequisiteComplete: true);

        Assert.False(result.CriteriaMet);
        Assert.Contains(result.BlockingReasons, r => r.Contains("Critical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_CommitmentLocked_WhenPrerequisiteIncomplete()
    {
        var metrics = new StageUnlockMetricsSnapshot
        {
            Stage = ObjectiveElementClassification.Commitment,
            TotalObligations = 5,
            ReadinessPercent = 10m,
            OpenCriticalDeficiencies = 0,
            VerifiedObligationPercent = 0m,
            EvidenceCoveragePercent = 0m,
            VerificationSuccessPercent = 0m
        };

        var criteria = StageUnlockCriteriaDefaults.ForStage(ObjectiveElementClassification.Commitment);
        var result = StageUnlockCriteriaEvaluator.Evaluate(
            ObjectiveElementClassification.Commitment,
            metrics,
            criteria,
            MaturityStageLifecycleState.Locked,
            prerequisiteComplete: false);

        Assert.Equal(MaturityStageLifecycleState.Locked, result.State);
        Assert.Contains(result.BlockingReasons, r => r.Contains("Core", StringComparison.OrdinalIgnoreCase));
    }
}
