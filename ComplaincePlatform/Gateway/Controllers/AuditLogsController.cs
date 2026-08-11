using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IMediator mediator,
        ILogger<AuditLogsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetAuditLogsQuery(apiVersion, entityType, entityId, from, to),
            cancellationToken);
    }

    [HttpGet("{entityType}/{entityId:guid}")]
    public Task<IActionResult> GetAuditLogsForEntity(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetAuditLogsForEntityQuery(apiVersion, entityType, entityId),
            cancellationToken);
    }
}
