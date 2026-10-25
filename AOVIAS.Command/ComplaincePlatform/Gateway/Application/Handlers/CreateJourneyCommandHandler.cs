using Gateway.Application.Commands;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for CreateJourneyCommand that proxies journey creation to ComplianceService.
/// </summary>
public class CreateJourneyCommandHandler : IRequestHandler<CreateJourneyCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<CreateJourneyCommandHandler> _logger;

    public CreateJourneyCommandHandler(
        IProxyService proxyService,
        ILogger<CreateJourneyCommandHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(CreateJourneyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Creating compliance journey via ComplianceService for HospitalId={HospitalId}, FrameworkId={FrameworkId}, ApiVersion={ApiVersion}",
            request.HospitalId, request.FrameworkId, request.ApiVersion);

        var endpoint =
            $"/api/v{request.ApiVersion}/hospitals/{request.HospitalId}/frameworks/{request.FrameworkId}/journeys";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Post,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}

