using Aovias.Runtime.Contracts.Deployment;
using Aovias.Runtime.Contracts.Observability;

namespace Aovias.Runtime.SDK;

/// <summary>
/// Real-time SignalR client for the AOVIAS Runtime Agent.
/// </summary>
public interface IRuntimeHubClient
{
    event EventHandler<WorkflowProgressMessage>? WorkflowProgressReceived;

    event EventHandler<WorkflowProgressMessage>? DeploymentProgressReceived;

    event EventHandler<WorkflowProgressMessage>? RollbackProgressReceived;

    event EventHandler<DeploymentPhaseEventDto>? DeploymentPhaseReceived;

    event EventHandler<DeploymentOperatorErrorDto>? DeploymentFailureReceived;

    event EventHandler<RuntimeHealthMessage>? RuntimeHealthReceived;

    event EventHandler<WorkflowEventMessage>? WorkflowEventReceived;

    event EventHandler<RuntimeStateDto>? RuntimeStateReceived;

    event EventHandler<RuntimeTimelineEntryDto>? RuntimeTimelineReceived;

    event EventHandler<RuntimeMetricsSnapshotDto>? RuntimeMetricsReceived;

    event EventHandler<RuntimeTimelineEntryDto>? ContainerEventReceived;

    event EventHandler<RuntimeTimelineEntryDto>? WorkflowUpdateReceived;

    event EventHandler<RuntimeTimelineEntryDto>? DeploymentUpdateReceived;

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Diagnostics from the most recent connection attempt.
    /// </summary>
    RuntimeHubConnectionDiagnostics? LastConnectionDiagnostics { get; }
}

/// <summary>
/// Workflow progress payload from SignalR.
/// </summary>
public sealed class WorkflowProgressMessage
{
    public required string WorkflowId { get; init; }

    public string? ReleaseId { get; init; }

    public required string EventType { get; init; }

    public string? StepName { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public string? CurrentStep { get; init; }

    public int CompletedSteps { get; init; }

    public int TotalSteps { get; init; }

    public string? RollbackStatus { get; init; }

    public string? ServiceId { get; init; }

    public string? DeploymentId { get; init; }

    public string? FailureCategory { get; init; }

    public string? RecoveryAction { get; init; }

    public int ProgressPercent { get; init; }
}

/// <summary>
/// Runtime health payload from SignalR.
/// </summary>
public sealed class RuntimeHealthMessage
{
    public required string Status { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Workflow event payload from SignalR.
/// </summary>
public sealed class WorkflowEventMessage
{
    public required string EventType { get; init; }

    public required string WorkflowId { get; init; }

    public string? StepName { get; init; }

    public required string Message { get; init; }

    public required DateTimeOffset Timestamp { get; init; }
}
