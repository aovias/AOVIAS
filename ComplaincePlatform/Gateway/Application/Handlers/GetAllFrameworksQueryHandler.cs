using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class GetAllFrameworksQueryHandler : IRequestHandler<GetAllFrameworksQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetAllFrameworksQueryHandler> _logger;

    public GetAllFrameworksQueryHandler(
        IProxyService proxyService,
        ILogger<GetAllFrameworksQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetAllFrameworksQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v{Version}/frameworks/all-frameworks — proxying", request.ApiVersion);

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: $"/api/v{request.ApiVersion}/frameworks",
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
