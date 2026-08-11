using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Platform.Core;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;

namespace Aovias.Orchestration.Contracts;

/// <summary>
/// Mutable workflow execution context passed through pipeline steps.
/// </summary>
public interface IWorkflowContext
{
    WorkflowInstance Instance { get; }

    PlatformExecutionContext Execution { get; }

    IReadOnlyDictionary<string, string> Parameters { get; }

    void SetParameter(string key, string value);
}

/// <summary>
/// Workflow template contract.
/// </summary>
public interface IWorkflow
{
    string WorkflowName { get; }

    WorkflowType WorkflowType { get; }

    string WorkflowVersion { get; }

    IReadOnlyList<WorkflowStepDefinition> Steps { get; }
}

/// <summary>
/// Executable workflow step.
/// </summary>
public interface IWorkflowStep
{
    string Name { get; }

    Task<ExecutionResult> ExecuteAsync(IWorkflowContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Executes a registered workflow through its pipeline.
/// </summary>
public interface IWorkflowExecutor
{
    Task<WorkflowInstance> ExecuteAsync(
        IWorkflow workflow,
        IReadOnlyDictionary<string, string> parameters,
        PlatformExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Supplies workflow templates to the registry.
/// </summary>
public interface IWorkflowProvider
{
    IEnumerable<IWorkflow> GetWorkflows();
}

/// <summary>
/// Registry of available workflow templates.
/// </summary>
public interface IWorkflowRegistry
{
    IWorkflow Get(WorkflowType workflowType);

    IReadOnlyList<IWorkflow> List();
}

/// <summary>
/// Orchestration execution engine for queuing and running workflows.
/// </summary>
public interface IExecutionEngine
{
    Task<WorkflowInstance> QueueAsync(
        WorkflowType workflowType,
        IReadOnlyDictionary<string, string> parameters,
        PlatformExecutionContext executionContext,
        CancellationToken cancellationToken = default);

    Task<WorkflowInstance?> GetAsync(string workflowId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowInstance>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowEvent>> GetEventsAsync(string workflowId, CancellationToken cancellationToken = default);

    Task<WorkflowInstance> EnqueueAsync(
        WorkflowType workflowType,
        IReadOnlyDictionary<string, string> parameters,
        PlatformExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}
