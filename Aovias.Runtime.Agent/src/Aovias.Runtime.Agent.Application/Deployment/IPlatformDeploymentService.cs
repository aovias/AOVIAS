using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;

namespace Aovias.Runtime.Agent.Application.Deployment;

/// <summary>
/// Orchestrates platform-level deployments through the Runtime Agent deployment queue.
/// </summary>
public interface IPlatformDeploymentService
{
    Task<PlatformDeploymentResponse> QueuePlatformDeploymentAsync(
        PlatformDeploymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PlatformDeploymentResponse> QueueSinglePackageDeployAsync(
        string packagePath,
        string runtime,
        string environment,
        string requestedBy,
        bool operatorConfirmed,
        CancellationToken cancellationToken = default);

    Task<DeploymentStatusResponse?> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetLogsAsync(string deploymentId, string? serviceId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default);

    Task<PlatformDeploymentReport?> GetReportAsync(string deploymentId, CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(CancelDeploymentRequest request, CancellationToken cancellationToken = default);

    Task<PlatformDeploymentResponse> RollbackAsync(PlatformRollbackRequest request, CancellationToken cancellationToken = default);

    Task<AgentRegistrationResponse> RegisterEnvironmentAsync(
        AgentRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<EnvironmentHealthSnapshot?> GetEnvironmentHealthAsync(
        string environmentId,
        CancellationToken cancellationToken = default);

    Task<EnvironmentDriftReport> DetectDriftAsync(string environmentId, CancellationToken cancellationToken = default);

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

    Task<PlatformCertificationReport?> GetCertificationAsync(string deploymentId, CancellationToken cancellationToken = default);

    Task<DeploymentReplayResult> ReplayDeploymentAsync(string deploymentId, string requestedBy, CancellationToken cancellationToken = default);

    Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChaosTestResult>> RunChaosTestsAsync(CancellationToken cancellationToken = default);

    Task<PlatformReconciliationReport> ReconcileAsync(CancellationToken cancellationToken = default);
}
