using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Providers;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Platform.Core;
using Aovias.Release.Contracts.Audit;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Synchronizes release records with orchestration workflow execution.
/// </summary>
public sealed class DeploymentWorkflowCoordinator : IDeploymentWorkflowCoordinator
{
    private readonly IReleaseRepository releaseRepository;
    private readonly IReleaseStateMachine stateMachine;
    private readonly IReleaseAuditStore auditStore;
    private readonly IServiceProvider serviceProvider;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;
    private readonly ILogger<DeploymentWorkflowCoordinator> logger;

    public DeploymentWorkflowCoordinator(
        IReleaseRepository releaseRepository,
        IReleaseStateMachine stateMachine,
        IReleaseAuditStore auditStore,
        IServiceProvider serviceProvider,
        IOptionsMonitor<DeploymentSettings> deploymentSettings,
        ILogger<DeploymentWorkflowCoordinator> logger)
    {
        this.releaseRepository = releaseRepository;
        this.stateMachine = stateMachine;
        this.auditStore = auditStore;
        this.serviceProvider = serviceProvider;
        this.deploymentSettings = deploymentSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task OnWorkflowStartedAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(instance, out var releaseId))
        {
            return;
        }

        await UpdateReleaseStateAsync(releaseId, ReleaseState.Deploying, null, cancellationToken);
        await AppendAuditAsync(releaseId, AuditEventType.DeploymentStarted, "Deployment workflow started.", instance, cancellationToken);
    }

    /// <inheritdoc />
    public async Task OnWorkflowCompletedAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(instance, out var releaseId))
        {
            return;
        }

        if (instance.WorkflowType == WorkflowType.Rollback)
        {
            await UpdateReleaseStateAsync(releaseId, ReleaseState.RolledBack, null, cancellationToken);
            await AppendAuditAsync(releaseId, AuditEventType.RollbackCompleted, "Rollback workflow completed.", instance, cancellationToken);
            return;
        }

        await UpdateReleaseStateAsync(releaseId, ReleaseState.Completed, null, cancellationToken);
        await AppendAuditAsync(releaseId, AuditEventType.DeploymentCompleted, "Deployment completed successfully.", instance, cancellationToken);
    }

    /// <inheritdoc />
    public async Task OnWorkflowFailedAsync(WorkflowInstance instance, string reason, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(instance, out var releaseId))
        {
            return;
        }

        await UpdateReleaseStateAsync(releaseId, ReleaseState.Failed, reason, cancellationToken);
        await AppendAuditAsync(releaseId, AuditEventType.PipelineStepFailed, reason, instance, cancellationToken);

        if (instance.WorkflowType == WorkflowType.Deployment && deploymentSettings.CurrentValue.AutomaticRollbackEnabled)
        {
            await OnRollbackTriggeredAsync(instance, reason, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task OnRollbackTriggeredAsync(WorkflowInstance instance, string reason, CancellationToken cancellationToken = default)
    {
        if (!TryGetReleaseId(instance, out var releaseId))
        {
            return;
        }

        await UpdateReleaseStateAsync(releaseId, ReleaseState.RollingBack, reason, cancellationToken);
        await AppendAuditAsync(releaseId, AuditEventType.RollbackStarted, $"Automatic rollback started: {reason}", instance, cancellationToken);

        var parameters = new Dictionary<string, string>(instance.Parameters, StringComparer.OrdinalIgnoreCase)
        {
            ["rollbackReason"] = reason
        };

        var context = PlatformExecutionContext.Create(
            instance.Parameters.GetValueOrDefault("requestedBy") ?? "system",
            "runtime",
            "production",
            cancellationToken,
            instance.CorrelationId);

        var executionEngine = serviceProvider.GetRequiredService<IExecutionEngine>();
        await executionEngine.EnqueueAsync(WorkflowType.Rollback, parameters, context, cancellationToken);
    }

    private static bool TryGetReleaseId(WorkflowInstance instance, out string releaseId)
    {
        if (instance.Parameters.TryGetValue("releaseId", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            releaseId = value;
            return true;
        }

        releaseId = string.Empty;
        return false;
    }

    private async Task UpdateReleaseStateAsync(
        string releaseId,
        ReleaseState targetState,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        var release = await releaseRepository.GetAsync(releaseId, cancellationToken);
        if (release is null)
        {
            return;
        }

        var nextState = stateMachine.CanTransition(release.State, targetState)
            ? stateMachine.Transition(release.State, targetState)
            : targetState;

        release = release with
        {
            State = nextState,
            UpdatedAt = DateTimeOffset.UtcNow,
            FailureReason = failureReason
        };

        await releaseRepository.SaveAsync(release, cancellationToken);
        logger.LogInformation("Release {ReleaseId} moved to state {State}.", releaseId, nextState);
    }

    private async Task AppendAuditAsync(
        string releaseId,
        AuditEventType eventType,
        string message,
        WorkflowInstance instance,
        CancellationToken cancellationToken)
    {
        var release = await releaseRepository.GetAsync(releaseId, cancellationToken);
        if (release is null)
        {
            return;
        }

        await auditStore.AppendAsync(new AuditEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            ReleaseId = releaseId,
            PackageId = release.Identity.PackageId,
            Actor = instance.Parameters.GetValueOrDefault("requestedBy") ?? "system",
            Timestamp = DateTimeOffset.UtcNow,
            Message = message,
            DeploymentId = instance.Parameters.GetValueOrDefault("deploymentId") ?? releaseId,
            CorrelationId = instance.CorrelationId,
            Metadata = new Dictionary<string, string>
            {
                ["workflowId"] = instance.WorkflowId
            }
        }, cancellationToken);
    }
}
