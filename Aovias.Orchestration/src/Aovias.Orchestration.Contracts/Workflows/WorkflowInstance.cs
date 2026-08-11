using Aovias.Orchestration.Contracts.Enums;

namespace Aovias.Orchestration.Contracts.Workflows;

/// <summary>
/// Workflow step definition within a workflow template.
/// </summary>
public sealed class WorkflowStepDefinition
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public int Order { get; init; }
}

/// <summary>
/// Workflow instance tracked by the orchestration engine.
/// </summary>
public sealed record WorkflowInstance
{
    public required string WorkflowId { get; init; }

    public required string WorkflowName { get; init; }

    public required WorkflowType WorkflowType { get; init; }

    public required string WorkflowVersion { get; init; }

    public required IReadOnlyList<WorkflowStepDefinition> Steps { get; init; }

    public required WorkflowState Status { get; init; }

    public required string CorrelationId { get; init; }

    public string? CurrentStep { get; init; }

    public int CompletedSteps { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public TimeSpan? Duration { get; init; }

    public string? FailureReason { get; init; }

    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}
