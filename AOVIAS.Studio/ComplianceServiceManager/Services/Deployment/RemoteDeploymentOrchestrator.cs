using System.IO;
using System.Security.Cryptography;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;
using Aovias.Release.SDK;
using ComplianceServiceManager.Contracts.Deployment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ComplianceServiceManager.Services.Deployment;

/// <summary>
/// Orchestrates remote platform / application / service deployments from AOVIAS Studio.
/// </summary>
public sealed class RemoteDeploymentOrchestrator : IRemoteDeploymentOrchestrator
{
    private readonly IRemoteAgentClientFactory clientFactory;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings;
    private readonly ILogger<RemoteDeploymentOrchestrator> logger;

    public event EventHandler<DeploymentUploadProgress>? UploadProgressChanged;

    public RemoteDeploymentOrchestrator(
        IRemoteAgentClientFactory clientFactory,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings,
        ILogger<RemoteDeploymentOrchestrator> logger)
    {
        this.clientFactory = clientFactory;
        this.runtimeSettings = runtimeSettings;
        this.deploymentSettings = deploymentSettings;
        this.logger = logger;
    }

    public async Task<PlatformDeploymentManifest> LoadManifestAsync(
        string releaseLibraryRoot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifestPath = Path.Combine(releaseLibraryRoot, "solution-deployment.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("solution-deployment.json was not found in the release library.", manifestPath);
        }

        var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        return System.Text.Json.JsonSerializer.Deserialize<PlatformDeploymentManifest>(
                   json,
                   new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to deserialize solution-deployment.json.");
    }

    public async Task<PlatformDeploymentResponse> DeployPlatformAsync(
        RemoteEnvironment environment,
        string releaseLibraryRoot,
        string requestedBy,
        IReadOnlyList<string>? selectedServiceIds = null,
        CancellationToken cancellationToken = default)
    {
        var mode = PlatformDeploymentScope.InferMode(selectedServiceIds);
        logger.LogInformation(
            "RemoteDeploymentOrchestrator entering DeployPlatformAsync. Environment={Environment} Mode={Mode} SelectedServices={SelectedCount}",
            environment.Name,
            mode,
            selectedServiceIds?.Count ?? 0);

        try
        {
            var clients = clientFactory.Create(environment);
            var manifest = await LoadManifestAsync(releaseLibraryRoot, cancellationToken);

            // Single-service mode uploads exactly one package and executes its release workflow.
            // Runtime Agent is bootstrap infrastructure; Gateway never requires its package.
            if (selectedServiceIds is { Count: 1 }
                && !PlatformDeploymentScope.IsInfrastructureService(selectedServiceIds[0]))
            {
                return await DeploySingleApplicationPackageAsync(
                    manifest,
                    releaseLibraryRoot,
                    selectedServiceIds[0],
                    environment,
                    requestedBy,
                    clients,
                    cancellationToken);
            }

            var effectiveIds = PlatformDeploymentScope.ResolveEffectiveServiceIds(manifest, selectedServiceIds, mode);
            IReadOnlyList<string>? packageFilter = mode == PlatformDeploymentMode.Platform ? null : effectiveIds;

            var scopedServices = manifest.Services
                .Where(service => packageFilter is null ||
                                  packageFilter.Contains(service.ServiceId, StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Drop infrastructure packages from Application deployments even if present in the filter set.
            if (mode == PlatformDeploymentMode.Application)
            {
                scopedServices.RemoveAll(service =>
                    PlatformDeploymentScope.IsInfrastructureService(service.ServiceId));
            }

            // A remote Runtime Agent cannot open Studio-local package paths. Transfer every
            // scoped package first, then send only paths acknowledged by that Runtime Agent.
            var packagePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var service in scopedServices)
            {
                var localPackagePath = ResolvePackagePath(releaseLibraryRoot, service);
                var upload = await UploadPackageChunkedAsync(
                    localPackagePath,
                    runtimeSettings.CurrentValue.ServerName,
                    environment.Name,
                    requestedBy,
                    clients.Deployment,
                    cancellationToken);
                packagePaths[service.ServiceId] = upload.PackagePath;
            }

            var request = new PlatformDeploymentRequest
            {
                RequestedBy = requestedBy,
                Runtime = runtimeSettings.CurrentValue.ServerName,
                Environment = environment.Name,
                Manifest = manifest,
                PackagePaths = packagePaths,
                OperatorConfirmed = true,
                ApprovalGranted = true,
                SelectedServiceIds = packageFilter,
                DeploymentMode = mode
            };

            logger.LogInformation(
                "RemoteDeploymentOrchestrator posting platform deploy. Mode={Mode} PackageKeys={PackageKeys}",
                mode,
                string.Join(",", packagePaths.Keys));

            var response = await clients.Deployment.DeployPlatformAsync(request, cancellationToken);
            logger.LogInformation(
                "RemoteDeploymentOrchestrator exiting DeployPlatformAsync. DeploymentId={DeploymentId} Mode={Mode} State={State} Packages={PackageCount}",
                response.DeploymentId,
                mode,
                response.State,
                packagePaths.Count);
            return response;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "RemoteDeploymentOrchestrator failed DeployPlatformAsync. Environment={Environment} Mode={Mode}",
                environment.Name,
                mode);
            throw;
        }
    }

    private async Task<PlatformDeploymentResponse> DeploySingleApplicationPackageAsync(
        PlatformDeploymentManifest manifest,
        string releaseLibraryRoot,
        string serviceId,
        RemoteEnvironment environment,
        string requestedBy,
        RemoteAgentClients clients,
        CancellationToken cancellationToken)
    {
        var service = ResolveManifestService(manifest, serviceId)
            ?? throw new InvalidOperationException(
                $"Service '{serviceId}' was not found in solution-deployment.json.");

        var packagePath = ResolvePackagePath(releaseLibraryRoot, service);
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException(
                string.Join(
                    Environment.NewLine,
                    [
                        "Package Missing",
                        service.Name,
                        "Expected",
                        packagePath,
                        "Found",
                        "No",
                        "Recovery",
                        $"Publish {service.Name}",
                        "Upload Package",
                        "Retry"
                    ]),
                packagePath);
        }

        logger.LogInformation(
            "RemoteDeploymentOrchestrator single-service deploy. Service={ServiceId} Package={PackagePath} SizeBytes={SizeBytes}",
            service.ServiceId,
            packagePath,
            new FileInfo(packagePath).Length);

        return await DeploySingleServiceViaChunkedUploadAsync(
            service,
            packagePath,
            environment,
            requestedBy,
            clients,
            cancellationToken);
    }

