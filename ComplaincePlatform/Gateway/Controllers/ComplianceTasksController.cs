using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/compliance")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class ComplianceTasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public ComplianceTasksController(IMediator mediator) => _mediator = mediator;

    [HttpGet("compliance-tasks")]
    public Task<IActionResult> List([FromQuery] Guid? frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new ListComplianceTasksQuery(apiVersion, frameworkId), cancellationToken);
    }

    [HttpGet("compliance-tasks/{taskId:guid}")]
    public Task<IActionResult> GetById(Guid taskId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetComplianceTaskByIdQuery(apiVersion, taskId), cancellationToken);
    }

    [HttpPost("compliance-tasks")]
    public Task<IActionResult> Create([FromBody] object requestBody, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new CreateComplianceTaskCommand(apiVersion, requestBody), cancellationToken);
    }

    [HttpPatch("compliance-tasks/{taskId:guid}")]
    public Task<IActionResult> Patch(Guid taskId, [FromBody] object requestBody, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new PatchComplianceTaskCommand(apiVersion, taskId, requestBody), cancellationToken);
    }

    [HttpPut("frameworks/{frameworkId:guid}/tasks/{taskId:guid}/complete")]
    public Task<IActionResult> Complete(Guid frameworkId, Guid taskId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new CompleteComplianceTaskCommand(apiVersion, frameworkId, taskId), cancellationToken);
    }
}
