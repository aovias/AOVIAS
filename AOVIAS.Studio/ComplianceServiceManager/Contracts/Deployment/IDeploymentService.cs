using Aovias.PackageBuilder.Abstractions;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;
using Aovias.Release.SDK;

namespace ComplianceServiceManager.Contracts.Deployment;

/// <summary>
/// Deployment orchestration service for the Operations Console.
/// </summary>
public interface IDeploymentService
{
    Task<PackageContents?> LoadPackageAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<PackageValidationResult> ValidateLocalPackageAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<UploadPackageResult> UploadPackageAsync(
        string packagePath,
        string runtime,
        string environment,
        string uploadedBy,
        CancellationToken cancellationToken = default);

    Task<PackageValidationResult> ValidateReleaseAsync(string releaseId, CancellationToken cancellationToken = default);

    Task<DeploymentPlan> GenerateDeploymentPlanAsync(string releaseId, string requestedBy, CancellationToken cancellationToken = default);

    Task<DeployReleaseResult> DeployAsync(string releaseId, string requestedBy, bool operatorConfirmed, CancellationToken cancellationToken = default);

    Task<RollbackReleaseResult> RollbackAsync(string releaseId, string requestedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseListItem>> GetReleasesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default);

    Task<ReleaseRecord?> GetReleaseAsync(string releaseId, CancellationToken cancellationToken = default);
}
