using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetScoringAggregateV11QueryHandler : IRequestHandler<GetScoringAggregateV11Query, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetScoringAggregateV11QueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetScoringAggregateV11Query request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty || request.JourneyId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/scoring/hospitals/{request.HospitalId}/frameworks/{request.FrameworkId}/journeys/{request.JourneyId}/aggregate?operationalMode={Uri.EscapeDataString(request.OperationalMode.ToString())}";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