    private async Task<PlatformDeploymentResponse> DeploySingleServiceViaChunkedUploadAsync(
        PlatformDeploymentServiceEntry service,
        string packagePath,
        RemoteEnvironment environment,
        string requestedBy,
        RemoteAgentClients clients,
        CancellationToken cancellationToken)
    {
        var upload = await UploadPackageChunkedAsync(
            packagePath,
            runtimeSettings.CurrentValue.ServerName,
            environment.Name,
            requestedBy,
            clients.Deployment,
            cancellationToken);

        await ValidateAndPlanReleaseAsync(clients, upload.ReleaseId, requestedBy, cancellationToken);
        var deploy = await clients.Release.DeployAsync(
            upload.ReleaseId,
            requestedBy,
            operatorConfirmed: true,
            cancellationToken);

        var deploymentId = !string.IsNullOrWhiteSpace(deploy.WorkflowId)
            ? deploy.WorkflowId!
            : upload.ReleaseId;

        return new PlatformDeploymentResponse
        {
            DeploymentId = deploymentId,
            State = PlatformDeploymentState.Queued,
            Message = string.IsNullOrWhiteSpace(deploy.Message)
                ? $"Single Service deployment queued for {service.Name}."
                : deploy.Message,
            Services =
            [
                new ServiceDeploymentStatus
                {
                    ServiceId = service.ServiceId,
                    Name = service.Name,
                    Status = "Pending",
                    ReleaseId = upload.ReleaseId,
                    WorkflowId = deploy.WorkflowId
                }
            ]
        };
    }

