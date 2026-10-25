using Gateway.Application.Commands;
using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/evidence")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class EvidenceLibraryController : ControllerBase
{
    private readonly IMediator _mediator;

    public EvidenceLibraryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("summary")]
    public Task<IActionResult> GetSummary([FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceSummaryQuery(apiVersion, frameworkId), cancellationToken);
    }

    [HttpGet("list")]
    public Task<IActionResult> GetList(
        [FromQuery] Guid frameworkId,
        [FromQuery] Guid? objectiveElementId,
        [FromQuery] Guid? chapterId,
        [FromQuery] Guid? standardId,
        [FromQuery] string? status,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? fileType,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetEvidenceListQuery(
                apiVersion,
                frameworkId,
                objectiveElementId,
                chapterId,
                standardId,
                status,
                departmentId,
                fileType,
                search,
                page,
                pageSize,
                sortBy,
                sortOrder),
            cancellationToken);
    }

    [HttpGet("analytics")]
    public Task<IActionResult> GetAnalytics([FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceAnalyticsQuery(apiVersion, frameworkId), cancellationToken);
    }

    [HttpGet("recent-activity")]
    public Task<IActionResult> GetRecentActivity(
        [FromQuery] Guid frameworkId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceRecentActivityQuery(apiVersion, frameworkId, take), cancellationToken);
    }

    [HttpGet("dashboard")]
    public Task<IActionResult> GetDashboard([FromQuery] Guid frameworkId, CancellationToken cancellationToken)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceDashboardQuery(apiVersion, frameworkId), cancellationToken);
    }

    [HttpGet("upload-context")]
    public Task<IActionResult> GetUploadContext([FromQuery] Guid frameworkId, CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetEvidenceUploadContextQuery(apiVersion, frameworkId), cancellationToken);
    }

    [HttpGet("{evidenceId:guid}/detail")]
    public Task<IActionResult> GetEvidenceDetail(
        Guid evidenceId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetLibraryEvidenceDetailQuery(apiVersion, frameworkId, evidenceId), cancellationToken);
    }

    [HttpGet("{evidenceId:guid}/download")]
    public Task<IActionResult> DownloadEvidence(
        Guid evidenceId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetLibraryEvidenceDownloadQuery(apiVersion, frameworkId, evidenceId), cancellationToken);
    }

    [HttpGet("review-queue")]
    public Task<IActionResult> GetReviewQueue(
        [FromQuery] Guid frameworkId,
        [FromQuery] string? status,
        [FromQuery] string? stage,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? ownerId,
        [FromQuery] string? evidenceType,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetEvidenceReviewQueueQuery(
                apiVersion,
                frameworkId,
                status,
                stage,
                departmentId,
                ownerId,
                evidenceType,
                search,
                page,
                pageSize),
            cancellationToken);
    }

    [HttpGet("{evidenceId:guid}/audit-trail")]
    public Task<IActionResult> GetAuditTrail(
        Guid evidenceId,
        [FromQuery] Guid frameworkId,
        [FromQuery] Guid? objectiveElementId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(
            new GetEvidenceAuditTrailQuery(apiVersion, frameworkId, evidenceId, objectiveElementId),
            cancellationToken);
    }

    [HttpPost("{evidenceId:guid}/approval")]
    public Task<IActionResult> PostApproval(
        Guid evidenceId,
        [FromBody] object body,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new PostEvidenceApprovalCommand(apiVersion, evidenceId, body), cancellationToken);
    }
}
