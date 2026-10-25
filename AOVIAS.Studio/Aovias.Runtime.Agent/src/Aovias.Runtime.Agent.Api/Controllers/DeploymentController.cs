using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Api.Diagnostics;
using Aovias.Runtime.Agent.Application.Deployment;
using Aovias.Runtime.Agent.Infrastructure.Admission;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Api.Controllers;

/// <summary>
/// Remote Deployment Orchestrator API executed by the Runtime Agent.
/// </summary>
[ApiController]
[Route("api/deployment")]
[Produces("application/json")]
public sealed class DeploymentController : ControllerBase
{
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings;
    private readonly ILogger<DeploymentController> logger;

    public DeploymentController(
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings,
        ILogger<DeploymentController> logger)
    {
        this.releaseSettings = releaseSettings;
        this.deploymentSettings = deploymentSettings;
        this.logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(16777216)]
    public async Task<IActionResult> UploadAsync(
        IFormFile package,
        [FromForm] string runtime,
        [FromForm] string environment,
        [FromForm] string uploadedBy,
        [FromServices] IPlatformDeploymentService deploymentService,
        [FromForm] bool operatorConfirmed = true,
        CancellationToken cancellationToken = default)
    {
        if (package is null || package.Length == 0)
        {
            logger.LogWarning("DeploymentController upload rejected: package file missing.");
            return DeploymentBoundaryDiagnostics.ValidationError(
                "Package file is required.",
                "Select a package before deploying.",
                field: "package",
                endpoint: "/api/deployment/upload",
                expectedRequestType: "multipart/form-data with package file");
        }

        if (package.Length > deploymentSettings.CurrentValue.UploadChunkSizeBytes)
        {
            return DeploymentBoundaryDiagnostics.ValidationError(
                $"Single-request upload rejected: package is {package.Length} bytes.",
                "Use /api/deployment/upload/initiate and resumable chunk upload for packages larger than one configured chunk.",
                field: "package",
                endpoint: "/api/deployment/upload",
                expectedRequestType: "resumable chunk upload");
        }

        var extension = Path.GetExtension(package.FileName);
        if (!string.Equals(extension, PackageLayout.Extension, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".aovdeploy", StringComparison.OrdinalIgnoreCase))
        {
            return DeploymentBoundaryDiagnostics.ValidationError(
                "Package must use the .aovpkg or .aovdeploy extension.",
                "Publish a valid AOVIAS deployment package.",
                field: "package",
                endpoint: "/api/deployment/upload",
                expectedRequestType: "multipart/form-data with package file");
        }

        var settings = releaseSettings.CurrentValue;
        DeploymentExecutionDiagnostics.CreateDirectory(
            settings.TemporaryFolder,
            nameof(DeploymentController),
            nameof(UploadAsync));
        var tempPath = Path.Combine(settings.TemporaryFolder, $"{Guid.NewGuid():N}{extension}");

        await using (var stream = System.IO.File.Create(tempPath))
        {
            await package.CopyToAsync(stream, cancellationToken);
        }

        var result = await deploymentService.QueueSinglePackageDeployAsync(
            tempPath,
            runtime,
            environment,
            uploadedBy,
            operatorConfirmed,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("deploy")]
    public Task<IActionResult> DeployAsync(
        IFormFile package,
        [FromForm] string runtime,
        [FromForm] string environment,
        [FromForm] string uploadedBy,
        [FromServices] IPlatformDeploymentService deploymentService,
        [FromForm] bool operatorConfirmed = true,
        CancellationToken cancellationToken = default)
        => UploadAsync(package, runtime, environment, uploadedBy, deploymentService, operatorConfirmed, cancellationToken);

    [HttpPost("platform")]
    public async Task<IActionResult> DeployPlatformAsync(
        [FromBody] PlatformDeploymentRequest request,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.QueuePlatformDeploymentAsync(request, cancellationToken));

    [HttpPost("rollback")]
    public async Task<IActionResult> RollbackAsync(
        [FromBody] PlatformRollbackRequest request,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.RollbackAsync(request, cancellationToken));

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelAsync(
        [FromBody] CancelDeploymentRequest request,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(new { cancelled = await deploymentService.CancelAsync(request, cancellationToken) });

    [HttpGet("status/{deploymentId}")]
    public async Task<IActionResult> GetStatusAsync(
        string deploymentId,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        var status = await deploymentService.GetStatusAsync(deploymentId, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpGet("logs/{deploymentId}")]
    public async Task<IActionResult> GetLogsAsync(
        string deploymentId,
        [FromQuery] string? serviceId,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.GetLogsAsync(deploymentId, serviceId, cancellationToken));

    [HttpGet("history")]
    public async Task<IActionResult> GetHistoryAsync(
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.GetHistoryAsync(cancellationToken));

    [HttpGet("report/{deploymentId}")]
    public async Task<IActionResult> GetReportAsync(
        string deploymentId,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        var report = await deploymentService.GetReportAsync(deploymentId, cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] AgentRegistrationRequest request,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.RegisterEnvironmentAsync(request, cancellationToken));

    [HttpGet("health/{environmentId}")]
    public async Task<IActionResult> GetEnvironmentHealthAsync(
        string environmentId,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        var health = await deploymentService.GetEnvironmentHealthAsync(environmentId, cancellationToken);
        return health is null ? NotFound() : Ok(health);
    }

    [HttpGet("drift/{environmentId}")]
    public async Task<IActionResult> GetDriftAsync(
        string environmentId,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.DetectDriftAsync(environmentId, cancellationToken));

    [HttpPost("upload/initiate")]
    public async Task<IActionResult> InitiateChunkedUploadAsync(
        [FromBody] InitiateChunkedUploadRequest request,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Upload initiation requested DeploymentId={DeploymentId} ServiceId={ServiceId} Package={Package} SizeBytes={SizeBytes} ChunkSizeBytes={ChunkSizeBytes} Checksum={Checksum}",
            request.DeploymentId,
            request.ServiceId,
            request.FileName,
            request.TotalBytes,
            request.ChunkSizeBytes,
            request.Checksum);
        var session = await deploymentService.InitiateChunkedUploadAsync(request, cancellationToken);
        logger.LogInformation(
            "Upload session accepted DeploymentId={DeploymentId} UploadId={UploadId} ReceivedChunks={ReceivedChunks} TotalChunks={TotalChunks}",
            session.DeploymentId,
            session.UploadId,
            session.ReceivedChunks,
            session.TotalChunks);
        return Ok(session);
    }

    [HttpPost("upload/{uploadId}/chunk/{chunkIndex}")]
    [RequestSizeLimit(16777216)]
    public async Task<IActionResult> UploadChunkAsync(
        string uploadId,
        int chunkIndex,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        var checksum = Request.Headers["X-Chunk-SHA256"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(checksum))
        {
            return DeploymentBoundaryDiagnostics.ValidationError(
                "X-Chunk-SHA256 header is required.",
                "Calculate the SHA-256 checksum for this chunk and retry it.",
                field: "X-Chunk-SHA256",
                endpoint: $"/api/deployment/upload/{uploadId}/chunk/{chunkIndex}",
                expectedRequestType: "application/octet-stream");
        }

        // Buffer asynchronously first — Request.Body disallows synchronous reads.
        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        var correlationId = Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? uploadId;
        logger.LogInformation(
            "Upload chunk received DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk} Bytes={Bytes} Checksum={Checksum}",
            correlationId,
            uploadId,
            chunkIndex,
            buffer.Length,
            checksum);
        var session = await deploymentService.UploadChunkAsync(
            uploadId,
            chunkIndex,
            new UploadChunkMetadata
            {
                DeploymentId = correlationId,
                Checksum = checksum,
                ContentLength = buffer.Length
            },
            buffer,
            cancellationToken);
        logger.LogInformation(
            "Upload chunk committed DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk} BytesReceived={BytesReceived} Progress={ProgressPercent}",
            correlationId,
            uploadId,
            chunkIndex,
            session.BytesReceived,
            session.ProgressPercent);
        return Ok(session);
    }

    [HttpGet("upload/{uploadId}")]
    public async Task<IActionResult> GetUploadSessionAsync(
        string uploadId,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.GetUploadSessionAsync(uploadId, cancellationToken));

    [HttpPost("upload/{uploadId}/complete")]
    public async Task<IActionResult> CompleteChunkedUploadAsync(
        string uploadId,
        [FromForm] string runtime,
        [FromForm] string environment,
        [FromForm] string uploadedBy,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Upload finalization requested UploadId={UploadId} Runtime={Runtime} Environment={Environment}",
            uploadId,
            runtime,
            environment);
        var result = await deploymentService.CompleteChunkedUploadAsync(
            uploadId,
            runtime,
            environment,
            uploadedBy,
            cancellationToken);
        logger.LogInformation(
            "Upload finalized UploadId={UploadId} ReleaseId={ReleaseId} PackageId={PackageId} State={State}",
            uploadId,
            result.ReleaseId,
            result.PackageId,
            result.State);
        return Ok(result);
    }

    [HttpGet("certification/{deploymentId}")]
    public async Task<IActionResult> GetCertificationAsync(
        string deploymentId,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
    {
        var report = await deploymentService.GetCertificationAsync(deploymentId, cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }

    [HttpPost("replay/{deploymentId}")]
    public async Task<IActionResult> ReplayDeploymentAsync(
        string deploymentId,
        [FromBody] PlatformRollbackRequest request,
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.ReplayDeploymentAsync(deploymentId, request.RequestedBy, cancellationToken));

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetricsAsync(
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.GetReliabilityMetricsAsync(cancellationToken));

    [HttpPost("chaos")]
    public async Task<IActionResult> RunChaosTestsAsync(
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.RunChaosTestsAsync(cancellationToken));

    [HttpPost("reconcile")]
    public async Task<IActionResult> ReconcileAsync(
        [FromServices] IPlatformDeploymentService deploymentService,
        CancellationToken cancellationToken)
        => Ok(await deploymentService.ReconcileAsync(cancellationToken));

    /// <summary>Sprint M2.7: the lightweight fleet heartbeat — the Operations Command Center's primary read of this environment's state.</summary>
    [HttpGet("heartbeat")]
    public async Task<IActionResult> GetHeartbeatAsync(
        [FromServices] IRuntimeHeartbeatService heartbeatService,
        CancellationToken cancellationToken)
        => Ok(await heartbeatService.GetHeartbeatAsync(cancellationToken));

    [HttpGet("maintenance")]
    public IActionResult GetMaintenanceMode([FromServices] IRuntimeHeartbeatService heartbeatService)
        => Ok(heartbeatService.GetMaintenanceMode());

    [HttpPost("maintenance")]
    public IActionResult SetMaintenanceMode(
        [FromBody] MaintenanceModeRequest request,
        [FromServices] IRuntimeHeartbeatService heartbeatService)
        => Ok(heartbeatService.SetMaintenanceMode(request));

    [HttpPost("maintenance/resume")]
    public IActionResult ResumeFromMaintenance(
        [FromBody] MaintenanceModeRequest request,
        [FromServices] IRuntimeHeartbeatService heartbeatService)
        => Ok(heartbeatService.ResumeFromMaintenance(request.RequestedBy));

    /// <summary>
    /// Sprint M4.4 — the full Deployment Ready report, including per-requirement diagnostics with
    /// remediation text ("Failed admission includes actionable diagnostics" — M4.4 acceptance).
    /// The heartbeat (<see cref="GetHeartbeatAsync"/>) only carries the compact
    /// <c>DeploymentReady</c>/<c>AdmissionBlockerCount</c> summary; CSM/operators fetch this
    /// endpoint on demand to see why.
    /// </summary>
    [HttpGet("admission")]
    public async Task<IActionResult> GetAdmissionAsync(
        [FromServices] EnvironmentAdmissionStore admissionStore,
        CancellationToken cancellationToken)
    {
        var report = await admissionStore.TryLoadAsync(cancellationToken);
        return report is null ? NotFound() : Ok(report);
    }
}
