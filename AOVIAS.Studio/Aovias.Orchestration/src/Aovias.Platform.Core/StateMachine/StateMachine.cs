namespace Aovias.Platform.Core.StateMachine;

/// <summary>
/// Generic finite state machine for platform workflows.
/// </summary>
public sealed class StateMachine<TState>
    where TState : struct, Enum
{
    private readonly Dictionary<TState, HashSet<TState>> transitions;

    public StateMachine(IReadOnlyDictionary<TState, IReadOnlyCollection<TState>> allowedTransitions)
    {
        transitions = allowedTransitions.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToHashSet());
    }

    public bool CanTransition(TState current, TState target)
        => transitions.TryGetValue(current, out var allowed) && allowed.Contains(target);

    public TState Transition(TState current, TState target)
    {
        if (!CanTransition(current, target))
        {
            throw new InvalidOperationException($"Invalid transition from {current} to {target}.");
        }

        return target;
    }
}
