using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetJourneyWorkspaceQueryHandler : IRequestHandler<GetJourneyWorkspaceQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneyWorkspaceQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetJourneyWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var qs = string.IsNullOrEmpty(request.QuerySuffix) ? string.Empty : request.QuerySuffix;
        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/workspace{qs}";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Get,
            requestBody: null,
            cancellationToken: cancellationToken);
    }
}
