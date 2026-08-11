using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetAuditLogsForEntityQueryHandler : IRequestHandler<GetAuditLogsForEntityQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<GetAuditLogsForEntityQueryHandler> _logger;

    public GetAuditLogsForEntityQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<GetAuditLogsForEntityQueryHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetAuditLogsForEntityQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (string.IsNullOrWhiteSpace(request.EntityType) || request.EntityId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        _logger.LogInformation(
            "GET /api/v{Version}/audit-logs/{EntityType}/{EntityId} - Proxying to ComplianceService",
            request.ApiVersion, request.EntityType, request.EntityId);

        var endpoint = $"/api/v{request.ApiVersion}/audit-logs/{Uri.EscapeDataString(request.EntityType)}/{request.EntityId}";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
