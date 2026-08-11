using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetNabhReadinessReportQueryHandler : IRequestHandler<GetNabhReadinessReportQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetNabhReadinessReportQueryHandler> _logger;

    public GetNabhReadinessReportQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetNabhReadinessReportQueryHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetNabhReadinessReportQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.HOSPITAL_ID_REQUIRED, traceId);
        }

        _logger.LogInformation(
            "GET /api/v{Version}/reports/nabh-readiness - Proxying to ComplianceService",
            request.ApiVersion);

        var endpoint = $"/api/v{request.ApiVersion}/reports/nabh-readiness?hospitalId={request.HospitalId}";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
