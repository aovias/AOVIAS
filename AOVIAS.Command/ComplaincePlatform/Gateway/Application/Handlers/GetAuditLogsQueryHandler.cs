using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetAuditLogsQueryHandler> _logger;

    public GetAuditLogsQueryHandler(
        IProxyService proxyService,
        ILogger<GetAuditLogsQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GET /api/v{Version}/audit-logs - Proxying to ComplianceService",
            request.ApiVersion);

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query.Add($"entityType={Uri.EscapeDataString(request.EntityType)}");
        }

        if (request.EntityId.HasValue && request.EntityId.Value != Guid.Empty)
        {
            query.Add($"entityId={request.EntityId.Value}");
        }

        if (request.From.HasValue)
        {
            query.Add($"from={request.From.Value:O}");
        }

        if (request.To.HasValue)
        {
            query.Add($"to={request.To.Value:O}");
        }

        var endpoint = $"/api/v{request.ApiVersion}/audit-logs";
        if (query.Count > 0)
        {
            endpoint += "?" + string.Join("&", query);
        }

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
