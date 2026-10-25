using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Workflows;

namespace Aovias.Orchestration.Engine;

/// <summary>
/// Registry of workflow templates supplied by providers.
/// </summary>
public sealed class WorkflowRegistry : IWorkflowRegistry
{
    private readonly IReadOnlyDictionary<WorkflowType, IWorkflow> workflows;

    public WorkflowRegistry(IEnumerable<IWorkflowProvider> providers)
    {
        workflows = providers
            .SelectMany(provider => provider.GetWorkflows())
            .GroupBy(workflow => workflow.WorkflowType)
            .ToDictionary(group => group.Key, group => group.First());
    }

    /// <inheritdoc />
    public IWorkflow Get(WorkflowType workflowType)
        => workflows.TryGetValue(workflowType, out var workflow)
            ? workflow
            : throw new KeyNotFoundException($"Workflow type '{workflowType}' is not registered.");

    /// <inheritdoc />
    public IReadOnlyList<IWorkflow> List() => workflows.Values.OrderBy(workflow => workflow.WorkflowName).ToList();
}
