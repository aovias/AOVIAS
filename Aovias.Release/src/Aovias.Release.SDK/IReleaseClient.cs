using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;

namespace Aovias.Release.SDK;

/// <summary>
/// Client contract for the AOVIAS Release Management Platform API.
/// </summary>
public interface IReleaseClient
{
    Task<UploadPackageResult> UploadPackageAsync(
        string packagePath,
        string runtime,
        string environment,
        string uploadedBy,
        CancellationToken cancellationToken = default);

    Task<PackageValidationResult> ValidatePackageAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentPlan> GenerateDeploymentPlanAsync(
        string releaseId,
        string requestedBy,
        CancellationToken cancellationToken = default);

    Task<DeployReleaseResult> DeployAsync(
        string releaseId,
        string requestedBy,
        bool operatorConfirmed,
        CancellationToken cancellationToken = default);

    Task<RollbackReleaseResult> RollbackAsync(
        string releaseId,
        string requestedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseListItem>> GetReleasesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default);

    Task<ReleaseRecord?> GetReleaseAsync(string releaseId, CancellationToken cancellationToken = default);
}
