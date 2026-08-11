using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/evidence-binder")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class EvidenceBinderController : ControllerBase
{
    private readonly IMediator _mediator;

    public EvidenceBinderController(IMediator mediator) => _mediator = mediator;

    [HttpGet("summary")]
    public Task<IActionResult> GetSummary([FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceBinderSummaryQuery(apiVersion, frameworkId), cancellationToken);
    }

    [HttpGet("chapters")]
    public Task<IActionResult> GetChapters([FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceBinderChaptersQuery(apiVersion, frameworkId), cancellationToken);
    }

    [HttpGet("chapters/{chapterId:guid}/objectives")]
    public Task<IActionResult> GetObjectives(Guid chapterId, [FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceBinderObjectivesQuery(apiVersion, frameworkId, chapterId), cancellationToken);
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/overview")]
    public Task<IActionResult> GetOverview(Guid objectiveElementId, [FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceBinderOverviewQuery(apiVersion, frameworkId, objectiveElementId), cancellationToken);
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/evidences")]
    public Task<IActionResult> GetEvidences(
        Guid objectiveElementId,
        [FromQuery] Guid frameworkId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetEvidenceBinderEvidencesQuery(apiVersion, frameworkId, objectiveElementId, page, pageSize),
            cancellationToken);
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/checklist")]
    public Task<IActionResult> GetChecklist(Guid objectiveElementId, [FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceBinderChecklistQuery(apiVersion, frameworkId, objectiveElementId), cancellationToken);
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/ai-mapping")]
    public Task<IActionResult> GetAiMapping(Guid objectiveElementId, [FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceBinderAiMappingQuery(apiVersion, frameworkId, objectiveElementId), cancellationToken);
    }

    [HttpGet("objective-elements/{objectiveElementId:guid}/history")]
    public Task<IActionResult> GetHistory(Guid objectiveElementId, [FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceBinderHistoryQuery(apiVersion, frameworkId, objectiveElementId), cancellationToken);
    }

    [HttpPost("objective-elements/{objectiveElementId:guid}/evidences")]
    public Task<IActionResult> PostEvidence(Guid objectiveElementId, [FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new PostEvidenceBinderOeEvidenceMultipartCommand(frameworkId, objectiveElementId, apiVersion), cancellationToken);
    }
}
