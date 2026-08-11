using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Architecture-only rollback orchestration service.
/// </summary>
public interface IReleaseRollbackService
{
    Task<RollbackReleaseResult> RollbackAsync(RollbackReleaseRequest request, ReleaseRecord release, CancellationToken cancellationToken = default);
}
