using Aovias.Orchestration.Contracts.Enums;
using Aovias.Runtime.Agent.Application.Workflows.Commands;
using Aovias.Runtime.Agent.Application.Workflows.Queries;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aovias.Runtime.Agent.Api.Controllers;

/// <summary>
/// Exposes workflow orchestration endpoints.
/// </summary>
[ApiController]
[Route("api/workflows")]
[Produces("application/json")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IMediator mediator;

    public WorkflowsController(IMediator mediator) => this.mediator = mediator;

    /// <summary>
    /// Queues a workflow for execution.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> QueueAsync([FromBody] QueueWorkflowRequest request, CancellationToken cancellationToken)
    {
        var instance = await mediator.Send(
            new QueueWorkflowCommand(
                request.WorkflowType,
                request.Parameters,
                request.RequestedBy,
                request.Runtime,
                request.Environment),
            cancellationToken);

        return Ok(instance);
    }

    /// <summary>
    /// Gets all workflows.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var workflows = await mediator.Send(new GetWorkflowsQuery(), cancellationToken);
        return Ok(workflows);
    }

    /// <summary>
    /// Gets a workflow by identifier.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
    {
        var workflow = await mediator.Send(new GetWorkflowByIdQuery(id), cancellationToken);
        return workflow is null ? NotFound() : Ok(workflow);
    }

    /// <summary>
    /// Gets workflow events for a workflow.
    /// </summary>
    [HttpGet("{id}/events")]
    public async Task<IActionResult> GetEventsAsync(string id, CancellationToken cancellationToken)
    {
        var events = await mediator.Send(new GetWorkflowEventsQuery(id), cancellationToken);
        return Ok(events);
    }
}

/// <summary>
/// Queue workflow request payload.
/// </summary>
public sealed class QueueWorkflowRequest
{
    public required WorkflowType WorkflowType { get; init; }

    public required IReadOnlyDictionary<string, string> Parameters { get; init; }

    public required string RequestedBy { get; init; }

    public required string Runtime { get; init; }

    public required string Environment { get; init; }
}
