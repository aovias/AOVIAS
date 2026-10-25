using Aovias.Publishing.Contracts.ReleaseLibrary;

namespace Aovias.Publishing.Core.ReleaseLibrary;

/// <summary>
/// Validates lifecycle transitions (Sprint M1.5 D2/D10). The state machine is
/// forward-only along the canonical order, with two explicit side branches
/// (Deprecated, Archived) reachable from any "in service" state.
/// </summary>
public static class ArtifactLifecycleTransitions
{
    private static readonly ArtifactLifecycleState[] ForwardOrder =
    [
        ArtifactLifecycleState.Building,
        ArtifactLifecycleState.Published,
        ArtifactLifecycleState.Verified,
        ArtifactLifecycleState.Certified,
        ArtifactLifecycleState.Ready,
        ArtifactLifecycleState.Deployed,
        ArtifactLifecycleState.Promoted
    ];

    public static bool IsValidTransition(ArtifactLifecycleState from, ArtifactLifecycleState to)
    {
        if (from == to)
        {
            return false;
        }

        // Deprecated and Archived are reachable from any in-service state, and
        // Archived is reachable from Deprecated. Neither is a source for any
        // further transition except Archived being terminal.
        if (to == ArtifactLifecycleState.Archived)
        {
            return from != ArtifactLifecycleState.Archived;
        }

        if (to == ArtifactLifecycleState.Deprecated)
        {
            return from is not (ArtifactLifecycleState.Deprecated or ArtifactLifecycleState.Archived);
        }

        if (from is ArtifactLifecycleState.Deprecated or ArtifactLifecycleState.Archived)
        {
            return false;
        }

        var fromIndex = Array.IndexOf(ForwardOrder, from);
        var toIndex = Array.IndexOf(ForwardOrder, to);
        if (fromIndex < 0 || toIndex < 0)
        {
            return false;
        }

        // Forward-only, but allows skipping ahead (e.g. Published -> Certified when
        // an intermediate stage was skipped) — never allows moving backward.
        return toIndex > fromIndex;
    }

    public static void EnsureValidTransition(ArtifactLifecycleState from, ArtifactLifecycleState to)
    {
        if (!IsValidTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Invalid Release Library lifecycle transition: '{from}' -> '{to}'.");
        }
    }
}
