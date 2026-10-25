using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Application.StageUnlock;
using Xunit;

namespace ComplianceService.Tests;

public sealed class MaturityStageStateMachineTests
{
    [Fact]
    public void NextStage_Sequential_NoSkipping()
    {
        Assert.Equal(ObjectiveElementClassification.Commitment,
            MaturityStageStateMachine.NextStage(ObjectiveElementClassification.Core));
        Assert.Equal(ObjectiveElementClassification.Achievement,
            MaturityStageStateMachine.NextStage(ObjectiveElementClassification.Commitment));
        Assert.Null(MaturityStageStateMachine.NextStage(ObjectiveElementClassification.Excellence));
    }

    [Fact]
    public void IsAccessible_AllowsAvailableInProgressCompleted()
    {
        Assert.True(MaturityStageStateMachine.IsAccessible(MaturityStageLifecycleState.Available));
        Assert.True(MaturityStageStateMachine.IsAccessible(MaturityStageLifecycleState.InProgress));
        Assert.True(MaturityStageStateMachine.IsAccessible(MaturityStageLifecycleState.Completed));
        Assert.False(MaturityStageStateMachine.IsAccessible(MaturityStageLifecycleState.Locked));
    }

    [Fact]
    public void CanTransition_CompletedIsTerminal()
    {
        Assert.False(MaturityStageStateMachine.CanTransition(
            MaturityStageLifecycleState.Completed,
            MaturityStageLifecycleState.InProgress));
    }
}
