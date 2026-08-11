using Aovias.Release.Contracts.Enums;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Applies release state machine transitions.
/// </summary>
public interface IReleaseStateMachine
{
    ReleaseState Transition(ReleaseState currentState, ReleaseState targetState);

    bool CanTransition(ReleaseState currentState, ReleaseState targetState);
}
