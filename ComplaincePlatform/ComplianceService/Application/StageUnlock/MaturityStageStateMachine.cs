using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.StageUnlock;

/// <summary>
/// Guards maturity stage lifecycle transitions. Stages unlock sequentially — no skipping.
/// </summary>
public static class MaturityStageStateMachine
{
    public static ObjectiveElementClassification? NextStage(ObjectiveElementClassification stage) =>
        stage switch
        {
            ObjectiveElementClassification.Core => ObjectiveElementClassification.Commitment,
            ObjectiveElementClassification.Commitment => ObjectiveElementClassification.Achievement,
            ObjectiveElementClassification.Achievement => ObjectiveElementClassification.Excellence,
            _ => null
        };

    public static ObjectiveElementClassification? PrerequisiteStage(ObjectiveElementClassification stage) =>
        stage switch
        {
            ObjectiveElementClassification.Commitment => ObjectiveElementClassification.Core,
            ObjectiveElementClassification.Achievement => ObjectiveElementClassification.Commitment,
            ObjectiveElementClassification.Excellence => ObjectiveElementClassification.Achievement,
            _ => null
        };

    public static bool CanTransition(MaturityStageLifecycleState from, MaturityStageLifecycleState to) =>
        (from, to) switch
        {
            (MaturityStageLifecycleState.Locked, MaturityStageLifecycleState.Available) => true,
            (MaturityStageLifecycleState.Available, MaturityStageLifecycleState.InProgress) => true,
            (MaturityStageLifecycleState.Available, MaturityStageLifecycleState.Completed) => true,
            (MaturityStageLifecycleState.InProgress, MaturityStageLifecycleState.Completed) => true,
            (MaturityStageLifecycleState.Completed, _) => false,
            (var f, var t) when f == t => true,
            _ => false
        };

    public static MaturityStageLifecycleState DeriveStageState(
        ObjectiveElementClassification stage,
        bool prerequisiteComplete,
        bool criteriaMet,
        decimal progressPercent)
    {
        if (stage == ObjectiveElementClassification.Core)
        {
            if (criteriaMet) return MaturityStageLifecycleState.Completed;
            return progressPercent > 0 ? MaturityStageLifecycleState.InProgress : MaturityStageLifecycleState.Available;
        }

        if (!prerequisiteComplete)
        {
            return MaturityStageLifecycleState.Locked;
        }

        if (criteriaMet)
        {
            return MaturityStageLifecycleState.Completed;
        }

        return progressPercent > 0
            ? MaturityStageLifecycleState.InProgress
            : MaturityStageLifecycleState.Available;
    }

    public static bool IsAccessible(MaturityStageLifecycleState state) =>
        state is MaturityStageLifecycleState.Available
            or MaturityStageLifecycleState.InProgress
            or MaturityStageLifecycleState.Completed;

    public static bool IsPreviewOnly(MaturityStageLifecycleState state) =>
        state == MaturityStageLifecycleState.Locked;
}