    private async Task<UploadPackageResult> UploadPackageChunkedAsync(
        string packagePath,
        string runtime,
        string environment,
        string uploadedBy,
        IDeploymentClient deploymentClient,
        CancellationToken cancellationToken)
    {
        var (fileInfo, checksum) = await ValidatePackageForUploadAsync(packagePath, cancellationToken);
        await ValidateRuntimeConnectivityAsync(deploymentClient, cancellationToken);

        var settings = deploymentSettings.CurrentValue;
        var chunkSize = Math.Clamp(settings.UploadChunkSizeBytes, 256 * 1024, 16 * 1024 * 1024);
        var deploymentId = BuildUploadDeploymentId(environment, fileInfo.Name, checksum);

        logger.LogInformation(
            "Upload stage=OpeningPackage DeploymentId={DeploymentId} Package={Package} SizeBytes={SizeBytes} ChunkSizeBytes={ChunkSizeBytes} Checksum={Checksum}",
            deploymentId,
            fileInfo.Name,
            fileInfo.Length,
            chunkSize,
            checksum);

        var session = await deploymentClient.InitiateChunkedUploadAsync(
            new InitiateChunkedUploadRequest
            {
                ProtocolVersion = 1,
                DeploymentId = deploymentId,
                ServiceId = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileName = fileInfo.Name,
                TotalBytes = fileInfo.Length,
                Checksum = checksum,
                ChunkSizeBytes = chunkSize
            },
            cancellationToken);

        if (session.ProtocolVersion < 1 || string.IsNullOrWhiteSpace(session.DeploymentId))
        {
            throw new DeploymentFailureException(new DeploymentFailure
            {
                Category = DeploymentFailureCategory.Infrastructure.ToString(),
                Phase = "Upload",
                Component = "RuntimeAgent",
                Operation = "NegotiateChunkedUpload",
                ExceptionType = "UploadProtocolVersionMismatch",
                Message =
                    $"Runtime Agent returned upload protocol v{session.ProtocolVersion}; Studio requires v1.",
                Recovery =
                    "Republish and re-bootstrap Runtime Agent on this target, then regenerate/reload its Nginx configuration.",
                CorrelationId = deploymentId,
                ServiceId = Path.GetFileNameWithoutExtension(fileInfo.Name),
                IsTransient = false
            });
        }

        logger.LogInformation(
            "Upload stage=ServerAccepted DeploymentId={DeploymentId} UploadId={UploadId} ReceivedChunks={ReceivedChunks} TotalChunks={TotalChunks} BytesReceived={BytesReceived}",
            deploymentId,
            session.UploadId,
            session.ReceivedChunks,
            session.TotalChunks,
            session.BytesReceived);

        var received = session.ReceivedChunkIndices.ToHashSet();
        var initialBytesReceived = session.BytesReceived;
        var started = System.Diagnostics.Stopwatch.StartNew();
        await using var file = new FileStream(
            packagePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: chunkSize,
            useAsync: true);
        var buffer = new byte[chunkSize];
        for (var chunkIndex = 0; chunkIndex < session.TotalChunks; chunkIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (received.Contains(chunkIndex))
            {
                continue;
            }

            var offset = (long)chunkIndex * chunkSize;
            file.Seek(offset, SeekOrigin.Begin);
            var expectedLength = (int)Math.Min(chunkSize, fileInfo.Length - offset);
            var read = 0;
            while (read < expectedLength)
            {
                var count = await file.ReadAsync(
                    buffer.AsMemory(read, expectedLength - read),
                    cancellationToken);
                if (count == 0)
                {
                    throw new EndOfStreamException(
                        $"Package ended while reading chunk {chunkIndex}. Expected {expectedLength} bytes; read {read} bytes.");
                }

                read += count;
            }

            var chunk = new byte[read];
            Buffer.BlockCopy(buffer, 0, chunk, 0, read);
            var chunkChecksum = Convert.ToHexString(SHA256.HashData(chunk)).ToLowerInvariant();
            session = await UploadChunkWithRetryAsync(
                session,
                chunkIndex,
                chunk,
                chunkChecksum,
                fileInfo.Length,
                settings,
                deploymentClient,
                cancellationToken);

            var elapsedSeconds = Math.Max(started.Elapsed.TotalSeconds, 0.001);
            var transferredThisRun = Math.Max(0, session.BytesReceived - initialBytesReceived);
            var bytesPerSecond = transferredThisRun / elapsedSeconds;
            var remainingBytes = Math.Max(0, fileInfo.Length - session.BytesReceived);
            TimeSpan? eta = bytesPerSecond <= 0
                ? null
                : TimeSpan.FromSeconds(remainingBytes / bytesPerSecond);
            UploadProgressChanged?.Invoke(
                this,
                new DeploymentUploadProgress
                {
                    DeploymentId = deploymentId,
                    ServiceId = Path.GetFileNameWithoutExtension(fileInfo.Name),
                    FileName = fileInfo.Name,
                    ChunkIndex = chunkIndex,
                    TotalChunks = session.TotalChunks,
                    BytesUploaded = session.BytesReceived,
                    TotalBytes = fileInfo.Length,
                    BytesPerSecond = bytesPerSecond,
                    EstimatedRemaining = eta,
                    Stage = "Uploading"
                });
        }

        logger.LogInformation(
            "Upload stage=ChunksComplete DeploymentId={DeploymentId} UploadId={UploadId} Chunks={ChunkCount} BytesUploaded={BytesUploaded} ElapsedMs={ElapsedMs}",
            deploymentId,
            session.UploadId,
            session.TotalChunks,
            session.BytesReceived,
            started.ElapsedMilliseconds);

        var result = await deploymentClient.CompleteChunkedUploadAsync(
            session.UploadId,
            runtime,
            environment,
            uploadedBy,
            cancellationToken);
        logger.LogInformation(
            "Upload stage=ResponseReceived DeploymentId={DeploymentId} UploadId={UploadId} ReleaseId={ReleaseId} State={State} ElapsedMs={ElapsedMs}",
            deploymentId,
            session.UploadId,
            result.ReleaseId,
            result.State,
            started.ElapsedMilliseconds);
        UploadProgressChanged?.Invoke(
            this,
            new DeploymentUploadProgress
            {
                DeploymentId = deploymentId,
                ServiceId = Path.GetFileNameWithoutExtension(fileInfo.Name),
                FileName = fileInfo.Name,
                ChunkIndex = session.TotalChunks - 1,
                TotalChunks = session.TotalChunks,
                BytesUploaded = fileInfo.Length,
                TotalBytes = fileInfo.Length,
                BytesPerSecond = fileInfo.Length / Math.Max(started.Elapsed.TotalSeconds, 0.001),
                EstimatedRemaining = TimeSpan.Zero,
                Stage = "Complete"
            });
        return result;
    }

