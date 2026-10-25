using Aovias.Orchestration.Contracts;
using Aovias.Orchestration.Contracts.Events;
using Aovias.Orchestration.Contracts.Workflows;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Workflows.Queries;

/// <summary>
/// Gets all workflows.
/// </summary>
public sealed record GetWorkflowsQuery : IRequest<IReadOnlyList<WorkflowInstance>>;

public sealed class GetWorkflowsQueryHandler : IRequestHandler<GetWorkflowsQuery, IReadOnlyList<WorkflowInstance>>
{
    private readonly IExecutionEngine executionEngine;

    public GetWorkflowsQueryHandler(IExecutionEngine executionEngine) => this.executionEngine = executionEngine;

    public Task<IReadOnlyList<WorkflowInstance>> Handle(GetWorkflowsQuery request, CancellationToken cancellationToken)
        => executionEngine.ListAsync(cancellationToken);
}

/// <summary>
/// Gets a workflow by identifier.
/// </summary>
public sealed record GetWorkflowByIdQuery(string WorkflowId) : IRequest<WorkflowInstance?>;

public sealed class GetWorkflowByIdQueryHandler : IRequestHandler<GetWorkflowByIdQuery, WorkflowInstance?>
{
    private readonly IExecutionEngine executionEngine;

    public GetWorkflowByIdQueryHandler(IExecutionEngine executionEngine) => this.executionEngine = executionEngine;

    public Task<WorkflowInstance?> Handle(GetWorkflowByIdQuery request, CancellationToken cancellationToken)
        => executionEngine.GetAsync(request.WorkflowId, cancellationToken);
}

/// <summary>
/// Gets workflow events for a workflow.
/// </summary>
public sealed record GetWorkflowEventsQuery(string WorkflowId) : IRequest<IReadOnlyList<WorkflowEvent>>;

public sealed class GetWorkflowEventsQueryHandler : IRequestHandler<GetWorkflowEventsQuery, IReadOnlyList<WorkflowEvent>>
{
    private readonly IExecutionEngine executionEngine;

    public GetWorkflowEventsQueryHandler(IExecutionEngine executionEngine) => this.executionEngine = executionEngine;

    public Task<IReadOnlyList<WorkflowEvent>> Handle(GetWorkflowEventsQuery request, CancellationToken cancellationToken)
        => executionEngine.GetEventsAsync(request.WorkflowId, cancellationToken);
}
