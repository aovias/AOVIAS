using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetAuditFindingsReportQueryHandler : IRequestHandler<GetAuditFindingsReportQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetAuditFindingsReportQueryHandler> _logger;

    public GetAuditFindingsReportQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetAuditFindingsReportQueryHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetAuditFindingsReportQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_ID_REQUIRED, traceId);
        }

        _logger.LogInformation(
            "GET /api/v{Version}/reports/audit-findings - Proxying to ComplianceService",
            request.ApiVersion);

        var query = new List<string> { $"hospitalId={request.HospitalId}" };
        if (request.FrameworkId.HasValue)
        {
            query.Add($"frameworkId={request.FrameworkId.Value}");
        }

        if (request.From.HasValue)
        {
            query.Add($"from={request.From.Value:O}");
        }

        if (request.To.HasValue)
        {
            query.Add($"to={request.To.Value:O}");
        }

        var endpoint = $"/api/v{request.ApiVersion}/reports/audit-findings?" + string.Join("&", query);

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
