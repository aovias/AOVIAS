using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.PackageBuilder.Abstractions;
using Aovias.Platform.Core;
using Aovias.Release.Contracts;
using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Identity;
using Aovias.Release.Contracts.Packages;
using Aovias.Release.Contracts.Releases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// Orchestrates release lifecycle operations for the AOVIAS platform.
/// </summary>
public sealed class ReleaseManagerService : Abstractions.IReleaseManager
{
    private readonly Abstractions.IReleaseRepository releaseRepository;
    private readonly Abstractions.IReleaseValidator releaseValidator;
    private readonly Abstractions.IReleasePlanner releasePlanner;
    private readonly Abstractions.IReleaseRollbackService rollbackService;
    private readonly Abstractions.IReleaseHistoryProvider releaseHistoryProvider;
    private readonly Abstractions.IReleaseAuditStore auditStore;
    private readonly Abstractions.IReleaseStateMachine stateMachine;
    private readonly Abstractions.IReleaseIdentityGenerator releaseIdentityGenerator;
    private readonly IPackageReader packageReader;
    private readonly IExecutionEngine executionEngine;
    private readonly IDeploymentPathResolver pathResolver;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;
    private readonly IOptionsMonitor<DockerSettings> dockerSettings;
    private readonly ILogger<ReleaseManagerService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseManagerService"/> class.
    /// </summary>
    public ReleaseManagerService(
        Abstractions.IReleaseRepository releaseRepository,
        Abstractions.IReleaseValidator releaseValidator,
        Abstractions.IReleasePlanner releasePlanner,
        Abstractions.IReleaseRollbackService rollbackService,
        Abstractions.IReleaseHistoryProvider releaseHistoryProvider,
        Abstractions.IReleaseAuditStore auditStore,
        Abstractions.IReleaseStateMachine stateMachine,
        Abstractions.IReleaseIdentityGenerator releaseIdentityGenerator,
        IPackageReader packageReader,
        IExecutionEngine executionEngine,
        IDeploymentPathResolver pathResolver,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        IOptionsMonitor<DockerSettings> dockerSettings,
        ILogger<ReleaseManagerService> logger)
    {
        this.releaseRepository = releaseRepository;
        this.releaseValidator = releaseValidator;
        this.releasePlanner = releasePlanner;
        this.rollbackService = rollbackService;
        this.releaseHistoryProvider = releaseHistoryProvider;
        this.auditStore = auditStore;
        this.stateMachine = stateMachine;
        this.releaseIdentityGenerator = releaseIdentityGenerator;
        this.packageReader = packageReader;
        this.executionEngine = executionEngine;
        this.pathResolver = pathResolver;
        this.releaseSettings = releaseSettings;
        this.deploymentSettings = deploymentSettings;
        this.dockerSettings = dockerSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<UploadPackageResult> UploadPackageAsync(UploadPackageRequest request, CancellationToken cancellationToken = default)
    {
        DeploymentExecutionDiagnostics.LogPipelineStage(
            "Service",
            nameof(ReleaseManagerService),
            nameof(UploadPackageAsync));
        DeploymentExecutionDiagnostics.LogConfigurationSnapshot(
            releaseSettings.CurrentValue,
            pathResolver,
            "ReleaseManagerService.UploadPackageAsync");

        await releaseRepository.EnsureDirectoriesAsync(cancellationToken);

        if (!File.Exists(request.PackagePath))
        {
            throw new FileNotFoundException("Package file was not found.", request.PackagePath);
        }

        var contents = await packageReader.ReadAsync(request.PackagePath, cancellationToken);
        var timestamp = DateTimeOffset.UtcNow;
        var releaseId = releaseIdentityGenerator.GenerateReleaseId(timestamp);
        var settings = releaseSettings.CurrentValue;
        var extension = Path.GetExtension(request.PackagePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = PackageLayout.Extension;
        }

        var destinationPath = Path.Combine(pathResolver.IncomingRoot, $"{releaseId}{extension}");

        File.Copy(request.PackagePath, destinationPath, overwrite: true);

        var record = new ReleaseRecord
        {
            Identity = new ReleaseIdentity
            {
                ReleaseId = releaseId,
                PackageId = contents.Manifest.PackageId,
                PackageVersion = contents.Manifest.PackageVersion,
                Runtime = request.Runtime,
                Environment = request.Environment,
                Timestamp = timestamp
            },
            State = ReleaseState.Uploaded,
            PackagePath = destinationPath,
            Manifest = contents.Manifest,
            UpdatedAt = timestamp
        };

        await releaseRepository.SaveAsync(record, cancellationToken);
        await AppendAuditAsync(
            AuditEventType.PackageUploaded,
            record,
            request.UploadedBy,
            $"Package {contents.Manifest.PackageId} uploaded as release {releaseId}.",
            cancellationToken);

        logger.LogInformation("Uploaded release {ReleaseId} for package {PackageId}", releaseId, contents.Manifest.PackageId);

        return new UploadPackageResult
        {
            ReleaseId = releaseId,
            PackageId = contents.Manifest.PackageId,
            PackagePath = destinationPath,
            State = record.State.ToString()
        };
    }

    /// <inheritdoc />
    public async Task<PackageValidationResult> ValidatePackageAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(releaseId, cancellationToken);
        if (ReleaseStatePolicy.HasCompletedPackageValidation(release.State))
        {
            return await releaseValidator.ValidateReleaseAsync(release, cancellationToken);
        }

        var validation = await releaseValidator.ValidateReleaseAsync(release, cancellationToken);

        var nextState = validation.IsValid
            ? stateMachine.Transition(release.State, ReleaseState.Validated)
            : ReleaseState.Failed;

        if (validation.IsValid)
        {
            nextState = stateMachine.Transition(nextState, ReleaseState.Compatible);
        }

        await SaveStateAsync(release, nextState, validation.IsValid ? null : string.Join("; ", validation.Errors), cancellationToken);

        await AppendAuditAsync(
            AuditEventType.PackageValidated,
            release,
            "system",
            validation.IsValid ? "Package validation succeeded." : "Package validation failed.",
            cancellationToken);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Release {releaseId} failed package validation: {string.Join("; ", validation.Errors)}");
        }

