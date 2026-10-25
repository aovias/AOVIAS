using Aovias.Configuration.Settings;
using Aovias.PackageBuilder.Abstractions;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;
using Aovias.Release.SDK;
using ComplianceServiceManager.Contracts.Deployment;
using Microsoft.Extensions.Options;
using System.IO;

namespace ComplianceServiceManager.Services.Deployment;

/// <summary>
/// Deployment service adapter combining local package inspection and release SDK operations.
/// </summary>
public sealed class DeploymentService : IDeploymentService
{
    private readonly IPackageReader packageReader;
    private readonly IPackageValidator packageValidator;
    private readonly IReleaseClient releaseClient;
    private readonly IDeploymentRequestTraceService traceService;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;

    public DeploymentService(
        IPackageReader packageReader,
        IPackageValidator packageValidator,
        IReleaseClient releaseClient,
        IDeploymentRequestTraceService traceService,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.packageReader = packageReader;
        this.packageValidator = packageValidator;
        this.releaseClient = releaseClient;
        this.traceService = traceService;
        this.releaseSettings = releaseSettings;
        this.apiSettings = apiSettings;
    }

    /// <inheritdoc />
    public async Task<PackageContents?> LoadPackageAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
        {
            return null;
        }

        return await packageReader.ReadAsync(packagePath, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PackageValidationResult> ValidateLocalPackageAsync(string packagePath, CancellationToken cancellationToken = default)
        => packageValidator.ValidateAsync(packagePath, cancellationToken);

    /// <inheritdoc />
    public Task<UploadPackageResult> UploadPackageAsync(
        string packagePath,
        string runtime,
        string environment,
        string uploadedBy,
        CancellationToken cancellationToken = default)
        => releaseClient.UploadPackageAsync(packagePath, runtime, environment, uploadedBy, cancellationToken);

    /// <inheritdoc />
    public Task<PackageValidationResult> ValidateReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => releaseClient.ValidatePackageAsync(releaseId, cancellationToken);

    /// <inheritdoc />
    public Task<DeploymentPlan> GenerateDeploymentPlanAsync(string releaseId, string requestedBy, CancellationToken cancellationToken = default)
        => releaseClient.GenerateDeploymentPlanAsync(releaseId, requestedBy, cancellationToken);

    /// <inheritdoc />
    public Task<DeployReleaseResult> DeployAsync(
        string releaseId,
        string requestedBy,
        bool operatorConfirmed,
        CancellationToken cancellationToken = default)
    {
        traceService.TraceEndpointComparison();

        var request = new DeployReleaseRequest
        {
            ReleaseId = releaseId,
            RequestedBy = requestedBy,
            OperatorConfirmed = operatorConfirmed
        };

        traceService.TraceRequestBuilt(
            "POST",
            releaseSettings.CurrentValue.DeployEndpoint,
            nameof(DeployReleaseRequest),
            request,
            packagePath: null);

        traceService.TraceStage("PackageSelected", "Deploy using registered release id.", new
        {
            ReleaseId = releaseId,
            RuntimeUrl = apiSettings.CurrentValue.BaseUrl
        });

        return releaseClient.DeployAsync(releaseId, requestedBy, operatorConfirmed, cancellationToken);
    }

    /// <inheritdoc />
    public Task<RollbackReleaseResult> RollbackAsync(string releaseId, string requestedBy, CancellationToken cancellationToken = default)
        => releaseClient.RollbackAsync(releaseId, requestedBy, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseListItem>> GetReleasesAsync(CancellationToken cancellationToken = default)
        => releaseClient.GetReleasesAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default)
        => releaseClient.GetHistoryAsync(cancellationToken);

    /// <inheritdoc />
    public Task<ReleaseRecord?> GetReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => releaseClient.GetReleaseAsync(releaseId, cancellationToken);
}
