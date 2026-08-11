using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;

namespace Aovias.Release.SDK;

/// <summary>
/// Client contract for the Remote Deployment Orchestrator API.
/// </summary>
public interface IDeploymentClient
{
    Task<PlatformDeploymentResponse> DeployPlatformAsync(
        PlatformDeploymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PlatformDeploymentResponse> UploadAndDeployPackageAsync(
        string packagePath,
        string runtime,
        string environment,
        string uploadedBy,
        bool operatorConfirmed = true,
        CancellationToken cancellationToken = default);

    Task<DeploymentStatusResponse?> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetLogsAsync(
        string deploymentId,
        string? serviceId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default);

    Task<PlatformDeploymentReport?> GetReportAsync(string deploymentId, CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(CancelDeploymentRequest request, CancellationToken cancellationToken = default);

    Task<PlatformDeploymentResponse> RollbackAsync(
        PlatformRollbackRequest request,
        CancellationToken cancellationToken = default);

    Task<AgentRegistrationResponse> RegisterEnvironmentAsync(
        AgentRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<EnvironmentHealthSnapshot?> GetEnvironmentHealthAsync(
        string environmentId,
        CancellationToken cancellationToken = default);

    Task<EnvironmentDriftReport> DetectDriftAsync(
        string environmentId,
        CancellationToken cancellationToken = default);

    Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(CancellationToken cancellationToken = default);

    Task<ChunkedUploadSession> InitiateChunkedUploadAsync(
        InitiateChunkedUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<ChunkedUploadSession> UploadChunkAsync(
        string uploadId,
        int chunkIndex,
        UploadChunkMetadata metadata,
        Stream chunkStream,
        CancellationToken cancellationToken = default);

    Task<ChunkedUploadSession> GetUploadSessionAsync(
        string uploadId,
        CancellationToken cancellationToken = default);

    Task<UploadPackageResult> CompleteChunkedUploadAsync(
        string uploadId,
        string runtime,
        string environment,
        string uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>Sprint M2.7: the lightweight fleet heartbeat — health/reconciliation/deployment summary, runtime version, capabilities, desired state version, last reconciliation timestamp.</summary>
    Task<RuntimeHeartbeatDto> GetHeartbeatAsync(CancellationToken cancellationToken = default);

    /// <summary>Sprint M2.7: triggers the same reconciliation cycle Sprint M2.6's <c>PlatformReconciliationService.ReconcileAsync</c> already exposes — the Command Center's "trigger reconciliation" API calls this, it does not invent a second reconciliation trigger path.</summary>
    Task<PlatformReconciliationReport> ReconcileAsync(CancellationToken cancellationToken = default);

    Task<MaintenanceModeStatus> GetMaintenanceModeAsync(CancellationToken cancellationToken = default);

    Task<MaintenanceModeStatus> SetMaintenanceModeAsync(MaintenanceModeRequest request, CancellationToken cancellationToken = default);

    Task<MaintenanceModeStatus> ResumeFromMaintenanceAsync(string resumedBy, CancellationToken cancellationToken = default);
}
