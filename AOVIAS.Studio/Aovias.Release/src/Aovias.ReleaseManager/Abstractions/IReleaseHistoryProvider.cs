using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Provides release history views.
/// </summary>
public interface IReleaseHistoryProvider
{
    Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(
        ReleaseHistorySearchCriteria? criteria = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEvent>> GetDeploymentDetailAsync(
        string deploymentId,
        CancellationToken cancellationToken = default);
}
