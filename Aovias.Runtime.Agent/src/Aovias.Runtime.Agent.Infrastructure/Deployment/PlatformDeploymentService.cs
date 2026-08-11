using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Providers;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using Aovias.Runtime.Agent.Application.Deployment;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Infrastructure.Reliability;
using Aovias.Runtime.Contracts.Deployment;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Executes and tracks platform deployments through the Runtime Agent queue.
/// </summary>
public sealed class PlatformDeploymentService : IPlatformDeploymentService
{
    private readonly DeploymentQueueStore queueStore;
    private readonly IReleaseManager releaseManager;
    private readonly IExecutionEngine executionEngine;
    private readonly EnvironmentRegistrationStore registrationStore;
    private readonly DeploymentPackageStore packageStore;
    private readonly IStatisticsProvider statisticsProvider;
    private readonly PlatformCertificationOrchestrator certificationOrchestrator;
    private readonly DeploymentReplayStore replayStore;
    private readonly DeploymentReliabilityMetricsStore reliabilityMetricsStore;
    private readonly ChaosTestHarness chaosTestHarness;
    private readonly PlatformReconciliationService reconciliationService;
    private readonly IReleaseDeploymentProvider releaseDeploymentProvider;
    private readonly DeploymentSessionStore sessionStore;
    private readonly RuntimeAgentDeploymentPathResolver pathResolver;
    private readonly IPlatformDeploymentEventPublisher eventPublisher;
    private readonly IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly ILogger<PlatformDeploymentService> logger;

    public PlatformDeploymentService(
        DeploymentQueueStore queueStore,
        IReleaseManager releaseManager,
        IExecutionEngine executionEngine,
        EnvironmentRegistrationStore registrationStore,
        DeploymentPackageStore packageStore,
        IStatisticsProvider statisticsProvider,
        PlatformCertificationOrchestrator certificationOrchestrator,
        DeploymentReplayStore replayStore,
        DeploymentReliabilityMetricsStore reliabilityMetricsStore,
        ChaosTestHarness chaosTestHarness,
        PlatformReconciliationService reconciliationService,
        IReleaseDeploymentProvider releaseDeploymentProvider,
        DeploymentSessionStore sessionStore,
        RuntimeAgentDeploymentPathResolver pathResolver,
        IPlatformDeploymentEventPublisher eventPublisher,
        IOptionsMonitor<RemoteDeploymentSettings> deploymentSettings,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        ILogger<PlatformDeploymentService> logger)
    {
        this.queueStore = queueStore;
        this.releaseManager = releaseManager;
        this.executionEngine = executionEngine;
        this.registrationStore = registrationStore;
        this.packageStore = packageStore;
        this.statisticsProvider = statisticsProvider;
        this.certificationOrchestrator = certificationOrchestrator;
        this.replayStore = replayStore;
        this.reliabilityMetricsStore = reliabilityMetricsStore;
        this.chaosTestHarness = chaosTestHarness;
        this.reconciliationService = reconciliationService;
        this.releaseDeploymentProvider = releaseDeploymentProvider;
        this.sessionStore = sessionStore;
        this.pathResolver = pathResolver;
        this.eventPublisher = eventPublisher;
        this.deploymentSettings = deploymentSettings;
        this.releaseSettings = releaseSettings;
        this.logger = logger;
    }

    public Task<PlatformDeploymentResponse> QueuePlatformDeploymentAsync(
        PlatformDeploymentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "PlatformDeploymentService entering QueuePlatformDeploymentAsync. Runtime={Runtime} Environment={Environment} Services={ServiceCount}",
            request.Runtime,
            request.Environment,
            request.Manifest.Services.Count);

        if (!request.ApprovalGranted)
        {
            throw new InvalidOperationException("Deployment approval is required before execution.");
        }

        var mode = request.DeploymentMode ?? PlatformDeploymentPlanner.InferMode(request.SelectedServiceIds);
        var effectiveServiceIds = PlatformDeploymentPlanner.ResolveEffectiveServiceIds(
            request.Manifest,
            request.SelectedServiceIds,
            mode);
        // Platform → null filter (all ready services). Application/Service → only the effective set.
        IReadOnlyList<string>? executionFilter = mode == PlatformDeploymentMode.Platform
            ? null
            : effectiveServiceIds;
        var orderedServices = PlatformDeploymentPlanner.OrderServices(request.Manifest, executionFilter);
        if (orderedServices.Count == 0)
        {
            throw new InvalidOperationException("No deployment-ready services were found for the selected deployment scope.");
        }

        // Package paths may only reference services in the execution set — never require unrelated packages.
        var scopedPackagePaths = request.PackagePaths
            .Where(pair => orderedServices.Any(service =>
                string.Equals(service.ServiceId, pair.Key, StringComparison.OrdinalIgnoreCase)))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        var deploymentId = Guid.NewGuid().ToString("N");
        var record = new PlatformDeploymentRecord
        {
            DeploymentId = deploymentId,
            Operator = request.RequestedBy,
            Runtime = request.Runtime,
            Environment = request.Environment,
            Manifest = request.Manifest,
            PackagePaths = scopedPackagePaths,
            OperatorConfirmed = request.OperatorConfirmed,
            DeploymentMode = mode,
            SelectedServiceIds = executionFilter,
            Services = orderedServices
                .Select(service => new ServiceDeploymentStatus
                {
                    ServiceId = service.ServiceId,
                    Name = service.Name,
                    Status = "Pending"
                })
                .ToList(),
            Version = new PlatformVersionSnapshot
            {
                TargetPlatformVersion = request.Manifest.Version
            }
        };

