namespace Aovias.Bootstrap.Contracts;

/// <summary>Runs every <see cref="BootstrapPhaseName"/> phase, in fixed order, against one target, producing the single <see cref="BootstrapReport"/> for the attempt.</summary>
public interface IBootstrapOrchestrator
{
    /// <param name="request">The bootstrap request.</param>
    /// <param name="observer">Optional live execution signals (see <see cref="IBootstrapExecutionObserver"/>) — omit for the previous fire-and-await-the-report behavior, unchanged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BootstrapReport> BootstrapAsync(BootstrapRequest request, IBootstrapExecutionObserver? observer = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Persists which <see cref="BootstrapPhaseName"/> phases have already completed for a given
/// target (keyed by <see cref="BootstrapTarget.StableIdentity"/>), so a re-run is idempotent (an
/// already-satisfied phase is not repeated) and a run interrupted partway through is resumable
/// (it continues from the first not-yet-completed phase rather than starting over).
/// </summary>
public interface IBootstrapStateStore
{
    Task<IReadOnlySet<BootstrapPhaseName>> GetCompletedPhasesAsync(string targetIdentity, CancellationToken cancellationToken);

    Task MarkCompletedAsync(string targetIdentity, BootstrapPhaseName phase, CancellationToken cancellationToken);

    Task ResetAsync(string targetIdentity, CancellationToken cancellationToken);
}
