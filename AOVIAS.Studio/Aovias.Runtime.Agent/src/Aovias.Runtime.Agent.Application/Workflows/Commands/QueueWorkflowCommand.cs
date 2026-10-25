using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Enums;
using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Workflows;
using Aovias.Platform.Core;
using PlatformExecutionContext = Aovias.Platform.Core.ExecutionContext;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Workflows.Commands;

/// <summary>
/// Queues a workflow for execution.
/// </summary>
public sealed record QueueWorkflowCommand(
    WorkflowType WorkflowType,
    IReadOnlyDictionary<string, string> Parameters,
    string RequestedBy,
    string Runtime,
    string Environment) : IRequest<WorkflowInstance>;

/// <summary>
/// Handles <see cref="QueueWorkflowCommand"/>.
/// </summary>
public sealed class QueueWorkflowCommandHandler : IRequestHandler<QueueWorkflowCommand, WorkflowInstance>
{
    private readonly IExecutionEngine executionEngine;

    public QueueWorkflowCommandHandler(IExecutionEngine executionEngine)
    {
        this.executionEngine = executionEngine;
    }

    /// <inheritdoc />
    public Task<WorkflowInstance> Handle(QueueWorkflowCommand request, CancellationToken cancellationToken)
    {
        var context = PlatformExecutionContext.Create(
            request.RequestedBy,
            request.Runtime,
            request.Environment,
            cancellationToken);

        return executionEngine.QueueAsync(request.WorkflowType, request.Parameters, context, cancellationToken);
    }
}
