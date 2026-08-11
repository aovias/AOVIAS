using Gateway.Application.Commands;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for TransitionJourneyCommand that proxies to ComplianceService.
/// CorrelationId is handled centrally in ProxyService; this handler just logs intent.
/// </summary>
public class TransitionJourneyCommandHandler : IRequestHandler<TransitionJourneyCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<TransitionJourneyCommandHandler> _logger;

    public TransitionJourneyCommandHandler(
        IProxyService proxyService,
        ILogger<TransitionJourneyCommandHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(TransitionJourneyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing journey transition for JourneyId {JourneyId}", 
            request.JourneyId);

        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/transition";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Post,
            requestBody: request.RequestBody,
            cancellationToken: cancellationToken);
    }
}

