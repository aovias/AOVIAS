using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for GetValidTransitionsQuery that proxies to ComplianceService.
/// </summary>
public class GetValidTransitionsQueryHandler : IRequestHandler<GetValidTransitionsQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetValidTransitionsQueryHandler> _logger;

    public GetValidTransitionsQueryHandler(
        IProxyService proxyService,
        ILogger<GetValidTransitionsQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetValidTransitionsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving valid transitions for JourneyId={JourneyId} via ComplianceService (v{Version})",
            request.JourneyId, request.ApiVersion);

        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/valid-transitions";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}

