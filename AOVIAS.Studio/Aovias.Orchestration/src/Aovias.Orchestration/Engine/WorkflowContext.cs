using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Platform.Core;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;

namespace Aovias.Orchestration.Engine;

/// <summary>
/// Mutable workflow execution context.
/// </summary>
public sealed class WorkflowContext : IWorkflowContext
{
    private readonly Dictionary<string, string> parameters;

    public WorkflowContext(
        WorkflowInstance instance,
        PlatformExecutionContext execution,
        IReadOnlyDictionary<string, string> parameters)
    {
        Instance = instance;
        Execution = execution;
        this.parameters = new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public WorkflowInstance Instance { get; private set; }

    /// <inheritdoc />
    public PlatformExecutionContext Execution { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Parameters => parameters;

    /// <inheritdoc />
    public void SetParameter(string key, string value) => parameters[key] = value;

    internal void UpdateInstance(WorkflowInstance instance) => Instance = instance;
}
