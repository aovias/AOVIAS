namespace Aovias.Runtime.Agent.Application.Services;

/// <summary>
/// Reserved for future runtime snapshot orchestration services.
/// </summary>
public interface IRuntimeSnapshotService
{
    /// <summary>
    /// Refreshes the in-memory runtime snapshot from the manifest provider.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A task representing the refresh operation.</returns>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
