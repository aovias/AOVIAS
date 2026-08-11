using Gateway.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>
/// Gateway controller for reporting APIs.
/// Proxies reporting requests to ComplianceService via MediatR.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class ReportingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportingController> _logger;

    public ReportingController(IMediator mediator, ILogger<ReportingController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("compliance-score")]
    public Task<IActionResult> GetComplianceScoreReport(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetComplianceScoreReportQuery(apiVersion, hospitalId, frameworkId, from, to), cancellationToken);
    }

    [HttpGet("audit-findings")]
    public Task<IActionResult> GetAuditFindingsReport(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetAuditFindingsReportQuery(apiVersion, hospitalId, frameworkId, from, to), cancellationToken);
    }

    [HttpGet("nabh-readiness")]
    public Task<IActionResult> GetNabhReadinessReport(
        [FromQuery] Guid hospitalId,
        CancellationToken cancellationToken = default)
    {
        var apiVersion = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";
        return _mediator.Send(new GetNabhReadinessReportQuery(apiVersion, hospitalId), cancellationToken);
    }

    [HttpGet("compliance")]
    public Task<IActionResult> GetComplianceReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        SendScreenQuery(new GetComplianceScreenReportQuery(ApiVersion(), hospitalId, frameworkId, chapterId, fromDate, toDate), cancellationToken);

    [HttpGet("audit")]
    public Task<IActionResult> GetAuditReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        SendScreenQuery(new GetAuditScreenReportQuery(ApiVersion(), hospitalId, frameworkId, chapterId, departmentId, fromDate, toDate), cancellationToken);

    [HttpGet("tasks")]
    public Task<IActionResult> GetTaskReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        SendScreenQuery(new GetTaskScreenReportQuery(ApiVersion(), hospitalId, frameworkId, departmentId, status, fromDate, toDate), cancellationToken);

    [HttpGet("evidence")]
    public Task<IActionResult> GetEvidenceReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        SendScreenQuery(new GetEvidenceScreenReportQuery(ApiVersion(), hospitalId, frameworkId, chapterId, fromDate, toDate), cancellationToken);

    [HttpGet("readiness")]
    public Task<IActionResult> GetReadinessReport(
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken = default) =>
        SendScreenQuery(new GetReadinessScreenReportQuery(ApiVersion(), hospitalId, frameworkId, organizationId), cancellationToken);

    [HttpPost("custom")]
    public Task<IActionResult> PostCustomReport(
        [FromBody] object body,
        [FromQuery] Guid? hospitalId,
        CancellationToken cancellationToken = default) =>
        SendScreenQuery(new PostCustomScreenReportCommand(ApiVersion(), hospitalId, body), cancellationToken);

    [HttpGet("compliance/export")]
    public Task<IActionResult> ExportCompliance(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        Export("compliance", format, hospitalId, frameworkId, chapterId, null, null, null, fromDate, toDate, cancellationToken);

    [HttpGet("audit/export")]
    public Task<IActionResult> ExportAudit(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] Guid? departmentId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        Export("audit", format, hospitalId, frameworkId, chapterId, departmentId, null, null, fromDate, toDate, cancellationToken);

    [HttpGet("tasks/export")]
    public Task<IActionResult> ExportTasks(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        Export("tasks", format, hospitalId, frameworkId, null, departmentId, status, null, fromDate, toDate, cancellationToken);

    [HttpGet("evidence/export")]
    public Task<IActionResult> ExportEvidence(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] Guid? chapterId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default) =>
        Export("evidence", format, hospitalId, frameworkId, chapterId, null, null, null, fromDate, toDate, cancellationToken);

    [HttpGet("readiness/export")]
    public Task<IActionResult> ExportReadiness(
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        CancellationToken cancellationToken = default) =>
        Export("readiness", format, hospitalId, frameworkId, null, null, null, null, null, null, cancellationToken);

    [HttpPost("custom/export")]
    public Task<IActionResult> ExportCustom(
        [FromBody] object body,
        [FromQuery] string format,
        [FromQuery] Guid? hospitalId,
        CancellationToken cancellationToken = default) =>
        _mediator.Send(new ExportCustomScreenReportCommand(ApiVersion(), format, hospitalId, body), cancellationToken);

    private string ApiVersion() => HttpContext.GetRequestedApiVersion()?.ToString() ?? "1.0";

    private Task<IActionResult> SendScreenQuery(IRequest<IActionResult> query, CancellationToken cancellationToken) =>
        _mediator.Send(query, cancellationToken);

    private Task<IActionResult> Export(
        string reportType,
        string format,
        Guid? hospitalId,
        Guid? frameworkId,
        Guid? chapterId,
        Guid? departmentId,
        string? status,
        string? severity,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken) =>
        _mediator.Send(
            new ExportScreenReportQuery(ApiVersion(), reportType, format, hospitalId, frameworkId, chapterId, departmentId, status, severity, fromDate, toDate),
            cancellationToken);
}
