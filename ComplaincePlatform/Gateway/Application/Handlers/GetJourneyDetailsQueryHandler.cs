using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for GetJourneyDetailsQuery that proxies to ComplianceService.
/// </summary>
public class GetJourneyDetailsQueryHandler : IRequestHandler<GetJourneyDetailsQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<GetJourneyDetailsQueryHandler> _logger;

    public GetJourneyDetailsQueryHandler(
        IProxyService proxyService,
        ILogger<GetJourneyDetailsQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(GetJourneyDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Retrieving journey details for JourneyId={JourneyId} via ComplianceService (v{Version})",
            request.JourneyId, request.ApiVersion);

        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}

