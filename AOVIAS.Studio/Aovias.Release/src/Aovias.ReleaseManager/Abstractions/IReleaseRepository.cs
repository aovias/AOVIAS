using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Persists release records and package locations.
/// </summary>
public interface IReleaseRepository
{
    Task SaveAsync(ReleaseRecord record, CancellationToken cancellationToken = default);

    Task<ReleaseRecord?> GetAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task EnsureDirectoriesAsync(CancellationToken cancellationToken = default);
}
