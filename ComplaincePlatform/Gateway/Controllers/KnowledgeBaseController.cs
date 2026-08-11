using System.Text.Json;
using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Proxies KnowledgeBase (PRAMAN cognitive engine) APIs. Public path uses <c>knowledge-base</c> prefix;
/// downstream paths match KnowledgeBase <c>/api/v1.0/...</c> routes.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/knowledge-base")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class KnowledgeBaseController : ControllerBase
{
    private readonly IMediator _mediator;

    public KnowledgeBaseController(IMediator mediator) => _mediator = mediator;

    private string ApiVersion => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    [HttpGet("frameworks")]
    public Task<IActionResult> ListFrameworks(CancellationToken cancellationToken) =>
        _mediator.Send(new ListKnowledgeBaseFrameworksQuery(ApiVersion), cancellationToken);

    [HttpGet("frameworks/{id:guid}")]
    public Task<IActionResult> GetFramework(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseFrameworkQuery(id, ApiVersion), cancellationToken);

    [HttpGet("nodes")]
    public Task<IActionResult> ListNodes([FromQuery] Guid frameworkId, CancellationToken cancellationToken) =>
        _mediator.Send(new ListKnowledgeBaseNodesQuery(frameworkId, ApiVersion), cancellationToken);

    [HttpGet("nodes/{id:guid}")]
    public Task<IActionResult> GetNode(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseNodeQuery(id, ApiVersion), cancellationToken);

    [HttpGet("nodes/{id:guid}/children")]
    public Task<IActionResult> GetNodeChildren(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseNodeChildrenQuery(id, ApiVersion), cancellationToken);

    [HttpGet("nodes/{id:guid}/details")]
    public Task<IActionResult> GetNodeDetails(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseNodeDetailsQuery(id, ApiVersion), cancellationToken);

    [HttpGet("nodes/code/{nodeCode}/details")]
    public Task<IActionResult> GetNodeDetailsByCode(string nodeCode, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseNodeDetailsByCodeQuery(nodeCode, ApiVersion), cancellationToken);

    [HttpGet("nodes/{nodeId:guid}/decisions")]
    public Task<IActionResult> GetNodeDecisions(Guid nodeId, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseNodeDecisionsQuery(nodeId, ApiVersion), cancellationToken);

    [HttpGet("knowledge/node/{nodeId:guid}")]
    public Task<IActionResult> GetKnowledgeByNode(Guid nodeId, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseKnowledgeByNodeQuery(nodeId, ApiVersion), cancellationToken);

    [HttpGet("knowledge/node/code/{nodeCode}")]
    public Task<IActionResult> GetKnowledgeByNodeCode(string nodeCode, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseKnowledgeByNodeCodeQuery(nodeCode, ApiVersion), cancellationToken);

    [HttpGet("knowledge/search")]
    public Task<IActionResult> SearchKnowledge(
        [FromQuery] string? q,
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken) =>
        _mediator.Send(new SearchKnowledgeBaseKnowledgeQuery(q, frameworkId, ApiVersion), cancellationToken);

    [HttpPost("ai/ask")]
    public Task<IActionResult> AiAsk([FromBody] JsonElement body, CancellationToken cancellationToken) =>
        _mediator.Send(new PostKnowledgeBaseAiAskCommand(body, ApiVersion), cancellationToken);

    [HttpPost("decisions")]
    public Task<IActionResult> CreateDecision([FromBody] JsonElement body, CancellationToken cancellationToken) =>
        _mediator.Send(new PostKnowledgeBaseDecisionCommand(body, ApiVersion), cancellationToken);

    [HttpPost("assessments")]
    public Task<IActionResult> CreateAssessment([FromBody] JsonElement body, CancellationToken cancellationToken) =>
        _mediator.Send(new PostKnowledgeBaseAssessmentCommand(body, ApiVersion), cancellationToken);

    [HttpPut("assessments/{id:guid}/items/{nodeId:guid}")]
    public Task<IActionResult> UpsertAssessmentItem(
        Guid id,
        Guid nodeId,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken) =>
        _mediator.Send(new PutKnowledgeBaseAssessmentItemCommand(id, nodeId, body, ApiVersion), cancellationToken);

    [HttpGet("assessments/{id:guid}/summary")]
    public Task<IActionResult> GetAssessmentSummary(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseAssessmentSummaryQuery(id, ApiVersion), cancellationToken);

    [HttpPost("assessments/{id:guid}/gap-analysis")]
    public Task<IActionResult> GenerateGapAnalysis(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new PostKnowledgeBaseGapAnalysisCommand(id, ApiVersion), cancellationToken);

    [HttpGet("assessments/{id:guid}/gap-analysis")]
    public Task<IActionResult> GetGapAnalysis(Guid id, CancellationToken cancellationToken) =>
        _mediator.Send(new GetKnowledgeBaseGapAnalysisByAssessmentQuery(id, ApiVersion), cancellationToken);
}
