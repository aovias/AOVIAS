using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Orchestrates release lifecycle operations.
/// </summary>
public interface IReleaseManager
{
    Task<UploadPackageResult> UploadPackageAsync(UploadPackageRequest request, CancellationToken cancellationToken = default);

    Task<PackageValidationResult> ValidatePackageAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentPlan> GenerateDeploymentPlanAsync(PlanDeploymentRequest request, CancellationToken cancellationToken = default);

    Task<DeployReleaseResult> DeployAsync(DeployReleaseRequest request, CancellationToken cancellationToken = default);

    Task<RollbackReleaseResult> RollbackAsync(RollbackReleaseRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseListItem>> GetReleasesAsync(CancellationToken cancellationToken = default);

    Task<ReleaseRecord?> GetReleaseAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(
        ReleaseHistorySearchCriteria? criteria = null,
        CancellationToken cancellationToken = default);
}