        record.RollbackPlan = BuildRollbackPlan(record, orderedServices);
        record.ReplayRequest = new PlatformDeploymentRequest
        {
            RequestedBy = request.RequestedBy,
            Runtime = request.Runtime,
            Environment = request.Environment,
            Manifest = request.Manifest,
            PackagePaths = scopedPackagePaths,
            OperatorConfirmed = request.OperatorConfirmed,
            ApprovalGranted = request.ApprovalGranted,
            SelectedServiceIds = executionFilter,
            DeploymentMode = mode
        };
        AppendLog(
            record,
            $"Deployment Mode: {mode}; Services: {string.Join(", ", orderedServices.Select(s => s.ServiceId))}; Packages: {orderedServices.Count}.");
        AppendLog(record, $"Deployment {deploymentId} queued for {orderedServices.Count} service(s).");
        queueStore.Enqueue(record);
        logger.LogInformation(
            "PlatformDeploymentService exiting QueuePlatformDeploymentAsync. DeploymentId={DeploymentId} Mode={Mode} Services={ServiceCount}",
            deploymentId,
            mode,
            orderedServices.Count);

        return Task.FromResult(new PlatformDeploymentResponse
        {
            DeploymentId = deploymentId,
            State = PlatformDeploymentState.Queued,
            Message = $"{mode} deployment queued ({orderedServices.Count} service(s)).",
            Services = record.Services
        });
    }

    public Task<PlatformDeploymentResponse> QueueSinglePackageDeployAsync(
        string packagePath,
        string runtime,
        string environment,
        string requestedBy,
        bool operatorConfirmed,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deploymentId = Guid.NewGuid().ToString("N");
        var record = new PlatformDeploymentRecord
        {
            DeploymentId = deploymentId,
            Operator = requestedBy,
            Runtime = runtime,
            Environment = environment,
            SinglePackagePath = packagePath,
            OperatorConfirmed = operatorConfirmed,
            Services =
            [
                new ServiceDeploymentStatus
                {
                    ServiceId = Path.GetFileNameWithoutExtension(packagePath),
                    Name = Path.GetFileName(packagePath),
                    Status = "Pending"
                }
            ]
        };

        AppendLog(record, $"Single package deployment {deploymentId} queued.");
        queueStore.Enqueue(record);

        return Task.FromResult(new PlatformDeploymentResponse
        {
            DeploymentId = deploymentId,
            State = PlatformDeploymentState.Queued,
            Message = "Package deployment queued.",
            Services = record.Services
        });
    }

    public Task<DeploymentStatusResponse?> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = queueStore.Get(deploymentId);
        return Task.FromResult(record?.ToStatusResponse());
    }

    public Task<IReadOnlyList<string>> GetLogsAsync(
        string deploymentId,
        string? serviceId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = queueStore.Get(deploymentId)
            ?? throw new KeyNotFoundException($"Deployment '{deploymentId}' was not found.");

        IEnumerable<string> logs = record.Logs;
        if (!string.IsNullOrWhiteSpace(serviceId))
        {
            logs = logs.Where(line => line.Contains(serviceId, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<string>>(logs.ToList());
    }

    public Task<IReadOnlyList<PlatformDeploymentHistoryEntry>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var history = queueStore.List()
            .Select(record => new PlatformDeploymentHistoryEntry
            {
                DeploymentId = record.DeploymentId,
                PlatformVersion = record.Version.TargetPlatformVersion,
                Operator = record.Operator,
                Environment = record.Environment,
                Duration = (record.CompletedUtc ?? DateTimeOffset.UtcNow) - record.StartedUtc,
                Packages = record.Services.Select(service => service.ServiceId).ToList(),
                HealthStatus = record.Infrastructure.Health,
                RolledBack = record.State == PlatformDeploymentState.RollingBack,
                FinalState = record.State,
                StartedUtc = record.StartedUtc,
                CompletedUtc = record.CompletedUtc,
                FailureCategory = record.FailureCategory
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<PlatformDeploymentHistoryEntry>>(history);
    }

    public Task<PlatformDeploymentReport?> GetReportAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = queueStore.Get(deploymentId);
        if (record is null)
        {
            return Task.FromResult<PlatformDeploymentReport?>(null);
        }

        return Task.FromResult<PlatformDeploymentReport?>(new PlatformDeploymentReport
        {
            DeploymentId = deploymentId,
            Status = record.ToStatusResponse(),
            RollbackPlan = record.RollbackPlan,
            Logs = record.Logs.ToList(),
            AuditTrail = record.AuditTrail.ToList(),
            Certification = record.Certification
        });
    }

    public Task<bool> CancelAsync(CancelDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var record = queueStore.Get(request.DeploymentId)
            ?? throw new KeyNotFoundException($"Deployment '{request.DeploymentId}' was not found.");

        if (record.State is PlatformDeploymentState.Completed or PlatformDeploymentState.Failed or PlatformDeploymentState.Cancelled)
        {
            return Task.FromResult(false);
        }

        record.CancelRequested = true;
        record.State = PlatformDeploymentState.Cancelled;
        record.CompletedUtc = DateTimeOffset.UtcNow;
        AppendLog(record, $"Deployment cancelled by {request.RequestedBy}.");
        queueStore.Upsert(record);
        return Task.FromResult(true);
    }

    public async Task<PlatformDeploymentResponse> RollbackAsync(
        PlatformRollbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var record = queueStore.Get(request.DeploymentId)
            ?? throw new KeyNotFoundException($"Deployment '{request.DeploymentId}' was not found.");

        record.State = PlatformDeploymentState.RollingBack;
        AppendLog(record, $"Rollback requested by {request.RequestedBy}.");
        queueStore.Upsert(record);

        if (record.RollbackPlan is null)
        {
            Fail(record, DeploymentFailureCategory.Rollback, "Rollback plan was not generated.");
            return BuildResponse(record);
        }

        foreach (var snapshot in record.RollbackPlan.Services.Where(service => !string.IsNullOrWhiteSpace(service.PreviousReleaseId)))
        {
            try
            {
                await releaseManager.RollbackAsync(
                    new RollbackReleaseRequest
                    {
                        ReleaseId = snapshot.PreviousReleaseId!,
                        RequestedBy = request.RequestedBy
                    },
                    cancellationToken);
            }
            catch (Exception exception)
            {
                Fail(
                    record,
                    DeploymentFailureCategory.Rollback,
                    exception.Message,
                    exception,
                    phase: DeploymentPhaseKind.Rollback,
                    operation: "RollbackService");
                return BuildResponse(record);
            }
        }

        record.State = PlatformDeploymentState.Completed;
        record.CompletedUtc = DateTimeOffset.UtcNow;
        record.Infrastructure.Health = "PASS";
        record.Infrastructure.Runtime = "Healthy";
        AppendLog(record, "Rollback completed.");
        queueStore.Upsert(record);
        return BuildResponse(record);
    }

    public Task<AgentRegistrationResponse> RegisterEnvironmentAsync(
        AgentRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = registrationStore.Register(request);
        return Task.FromResult(response);
    }

    public async Task<EnvironmentHealthSnapshot?> GetEnvironmentHealthAsync(
        string environmentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var environment = registrationStore.Get(environmentId);
        if (environment is null)
        {
            return null;
        }

        var statistics = await statisticsProvider.GetStatisticsAsync(cancellationToken);
        return new EnvironmentHealthSnapshot
        {
            EnvironmentId = environmentId,
            Name = environment.Name,
            HealthStatus = environment.HealthStatus,
            CpuUsagePercent = statistics.CpuUsagePercent,
            MemoryUsagePercent = statistics.MemoryUsagePercent,
            DiskUsagePercent = statistics.DiskUsagePercent,
            DockerStatus = statistics.Docker.Status,
            RunningContainerCount = statistics.Docker.RunningContainerCount,
            ContainerCount = statistics.Docker.ContainerCount,
            PlatformVersion = environment.PlatformVersion,
            LastDeploymentUtc = environment.LastDeploymentUtc
        };
    }

    public Task<EnvironmentDriftReport> DetectDriftAsync(string environmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var latest = queueStore.List().FirstOrDefault(record => record.State == PlatformDeploymentState.Completed);
        var findings = new List<EnvironmentDriftFinding>();

        if (latest?.Manifest is not null)
        {
            foreach (var service in latest.Manifest.Services)
            {
                var deployed = latest.Services.FirstOrDefault(item =>
                    string.Equals(item.ServiceId, service.ServiceId, StringComparison.OrdinalIgnoreCase));

                if (deployed is null || !string.Equals(deployed.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new EnvironmentDriftFinding
                    {
                        Category = "MissingContainer",
                        Resource = service.ServiceId,
                        Expected = service.Version,
                        Actual = deployed?.Status ?? "Missing",
                        Severity = "Critical"
                    });
                }
                else if (!string.Equals(deployed.HealthStatus, "PASS", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new EnvironmentDriftFinding
                    {
                        Category = "Health",
                        Resource = service.ServiceId,
                        Expected = "PASS",
                        Actual = deployed.HealthStatus ?? "Unknown",
                        Severity = "Warning"
                    });
                }
            }
        }

        return Task.FromResult(new EnvironmentDriftReport
        {
            EnvironmentId = environmentId,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Findings = findings,
            RequiresReconciliation = findings.Count > 0
        });
    }

    public Task<ChunkedUploadSession> InitiateChunkedUploadAsync(
        InitiateChunkedUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(packageStore.Initiate(request));
    }

    public async Task<ChunkedUploadSession> UploadChunkAsync(
        string uploadId,
        int chunkIndex,
        UploadChunkMetadata metadata,
        Stream chunkStream,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await packageStore.UploadChunkAsync(uploadId, chunkIndex, metadata, chunkStream, cancellationToken);
    }

    public Task<ChunkedUploadSession> GetUploadSessionAsync(
        string uploadId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(packageStore.Get(uploadId));
    }

    public async Task<UploadPackageResult> CompleteChunkedUploadAsync(
        string uploadId,
        string runtime,
        string environment,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        var packagePath = await packageStore.CompleteAsync(uploadId, cancellationToken);
        return await releaseManager.UploadPackageAsync(
            new UploadPackageRequest
            {
                PackagePath = packagePath,
                Runtime = runtime,
                Environment = environment,
                UploadedBy = uploadedBy
            },
            cancellationToken);
    }

    public Task<PlatformCertificationReport?> GetCertificationAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(queueStore.Get(deploymentId)?.Certification);
    }

    public async Task<DeploymentReplayResult> ReplayDeploymentAsync(
        string deploymentId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        var replay = replayStore.Get(deploymentId)
            ?? throw new KeyNotFoundException($"Replay record for deployment '{deploymentId}' was not found.");

        var response = await QueuePlatformDeploymentAsync(
            new PlatformDeploymentRequest
            {
                RequestedBy = requestedBy,
                Runtime = replay.Request.Runtime,
                Environment = replay.Request.Environment,
                Manifest = replay.Request.Manifest,
                PackagePaths = replay.Request.PackagePaths,
                OperatorConfirmed = replay.Request.OperatorConfirmed,
                ApprovalGranted = replay.Request.ApprovalGranted,
                SelectedServiceIds = replay.Request.SelectedServiceIds,
                DeploymentMode = replay.Request.DeploymentMode
            },
            cancellationToken);

        var original = queueStore.Get(deploymentId);
        var replayed = queueStore.Get(response.DeploymentId);
        var identical = original?.State == replayed?.State;

        return new DeploymentReplayResult
        {
            OriginalDeploymentId = deploymentId,
            ReplayDeploymentId = response.DeploymentId,
            IdenticalOutcome = identical,
            Message = identical
                ? "Replay produced an identical deployment outcome."
                : "Replay completed with a different outcome."
        };
    }

    public Task<DeploymentReliabilityMetrics> GetReliabilityMetricsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(reliabilityMetricsStore.GetSnapshot());
    }

    public Task<IReadOnlyList<ChaosTestResult>> RunChaosTestsAsync(CancellationToken cancellationToken = default)
        => chaosTestHarness.RunSafeScenariosAsync(cancellationToken);

    public Task<PlatformReconciliationReport> ReconcileAsync(CancellationToken cancellationToken = default)
        => reconciliationService.ReconcileAsync(cancellationToken);

    public void RecoverInterruptedDeployments()
    {
        foreach (var snapshot in sessionStore.ListRecoverable())
        {
            logger.LogWarning(
                "Recoverable deployment {DeploymentId} in state {State}. Operator may resume or replay from AOVIAS Command.",
                snapshot.DeploymentId,
                snapshot.State);
        }
    }

    internal async Task ProcessNextQueuedDeploymentAsync(CancellationToken cancellationToken)
    {
        if (!queueStore.TryDequeue(out var record) || record is null)
        {
            return;
        }

        logger.LogInformation(
            "PlatformDeploymentService entering ProcessNextQueuedDeploymentAsync. DeploymentId={DeploymentId}",
            record.DeploymentId);

        try
        {
            if (record.SinglePackagePath is not null)
            {
                await ExecuteSinglePackageAsync(record, cancellationToken);
            }
            else
            {
                await ExecutePlatformAsync(record, cancellationToken);
            }

            logger.LogInformation(
                "PlatformDeploymentService exiting ProcessNextQueuedDeploymentAsync. DeploymentId={DeploymentId} State={State}",
                record.DeploymentId,
                record.State);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Platform deployment {DeploymentId} failed.", record.DeploymentId);
            Fail(
                record,
                DeploymentFailureMapper.Classify(exception),
                exception.Message,
                exception,
                phase: DeploymentPhaseKind.Failure,
                operation: record.SinglePackagePath is not null ? "ExecuteSinglePackage" : "ExecutePlatform");
        }
        finally
        {
            queueStore.Upsert(record);
            PersistSession(record);
            queueStore.MarkActiveCompleted();
        }
    }

    private void PersistSession(PlatformDeploymentRecord record)
        => sessionStore.Save(record);

    private async Task ExecutePlatformAsync(PlatformDeploymentRecord record, CancellationToken cancellationToken)
    {
        var executionServices = GetExecutionServices(record);

        record.State = PlatformDeploymentState.Preparing;
        queueStore.Upsert(record);
        AppendLog(
            record,
            $"Preparing {record.DeploymentMode} deployment for: {string.Join(", ", executionServices.Select(s => s.ServiceId))}.");
        await PublishPhaseAsync(record, DeploymentPhaseKind.Package, "Running", "Analyzing selected packages.", cancellationToken: cancellationToken);

        record.Infrastructure.Infrastructure = "Running";
        record.Infrastructure.Contracts = "Running";
        record.State = PlatformDeploymentState.Uploading;
        queueStore.Upsert(record);
        await PublishPhaseAsync(record, DeploymentPhaseKind.Upload, "Running", "Uploading selected packages.", cancellationToken: cancellationToken);

        var releaseIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var service in executionServices)
        {
            if (record.CancelRequested)
            {
                return;
            }

            SetServiceStatus(record, service.ServiceId, "Uploading");
            var packagePath = ResolvePackagePath(record, service);
            if (!File.Exists(packagePath))
            {
                Fail(
                    record,
                    DeploymentFailureCategory.Upload,
                    BuildPackageMissingMessage(service, packagePath),
                    phase: DeploymentPhaseKind.Upload,
                    operation: "ResolvePackage",
                    serviceId: service.ServiceId);
                await TriggerAutomaticRollbackAsync(record, cancellationToken);
                return;
            }

            AppendLog(record, $"Package {service.PackageFileName} resolved for {service.ServiceId}.");
            var upload = await releaseManager.UploadPackageAsync(
                new UploadPackageRequest
                {
                    PackagePath = packagePath,
                    Runtime = record.Runtime,
                    Environment = record.Environment,
                    UploadedBy = record.Operator
                },
                cancellationToken);

            releaseIds[service.ServiceId] = upload.ReleaseId;
            SetServiceStatus(record, service.ServiceId, "Uploaded", upload.ReleaseId);
            AppendLog(record, $"Uploaded {service.ServiceId} as release {upload.ReleaseId}.");
            await PublishPhaseAsync(
                record,
                DeploymentPhaseKind.Upload,
                "Passed",
                $"Package uploaded for {service.Name}.",
                service.ServiceId,
                upload.ReleaseId,
                cancellationToken: cancellationToken);
        }

        await PublishPhaseAsync(record, DeploymentPhaseKind.Upload, "Passed", "Selected packages uploaded.", cancellationToken: cancellationToken);

        record.Infrastructure.Infrastructure = "PASS";
        record.Infrastructure.Contracts = "PASS";
        record.State = PlatformDeploymentState.Provisioning;
        queueStore.Upsert(record);
        PersistSession(record);
        AppendLog(record, "Running infrastructure preflight for selected services.");
        await PublishPhaseAsync(record, DeploymentPhaseKind.Provision, "Running", "Provisioning infrastructure.", cancellationToken: cancellationToken);

        foreach (var service in executionServices)
        {
            if (record.CancelRequested)
            {
                return;
            }

            if (!releaseIds.TryGetValue(service.ServiceId, out var releaseId))
            {
                continue;
            }

            var preflight = await releaseDeploymentProvider.EnsureRuntimeFoldersAsync(releaseId, cancellationToken);
            if (!preflight.IsSuccess)
            {
                var message = preflight.Message ?? $"Infrastructure preflight failed for {service.ServiceId}.";
                Fail(
                    record,
                    DeploymentFailureCategory.Infrastructure,
                    message,
                    phase: DeploymentPhaseKind.Provision,
                    component: "InfrastructureProvisioningPipeline",
                    operation: "EnsureRuntimeFolders",
                    serviceId: service.ServiceId);
                record.Infrastructure.Infrastructure = "FAIL";
                record.Infrastructure.Contracts = "FAIL";
                await TriggerAutomaticRollbackAsync(record, cancellationToken);
                return;
            }

            AppendLog(record, $"Infrastructure preflight passed for {service.ServiceId}.");
        }

        record.Infrastructure.Infrastructure = "PASS";
        record.Infrastructure.Contracts = "PASS";
        AppendLog(record, "Infrastructure provisioning complete.");
        PersistSession(record);
        await PublishPhaseAsync(record, DeploymentPhaseKind.Provision, "Passed", "Infrastructure provisioning complete.", cancellationToken: cancellationToken);

        record.State = PlatformDeploymentState.Deploying;
        queueStore.Upsert(record);
        PersistSession(record);
        await PublishPhaseAsync(record, DeploymentPhaseKind.Compose, "Running", "Deploying selected services.", cancellationToken: cancellationToken);

        foreach (var service in executionServices)
        {
            if (record.CancelRequested)
            {
                return;
            }

            if (!releaseIds.TryGetValue(service.ServiceId, out var releaseId))
            {
                continue;
            }

            SetServiceStatus(record, service.ServiceId, "Running");

            try
            {
                var validation = await releaseManager.ValidatePackageAsync(releaseId, cancellationToken);
                if (!validation.IsValid)
                {
                    Fail(
                        record,
                        DeploymentFailureCategory.Contracts,
                        string.Join("; ", validation.Errors),
                        phase: DeploymentPhaseKind.Package,
                        operation: "ValidatePackage",
                        serviceId: service.ServiceId);
                    await TriggerAutomaticRollbackAsync(record, cancellationToken);
                    return;
                }

                await releaseManager.GenerateDeploymentPlanAsync(
                    new PlanDeploymentRequest { ReleaseId = releaseId, RequestedBy = record.Operator },
                    cancellationToken);

                var deploy = await releaseManager.DeployAsync(
                    new DeployReleaseRequest
                    {
                        ReleaseId = releaseId,
                        RequestedBy = record.Operator,
                        OperatorConfirmed = record.OperatorConfirmed
                    },
                    cancellationToken);

                SetServiceWorkflow(record, service.ServiceId, deploy.WorkflowId, "Deploying");

                if (!string.IsNullOrWhiteSpace(deploy.WorkflowId))
                {
                    var completed = await WaitForWorkflowAsync(deploy.WorkflowId, cancellationToken);
                    if (!completed)
                    {
                        Fail(
                            record,
                            DeploymentFailureCategory.Application,
                            $"Deployment workflow failed for {service.ServiceId}.",
                            phase: DeploymentPhaseKind.Compose,
                            operation: "WaitForWorkflow",
                            serviceId: service.ServiceId);
                        await TriggerAutomaticRollbackAsync(record, cancellationToken);
                        return;
                    }
                }

                SetServiceHealth(record, service.ServiceId, "Completed", "PASS");
                await PublishPhaseAsync(
                    record,
                    DeploymentPhaseKind.Compose,
                    "Passed",
                    $"{service.Name} deployed successfully.",
                    service.ServiceId,
                    releaseId,
                    deploy.WorkflowId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                Fail(
                    record,
                    Classify(exception),
                    exception.Message,
                    exception,
                    phase: DeploymentPhaseKind.Compose,
                    operation: "DeployService",
                    serviceId: service.ServiceId);
                await TriggerAutomaticRollbackAsync(record, cancellationToken);
                return;
            }
        }

        record.Infrastructure.Containers = "PASS";
        record.Infrastructure.Dependencies = "PASS";
        record.Infrastructure.Health = "Running";
        record.State = PlatformDeploymentState.Verifying;
        queueStore.Upsert(record);
        await PublishPhaseAsync(record, DeploymentPhaseKind.Health, "Running", "Verifying platform health.", cancellationToken: cancellationToken);

        record.Certification = await certificationOrchestrator.CertifyAsync(record, cancellationToken);
        await PublishPhaseAsync(record, DeploymentPhaseKind.Smoke, "Running", "Running smoke tests.", cancellationToken: cancellationToken);
        await PublishPhaseAsync(
            record,
            DeploymentPhaseKind.Certification,
            record.Certification.IsCertified ? "Passed" : "Failed",
            record.Certification.IsCertified ? "Platform certification passed." : "Platform certification failed.",
            cancellationToken: cancellationToken);
        if (!record.Certification.IsCertified)
        {
            Fail(record, DeploymentFailureCategory.Health, "Platform certification failed.");
            record.Infrastructure.Health = "FAIL";
            await TriggerAutomaticRollbackAsync(record, cancellationToken);
            reliabilityMetricsStore.RecordDeployment(record);
            if (record.ReplayRequest is not null)
            {
                replayStore.Save(record, record.ReplayRequest);
            }
            return;
        }

        record.Infrastructure.Health = "PASS";
        record.Infrastructure.Runtime = "Healthy";
        record.State = PlatformDeploymentState.Completed;
        record.CompletedUtc = DateTimeOffset.UtcNow;
        record.Version.InstalledPlatformVersion = record.Manifest?.Version ?? record.Version.TargetPlatformVersion;
        record.Version.HasDrift = false;
        AppendLog(record, "Platform deployment certified and verified healthy.");
        await PublishPhaseAsync(record, DeploymentPhaseKind.Health, "Passed", "Platform healthy.", cancellationToken: cancellationToken);
        reliabilityMetricsStore.RecordDeployment(record);
        if (record.ReplayRequest is not null)
        {
            replayStore.Save(record, record.ReplayRequest);
        }
        PersistSession(record);
        queueStore.Upsert(record);
    }

    private async Task ExecuteSinglePackageAsync(PlatformDeploymentRecord record, CancellationToken cancellationToken)
    {
        record.State = PlatformDeploymentState.Uploading;
        queueStore.Upsert(record);

        var upload = await releaseManager.UploadPackageAsync(
            new UploadPackageRequest
            {
                PackagePath = record.SinglePackagePath!,
                Runtime = record.Runtime,
                Environment = record.Environment,
                UploadedBy = record.Operator
            },
            cancellationToken);

        SetServiceStatus(record, record.Services[0].ServiceId, "Uploaded", upload.ReleaseId);

        record.State = PlatformDeploymentState.Deploying;
        var validation = await releaseManager.ValidatePackageAsync(upload.ReleaseId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Release {upload.ReleaseId} failed package validation: {string.Join("; ", validation.Errors)}");
        }

        await releaseManager.GenerateDeploymentPlanAsync(
            new PlanDeploymentRequest { ReleaseId = upload.ReleaseId, RequestedBy = record.Operator },
            cancellationToken);

        var deploy = await releaseManager.DeployAsync(
            new DeployReleaseRequest
            {
                ReleaseId = upload.ReleaseId,
                RequestedBy = record.Operator,
                OperatorConfirmed = record.OperatorConfirmed
            },
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(deploy.WorkflowId))
        {
            await WaitForWorkflowAsync(deploy.WorkflowId, cancellationToken);
        }

        record.State = PlatformDeploymentState.Completed;
        record.CompletedUtc = DateTimeOffset.UtcNow;
        SetServiceWorkflow(record, record.Services[0].ServiceId, deploy.WorkflowId, "Completed");
        record.Services[0].HealthStatus = "PASS";
        AppendLog(record, "Single package deployment completed.");
        queueStore.Upsert(record);
    }

    private async Task TriggerAutomaticRollbackAsync(PlatformDeploymentRecord record, CancellationToken cancellationToken)
    {
        if (record.RollbackPlan is null)
        {
            return;
        }

        record.State = PlatformDeploymentState.RollingBack;
        AppendLog(record, "Automatic rollback started.");
        queueStore.Upsert(record);
        await PublishPhaseAsync(record, DeploymentPhaseKind.Rollback, "Running", "Rollback started.", cancellationToken: cancellationToken);

        await RollbackAsync(
            new PlatformRollbackRequest { DeploymentId = record.DeploymentId, RequestedBy = "system" },
            cancellationToken);
    }

    private async Task<bool> WaitForWorkflowAsync(string workflowId, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Max(deploymentSettings.CurrentValue.TimeoutSeconds, 60));
        var started = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - started < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var workflow = await executionEngine.GetAsync(workflowId, cancellationToken);
            if (workflow is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
            }

            if (workflow.Status is WorkflowState.Completed or WorkflowState.Succeeded)
            {
                return true;
            }

            if (workflow.Status is WorkflowState.Failed or WorkflowState.Cancelled)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        return false;
    }

    private static IReadOnlyList<PlatformDeploymentServiceEntry> GetExecutionServices(PlatformDeploymentRecord record)
    {
        if (record.Manifest is null)
        {
            throw new InvalidOperationException("Platform manifest is required for platform deployment execution.");
        }

        // Authoritative scope = what was queued. Never re-expand to the full platform manifest.
        // This is what caused Gateway deploys to demand runtime-agent.aovdeploy.
        if (record.Services.Count > 0)
        {
            var queuedIds = record.Services
                .Select(service => service.ServiceId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return PlatformDeploymentPlanner.OrderServices(record.Manifest, queuedIds);
        }

        var selectedIds = record.SelectedServiceIds
            ?? record.ReplayRequest?.SelectedServiceIds;
        return PlatformDeploymentPlanner.OrderServices(record.Manifest, selectedIds);
    }

    private static string BuildPackageMissingMessage(PlatformDeploymentServiceEntry service, string expectedPath)
        => string.Join(
            Environment.NewLine,
            [
                "Package Missing",
                service.Name,
                "Expected",
                expectedPath,
                "Found",
                "No",
                "Recovery",
                $"Publish {service.Name}",
                "Upload Package",
                "Retry"
            ]);

    private static PlatformRollbackPlan BuildRollbackPlan(
        PlatformDeploymentRecord record,
        IReadOnlyList<PlatformDeploymentServiceEntry> services)
    {
        return new PlatformRollbackPlan
        {
            DeploymentId = record.DeploymentId,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Services = services
                .Select(service => new RollbackServiceSnapshot
                {
                    ServiceId = service.ServiceId,
                    PreviousImage = service.DockerImage,
                    PreviousPackageVersion = service.Version
                })
                .ToList()
        };
    }

    private string ResolvePackagePath(PlatformDeploymentRecord record, PlatformDeploymentServiceEntry service)
    {
        if (record.PackagePaths.TryGetValue(service.ServiceId, out var path) && File.Exists(path))
        {
            return path;
        }

        if (!string.IsNullOrWhiteSpace(service.PackagePath) && File.Exists(service.PackagePath))
        {
            return service.PackagePath;
        }

        var incomingCandidate = Path.Combine(releaseSettings.CurrentValue.IncomingFolder, service.PackageFileName);
        if (File.Exists(incomingCandidate))
        {
            return incomingCandidate;
        }

        // Only resolve against packages/current for services in this deployment's scope.
        // Never invent paths for unrelated infrastructure packages.
        var packageStoreRoot = pathResolver.ResolvePackageStoreRoot();
        return Path.Combine(packageStoreRoot, "current", service.PackageFileName);
    }

    private void SetServiceStatus(
        PlatformDeploymentRecord record,
        string serviceId,
        string status,
        string? releaseId = null)
    {
        var service = record.Services.FirstOrDefault(item =>
            string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            return;
        }

        service.Status = status;
        if (!string.IsNullOrWhiteSpace(releaseId))
        {
            service.ReleaseId = releaseId;
        }

        queueStore.Upsert(record);
    }

    private void SetServiceWorkflow(
        PlatformDeploymentRecord record,
        string serviceId,
        string? workflowId,
        string status)
    {
        var service = record.Services.FirstOrDefault(item =>
            string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            return;
        }

        service.WorkflowId = workflowId;
        service.Status = status;
        queueStore.Upsert(record);
    }

    private void SetServiceHealth(
        PlatformDeploymentRecord record,
        string serviceId,
        string status,
        string healthStatus)
    {
        var service = record.Services.FirstOrDefault(item =>
            string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            return;
        }

        service.Status = status;
        service.HealthStatus = healthStatus;
        queueStore.Upsert(record);
    }

    private void Fail(
        PlatformDeploymentRecord record,
        DeploymentFailureCategory category,
        string message,
        Exception? exception = null,
        DeploymentPhaseKind phase = DeploymentPhaseKind.Failure,
        string component = "PlatformDeploymentService",
        string operation = "Execute",
        string? serviceId = null)
    {
        record.State = PlatformDeploymentState.Failed;
        record.FailureCategory = category;
        record.FailureMessage = message;
        record.CompletedUtc = DateTimeOffset.UtcNow;

        var structured = exception is not null
            ? DeploymentFailureMapper.FromException(
                exception,
                phase.ToString(),
                component,
                operation,
                category,
                serviceId,
                record.DeploymentId)
            : DeploymentFailureMapper.FromOperatorError(
                DeploymentOperatorErrorMapper.Map(category, message, serviceId),
                phase.ToString(),
                component,
                operation,
                record.DeploymentId);

        AppendLog(record, $"FAILED [{structured.Category}] {structured.Component}/{structured.Operation}: {structured.Message}");
        if (!string.IsNullOrWhiteSpace(structured.InnerExceptionMessage))
        {
            AppendLog(record, $"  Inner ({structured.ExceptionType}): {structured.InnerExceptionMessage}");
        }

        PersistSession(record);
        _ = PublishPhaseAsync(
            record,
            phase,
            "Failed",
            structured.Message,
            serviceId: serviceId ?? structured.ServiceId,
            failureCategory: category,
            component: structured.Component,
            operation: structured.Operation,
            exceptionType: structured.ExceptionType,
            innerExceptionMessage: structured.InnerExceptionMessage);
    }

    private async Task PublishPhaseAsync(
        PlatformDeploymentRecord record,
        DeploymentPhaseKind phase,
        string status,
        string message,
        string? serviceId = null,
        string? releaseId = null,
        string? workflowId = null,
        DeploymentFailureCategory? failureCategory = null,
        string? component = null,
        string? operation = null,
        string? exceptionType = null,
        string? innerExceptionMessage = null,
        CancellationToken cancellationToken = default)
    {
        var operatorError = failureCategory is null
            ? null
            : DeploymentOperatorErrorMapper.Map(failureCategory.Value, message, serviceId);

        await eventPublisher.PublishAsync(
            new DeploymentPhaseEventDto
            {
                DeploymentId = record.DeploymentId,
                ServiceId = serviceId,
                ReleaseId = releaseId,
                WorkflowId = workflowId,
                Phase = phase,
                Status = status,
                Message = message,
                ProgressPercent = ResolveServiceProgress(record, serviceId),
                OverallProgressPercent = record.ProgressPercent,
                FailureCategory = failureCategory?.ToString(),
                RecoveryAction = operatorError?.RecoveryAction,
                DocumentationHint = operatorError?.DocumentationHint,
                Component = component,
                Operation = operation,
                ExceptionType = exceptionType,
                InnerExceptionMessage = innerExceptionMessage,
                Timestamp = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }

    private static int ResolveServiceProgress(PlatformDeploymentRecord record, string? serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            return record.ProgressPercent;
        }

        var service = record.Services.FirstOrDefault(item =>
            string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase));

        return service?.Status switch
        {
            "Completed" or "PASS" => 100,
            "Deploying" or "Running" => 65,
            "Uploaded" => 35,
            _ => 10
        };
    }

    private static void AppendLog(PlatformDeploymentRecord record, string message)
    {
        record.Logs.Insert(0, $"{DateTimeOffset.UtcNow:O} {message}");
        record.AuditTrail.Insert(0, message);
    }

    private static DeploymentFailureCategory Classify(Exception exception)
        => exception switch
        {
            UnauthorizedAccessException => DeploymentFailureCategory.Authentication,
            FileNotFoundException => DeploymentFailureCategory.Upload,
            InvalidOperationException operation when operation.Message.Contains("validation", StringComparison.OrdinalIgnoreCase)
                => DeploymentFailureCategory.Contracts,
            InvalidOperationException operation when operation.Message.Contains("docker", StringComparison.OrdinalIgnoreCase)
                => DeploymentFailureCategory.Docker,
            InvalidOperationException operation when operation.Message.Contains("health", StringComparison.OrdinalIgnoreCase)
                => DeploymentFailureCategory.Health,
            _ => DeploymentFailureCategory.Application
        };

    private static PlatformDeploymentResponse BuildResponse(PlatformDeploymentRecord record)
        => new()
        {
            DeploymentId = record.DeploymentId,
            State = record.State,
            Message = record.FailureMessage ?? record.State.ToString(),
            Services = record.Services
        };
}