        return validation;
    }

    /// <inheritdoc />
    public async Task<DeploymentPlan> GenerateDeploymentPlanAsync(PlanDeploymentRequest request, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(request.ReleaseId, cancellationToken);

        if (!ReleaseStatePolicy.IsReadyForPlanning(release.State))
        {
            throw new InvalidOperationException(
                $"Release {request.ReleaseId} must be validated and compatible before planning. "
                + $"Current state: {release.State}. "
                + "Call ValidatePackageAsync and ensure it succeeds before GenerateDeploymentPlanAsync.");
        }

        var plan = await releasePlanner.CreatePlanAsync(release, cancellationToken);
        var nextState = plan.CompatibilityPassed
            ? stateMachine.Transition(release.State, ReleaseState.Staged)
            : ReleaseState.Failed;

        release = release with
        {
            State = nextState,
            LastPlan = plan,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailureReason = plan.CompatibilityPassed ? null : "Compatibility checks failed."
        };

        await releaseRepository.SaveAsync(release, cancellationToken);
        await AppendAuditAsync(
            AuditEventType.DeploymentPlanned,
            release,
            request.RequestedBy,
            $"Deployment plan {plan.PlanId} generated.",
            cancellationToken);

        return plan;
    }

    /// <inheritdoc />
    public async Task<DeployReleaseResult> DeployAsync(DeployReleaseRequest request, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(request.ReleaseId, cancellationToken);

        if (release.State != ReleaseState.Staged)
        {
            throw new InvalidOperationException($"Release {request.ReleaseId} must be staged before deployment. Current state: {release.State}.");
        }

        if (release.LastPlan is null)
        {
            throw new InvalidOperationException("A deployment plan must be generated before deployment.");
        }

        if (release.LastPlan.OperatorConfirmationRequired && !request.OperatorConfirmed)
        {
            throw new InvalidOperationException("Operator confirmation is required for this deployment plan.");
        }

        await AppendAuditAsync(
            AuditEventType.DeploymentStarted,
            release,
            request.RequestedBy,
            "Deployment queued for execution.",
            cancellationToken);

        var contents = await packageReader.ReadAsync(release.PackagePath, cancellationToken);
        var deployment = deploymentSettings.CurrentValue;
        var composeFilePath = Path.Combine(dockerSettings.CurrentValue.ComposeRoot, contents.Service.ServiceId, "docker-compose.yml");
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["releaseId"] = release.Identity.ReleaseId,
            ["packagePath"] = release.PackagePath,
            ["requestedBy"] = request.RequestedBy,
            ["serviceId"] = contents.Service.ServiceId,
            ["containerName"] = contents.Service.ContainerName,
            ["composeFile"] = contents.Service.ComposeFile,
            ["composeFilePath"] = composeFilePath,
            ["composeProjectName"] = $"{deployment.ComposeProjectNamePrefix}-{contents.Service.ServiceId}",
            ["healthEndpoint"] = contents.Health.HealthEndpoint,
            ["expectedStatusCode"] = contents.Health.ExpectedStatusCode.ToString(),
            ["healthTimeoutSeconds"] = Math.Max((int)contents.Health.Timeout.TotalSeconds, deployment.HealthCheckTimeoutSeconds).ToString(),
            ["healthRetryCount"] = Math.Max(contents.Health.RetryCount, deployment.HealthCheckRetryCount).ToString()
        };

        var context = PlatformExecutionContext.Create(
            request.RequestedBy,
            release.Identity.Runtime,
            release.Identity.Environment,
            cancellationToken,
            release.Identity.ReleaseId);

        var instance = await executionEngine.EnqueueAsync(WorkflowType.Deployment, parameters, context, cancellationToken);
        logger.LogInformation("Queued deployment workflow {WorkflowId} for release {ReleaseId}.", instance.WorkflowId, release.Identity.ReleaseId);

        return new DeployReleaseResult
        {
            ReleaseId = release.Identity.ReleaseId,
            State = ReleaseState.Deploying.ToString(),
            WorkflowId = instance.WorkflowId,
            Message = $"Deployment workflow {instance.WorkflowId} queued.",
            QueuedForExecution = true
        };
    }

    /// <inheritdoc />
    public async Task<RollbackReleaseResult> RollbackAsync(RollbackReleaseRequest request, CancellationToken cancellationToken = default)
    {
        var release = await RequireReleaseAsync(request.ReleaseId, cancellationToken);

        await AppendAuditAsync(
            AuditEventType.RollbackStarted,
            release,
            request.RequestedBy,
            "Rollback requested.",
            cancellationToken);

        var rollingBack = stateMachine.CanTransition(release.State, ReleaseState.RollingBack)
            ? stateMachine.Transition(release.State, ReleaseState.RollingBack)
            : ReleaseState.RollingBack;

        await SaveStateAsync(release, rollingBack, null, cancellationToken);

        var result = await rollbackService.RollbackAsync(request, release, cancellationToken);
        var rolledBack = stateMachine.Transition(rollingBack, ReleaseState.RolledBack);
        await SaveStateAsync(release, rolledBack, null, cancellationToken);

        await AppendAuditAsync(
            AuditEventType.RollbackCompleted,
            release,
            request.RequestedBy,
            result.Message,
            cancellationToken);

        return new RollbackReleaseResult
        {
            ReleaseId = release.Identity.ReleaseId,
            State = ReleaseState.RolledBack.ToString(),
            Message = result.Message,
            QueuedForExecution = result.QueuedForExecution
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseListItem>> GetReleasesAsync(CancellationToken cancellationToken = default)
    {
        var records = await releaseRepository.ListAsync(cancellationToken);
        return records
            .Select(record => new ReleaseListItem
            {
                ReleaseId = record.Identity.ReleaseId,
                PackageId = record.Identity.PackageId,
                PackageVersion = record.Identity.PackageVersion,
                ApplicationName = record.Manifest.ApplicationName,
                State = record.State,
                UpdatedAt = record.UpdatedAt
            })
            .ToList();
    }

    /// <inheritdoc />
    public Task<ReleaseRecord?> GetReleaseAsync(string releaseId, CancellationToken cancellationToken = default)
        => releaseRepository.GetAsync(releaseId, cancellationToken);

    /// <summary>
    /// Gets release history entries.
    /// </summary>
    public Task<IReadOnlyList<ReleaseHistoryEntry>> GetHistoryAsync(
        ReleaseHistorySearchCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        => releaseHistoryProvider.GetHistoryAsync(criteria, cancellationToken);

    private async Task<ReleaseRecord> RequireReleaseAsync(string releaseId, CancellationToken cancellationToken)
    {
        var release = await releaseRepository.GetAsync(releaseId, cancellationToken);
        return release ?? throw new KeyNotFoundException($"Release '{releaseId}' was not found.");
    }

    private async Task SaveStateAsync(
        ReleaseRecord release,
        ReleaseState state,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        release = release with
        {
            State = state,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailureReason = failureReason
        };

        await releaseRepository.SaveAsync(release, cancellationToken);
    }

    private Task AppendAuditAsync(
        AuditEventType eventType,
        ReleaseRecord release,
        string actor,
        string message,
        CancellationToken cancellationToken)
    {
        return auditStore.AppendAsync(new AuditEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            ReleaseId = release.Identity.ReleaseId,
            PackageId = release.Identity.PackageId,
            Actor = actor,
            Timestamp = DateTimeOffset.UtcNow,
            Message = message
        }, cancellationToken);
    }
}
