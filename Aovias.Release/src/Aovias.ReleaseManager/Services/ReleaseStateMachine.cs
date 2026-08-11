using Aovias.Release.Contracts.Enums;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Release lifecycle state machine.
/// </summary>
public sealed class ReleaseStateMachine : Abstractions.IReleaseStateMachine
{
    private static readonly Dictionary<ReleaseState, HashSet<ReleaseState>> AllowedTransitions =
        new()
        {
            [ReleaseState.Uploaded] = [ReleaseState.Validated, ReleaseState.Failed],
            [ReleaseState.Validated] = [ReleaseState.Compatible, ReleaseState.Failed],
            [ReleaseState.Compatible] = [ReleaseState.Staged, ReleaseState.Failed],
            [ReleaseState.Staged] = [ReleaseState.Deploying, ReleaseState.Failed, ReleaseState.Archived],
            [ReleaseState.Deploying] = [ReleaseState.HealthCheck, ReleaseState.Failed],
            [ReleaseState.HealthCheck] = [ReleaseState.Activated, ReleaseState.Failed],
            [ReleaseState.Activated] = [ReleaseState.Completed, ReleaseState.Failed],
            [ReleaseState.Completed] = [ReleaseState.Archived, ReleaseState.RollingBack],
            [ReleaseState.Failed] = [ReleaseState.Archived, ReleaseState.RollingBack],
            [ReleaseState.RollingBack] = [ReleaseState.RolledBack, ReleaseState.Failed],
            [ReleaseState.RolledBack] = [ReleaseState.Archived],
            [ReleaseState.Archived] = []
        };

    /// <inheritdoc />
    public ReleaseState Transition(ReleaseState currentState, ReleaseState targetState)
    {
        if (!CanTransition(currentState, targetState))
        {
            throw new InvalidOperationException($"Invalid release transition from {currentState} to {targetState}.");
        }

        return targetState;
    }

    /// <inheritdoc />
    public bool CanTransition(ReleaseState currentState, ReleaseState targetState)
        => AllowedTransitions.TryGetValue(currentState, out var targets) && targets.Contains(targetState);
}
