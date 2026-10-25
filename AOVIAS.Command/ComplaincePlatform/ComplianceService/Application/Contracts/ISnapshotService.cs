using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Service interface for retrieving compliance snapshots.
/// </summary>
public interface ISnapshotService
{
    /// <summary>
    /// Gets the latest compliance snapshot for a hospital by HospitalId.
    /// </summary>
    /// <param name="hospitalId">The unique identifier of the hospital.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compliance snapshot if found, null otherwise.</returns>
    Task<ComplianceSnapshot?> GetSnapshotByHospitalIdAsync(Guid hospitalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest compliance snapshot for a hospital by HospitalId, optionally filtered by FrameworkId.
    /// </summary>
    /// <param name="hospitalId">The unique identifier of the hospital.</param>
    /// <param name="frameworkId">Optional framework ID to filter by. If null, returns the latest snapshot for the hospital.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compliance snapshot if found, null otherwise.</returns>
    Task<ComplianceSnapshot?> GetSnapshotByHospitalIdAsync(Guid hospitalId, Guid? frameworkId, CancellationToken cancellationToken = default);
}