    private async Task<ChunkedUploadSession> UploadChunkWithRetryAsync(
        ChunkedUploadSession session,
        int chunkIndex,
        byte[] chunk,
        string chunkChecksum,
        long totalPackageBytes,
        RemoteDeploymentSettings settings,
        IDeploymentClient deploymentClient,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(1, settings.UploadTransientRetryCount + 1);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(ResolveUploadTimeoutSeconds(totalPackageBytes, settings)));
            try
            {
                logger.LogInformation(
                    "Upload stage=ChunkStarted DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk} TotalChunks={TotalChunks} Bytes={Bytes} Attempt={Attempt}",
                    session.DeploymentId,
                    session.UploadId,
                    chunkIndex,
                    session.TotalChunks,
                    chunk.Length,
                    attempt);
                await using var chunkStream = new MemoryStream(chunk, writable: false);
                var acknowledged = await deploymentClient.UploadChunkAsync(
                    session.UploadId,
                    chunkIndex,
                    new UploadChunkMetadata
                    {
                        DeploymentId = session.DeploymentId,
                        Checksum = chunkChecksum,
                        ContentLength = chunk.Length
                    },
                    chunkStream,
                    timeout.Token);
                logger.LogInformation(
                    "Upload stage=ChunkAccepted DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk} BytesReceived={BytesReceived} Attempt={Attempt}",
                    session.DeploymentId,
                    session.UploadId,
                    chunkIndex,
                    acknowledged.BytesReceived,
                    attempt);
                return acknowledged;
            }
            catch (Exception exception) when (IsTransientUploadFailure(exception) && attempt < maxAttempts)
            {
                try
                {
                    var status = await deploymentClient.GetUploadSessionAsync(session.UploadId, cancellationToken);
                    if (status.ReceivedChunkIndices.Contains(chunkIndex))
                    {
                        logger.LogInformation(
                            "Upload stage=ChunkRecoveredFromStatus DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk} Attempt={Attempt}",
                            session.DeploymentId,
                            session.UploadId,
                            chunkIndex,
                            attempt);
                        return status;
                    }
                }
                catch (Exception statusException)
                {
                    logger.LogWarning(
                        statusException,
                        "Upload status probe failed DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk}",
                        session.DeploymentId,
                        session.UploadId,
                        chunkIndex);
                }

                var delay = TimeSpan.FromMilliseconds(
                    Math.Max(100, settings.UploadRetryBaseDelayMilliseconds) * Math.Pow(2, attempt - 1));
                logger.LogWarning(
                    exception,
                    "Transient upload failure DeploymentId={DeploymentId} UploadId={UploadId} Chunk={Chunk} Attempt={Attempt} RetryDelayMs={RetryDelayMs}",
                    session.DeploymentId,
                    session.UploadId,
                    chunkIndex,
                    attempt,
                    delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Chunk {chunkIndex} upload exhausted all attempts.");
    }

    private static bool IsTransientUploadFailure(Exception exception)
    {
        if (exception is DeploymentFailureException deploymentFailure)
        {
            return deploymentFailure.Failure.IsTransient;
        }

        return DeploymentFailureMapper.IsTransient(exception);
    }

    private static int ResolveUploadTimeoutSeconds(long packageBytes, RemoteDeploymentSettings settings)
        => packageBytes >= settings.LargePackageThresholdBytes
            ? settings.LargeUploadTimeoutSeconds
            : packageBytes >= settings.MediumPackageThresholdBytes
                ? settings.MediumUploadTimeoutSeconds
                : settings.SmallUploadTimeoutSeconds;

    private static string BuildUploadDeploymentId(string environment, string fileName, string checksum)
    {
        var identity = $"{environment}|{fileName}|{checksum}";
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)))
            .ToLowerInvariant()[..24];
    }

    private async Task<(FileInfo File, string Checksum)> ValidatePackageForUploadAsync(
        string packagePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException(
                $"Upload rejected before network transfer: package '{packagePath}' does not exist.",
                packagePath);
        }

        var fileInfo = new FileInfo(packagePath);
        if (fileInfo.Length <= 0)
        {
            throw new InvalidDataException(
                $"Upload rejected before network transfer: package '{packagePath}' is empty.");
        }

        try
        {
            await using var stream = new FileStream(
                packagePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                useAsync: true);
            logger.LogInformation(
                "Upload stage=StreamOpened Package={Package} SizeBytes={SizeBytes} CanRead={CanRead}",
                fileInfo.Name,
                fileInfo.Length,
                stream.CanRead);
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            var checksum = Convert.ToHexString(hash).ToLowerInvariant();
            logger.LogInformation(
                "Upload stage=ChecksumGenerated Package={Package} SizeBytes={SizeBytes} Checksum={Checksum}",
                fileInfo.Name,
                fileInfo.Length,
                checksum);
            return (fileInfo, checksum);
        }
        catch (IOException exception)
        {
            throw new IOException(
                $"Upload rejected before network transfer: package '{packagePath}' is locked or unreadable. {exception.Message}",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new UnauthorizedAccessException(
                $"Upload rejected before network transfer: read access denied for '{packagePath}'.",
                exception);
        }
    }

    private async Task ValidateRuntimeConnectivityAsync(
        IDeploymentClient deploymentClient,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await deploymentClient.GetHeartbeatAsync(timeout.Token);
            logger.LogInformation("Upload stage=RuntimeConnected");
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Upload rejected before transfer: Runtime Agent connectivity check failed. {exception.Message}",
                exception);
        }
    }

    public Task<DeploymentStatusResponse?> PollStatusAsync(
        RemoteEnvironment environment,
        string deploymentId,
        CancellationToken cancellationToken = default)
        => clientFactory.Create(environment).Deployment.GetStatusAsync(deploymentId, cancellationToken);

    public Task<PlatformDeploymentResponse> RollbackAsync(
        RemoteEnvironment environment,
        string deploymentId,
        string requestedBy,
        CancellationToken cancellationToken = default)
        => clientFactory.Create(environment).Deployment.RollbackAsync(
            new PlatformRollbackRequest { DeploymentId = deploymentId, RequestedBy = requestedBy },
            cancellationToken);

    public Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(
        RemoteEnvironment environment,
        CancellationToken cancellationToken = default)
        => clientFactory.Create(environment).Deployment.GetHistoryAsync(cancellationToken);

    public Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(
        RemoteEnvironment environment,
        CancellationToken cancellationToken = default)
        => clientFactory.Create(environment).Deployment.GetReliabilityMetricsAsync(cancellationToken);

    private static PlatformDeploymentServiceEntry? ResolveManifestService(
        PlatformDeploymentManifest manifest,
        string serviceId)
    {
        var byId = manifest.Services.FirstOrDefault(entry =>
            string.Equals(entry.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));
        if (byId is not null)
        {
            return byId;
        }

        var packageFile = serviceId.EndsWith(".aovdeploy", StringComparison.OrdinalIgnoreCase)
            ? serviceId
            : $"{serviceId}.aovdeploy";

        return manifest.Services.FirstOrDefault(entry =>
            string.Equals(entry.PackageFileName, packageFile, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(entry.PackagePath)
                && string.Equals(
                    Path.GetFileNameWithoutExtension(entry.PackagePath),
                    Path.GetFileNameWithoutExtension(serviceId),
                    StringComparison.OrdinalIgnoreCase)));
    }

    private static string ResolvePackagePath(string releaseLibraryRoot, PlatformDeploymentServiceEntry service)
    {
        if (!string.IsNullOrWhiteSpace(service.PackagePath) && File.Exists(service.PackagePath))
        {
            return service.PackagePath;
        }

        var candidate = Path.Combine(releaseLibraryRoot, service.PackageFileName);
        if (File.Exists(candidate))
        {
            return candidate;
        }

        return !string.IsNullOrWhiteSpace(service.PackagePath) ? service.PackagePath : candidate;
    }

    /// <inheritdoc />
    public async Task<DeploymentPlan> ValidateAndPlanReleaseOnRuntimeAsync(
        RemoteEnvironment environment,
        string releaseId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseId);
        var clients = clientFactory.Create(environment);
        return await ValidateAndPlanReleaseAsync(clients, releaseId, requestedBy, cancellationToken);
    }

    private static async Task<DeploymentPlan> ValidateAndPlanReleaseAsync(
        RemoteAgentClients clients,
        string releaseId,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        await clients.Release.ValidatePackageAsync(releaseId, cancellationToken);

        var release = await clients.Release.GetReleaseAsync(releaseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Release '{releaseId}' was not found after validation.");

        if (release.State != ReleaseState.Compatible)
        {
            throw new InvalidOperationException(
                $"Release {releaseId} is in state {release.State} after validation; expected {ReleaseState.Compatible}.");
        }

        return await clients.Release.GenerateDeploymentPlanAsync(releaseId, requestedBy, cancellationToken);
    }
}
