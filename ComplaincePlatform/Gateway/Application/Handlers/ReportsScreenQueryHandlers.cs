using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public sealed class GetComplianceScreenReportQueryHandler : IRequestHandler<GetComplianceScreenReportQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetComplianceScreenReportQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetComplianceScreenReportQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            ReportsProxyQueryBuilder.BuildEndpoint(request.ApiVersion, "compliance", request.HospitalId, request.FrameworkId, request.ChapterId, null, null, null, request.FromDate, request.ToDate),
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetAuditScreenReportQueryHandler : IRequestHandler<GetAuditScreenReportQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetAuditScreenReportQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetAuditScreenReportQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            ReportsProxyQueryBuilder.BuildEndpoint(request.ApiVersion, "audit", request.HospitalId, request.FrameworkId, request.ChapterId, request.DepartmentId, null, null, request.FromDate, request.ToDate),
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetTaskScreenReportQueryHandler : IRequestHandler<GetTaskScreenReportQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetTaskScreenReportQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetTaskScreenReportQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            ReportsProxyQueryBuilder.BuildEndpoint(request.ApiVersion, "tasks", request.HospitalId, request.FrameworkId, null, request.DepartmentId, request.Status, null, request.FromDate, request.ToDate),
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetEvidenceScreenReportQueryHandler : IRequestHandler<GetEvidenceScreenReportQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetEvidenceScreenReportQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetEvidenceScreenReportQuery request, CancellationToken cancellationToken) =>
        _proxy.ProxyToComplianceServiceAsync(
            ReportsProxyQueryBuilder.BuildEndpoint(request.ApiVersion, "evidence", request.HospitalId, request.FrameworkId, request.ChapterId, null, null, null, request.FromDate, request.ToDate),
            HttpMethod.Get,
            null,
            null,
            cancellationToken);
}

public sealed class GetReadinessScreenReportQueryHandler : IRequestHandler<GetReadinessScreenReportQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public GetReadinessScreenReportQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(GetReadinessScreenReportQuery request, CancellationToken cancellationToken)
    {
        var q = ReportsProxyQueryBuilder.BuildEndpoint(request.ApiVersion, "readiness", request.HospitalId, request.FrameworkId, null, null, null, null, null, null);
        if (request.OrganizationId.HasValue)
        {
            q += $"&organizationId={request.OrganizationId.Value}";
        }

        return _proxy.ProxyToComplianceServiceAsync(q, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class PostCustomScreenReportCommandHandler : IRequestHandler<PostCustomScreenReportCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public PostCustomScreenReportCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(PostCustomScreenReportCommand request, CancellationToken cancellationToken)
    {
        var q = request.HospitalId.HasValue ? $"?hospitalId={request.HospitalId.Value}" : string.Empty;
        var endpoint = $"/api/v{request.ApiVersion}/reports/custom{q}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, request.Body, null, cancellationToken);
    }
}

public sealed class ExportCustomScreenReportCommandHandler : IRequestHandler<ExportCustomScreenReportCommand, IActionResult>
{
    private readonly IProxyService _proxy;
    public ExportCustomScreenReportCommandHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ExportCustomScreenReportCommand request, CancellationToken cancellationToken)
    {
        var q = new List<string> { $"format={Uri.EscapeDataString(request.Format)}" };
        if (request.HospitalId.HasValue)
        {
            q.Add($"hospitalId={request.HospitalId.Value}");
        }

        var endpoint = $"/api/v{request.ApiVersion}/reports/custom/export?{string.Join("&", q)}";
        return _proxy.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, request.Body, null, cancellationToken);
    }
}

public sealed class ExportScreenReportQueryHandler : IRequestHandler<ExportScreenReportQuery, IActionResult>
{
    private readonly IProxyService _proxy;
    public ExportScreenReportQueryHandler(IProxyService proxy) => _proxy = proxy;

    public Task<IActionResult> Handle(ExportScreenReportQuery request, CancellationToken cancellationToken)
    {
        var path = $"{request.ReportType}/export";
        var endpoint = ReportsProxyQueryBuilder.BuildEndpoint(request.ApiVersion, path, request.HospitalId, request.FrameworkId, request.ChapterId, request.DepartmentId, request.Status, request.Severity, request.FromDate, request.ToDate);
        endpoint += $"&format={Uri.EscapeDataString(request.Format)}";
        return _proxy.ProxyComplianceServiceStreamGetAsync(endpoint, cancellationToken);
    }
}

public static class ReportsProxyQueryBuilder
{
    public static string BuildEndpoint(
        string apiVersion,
        string path,
        Guid? hospitalId,
        Guid? frameworkId,
        Guid? chapterId,
        Guid? departmentId,
        string? status,
        string? severity,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var parts = new List<string>();
        if (hospitalId.HasValue)
        {
            parts.Add($"hospitalId={hospitalId.Value}");
        }

        if (frameworkId.HasValue)
        {
            parts.Add($"frameworkId={frameworkId.Value}");
        }

        if (chapterId.HasValue)
        {
            parts.Add($"chapterId={chapterId.Value}");
        }

        if (departmentId.HasValue)
        {
            parts.Add($"departmentId={departmentId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            parts.Add($"status={Uri.EscapeDataString(status)}");
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            parts.Add($"severity={Uri.EscapeDataString(severity)}");
        }

        if (fromDate.HasValue)
        {
            parts.Add($"fromDate={fromDate.Value:O}");
        }

        if (toDate.HasValue)
        {
            parts.Add($"toDate={toDate.Value:O}");
        }

        var query = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
        return $"/api/v{apiVersion}/reports/{path}{query}";
    }
}
