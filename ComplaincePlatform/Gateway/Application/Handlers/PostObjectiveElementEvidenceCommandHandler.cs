using Gateway.Application.Commands;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class PostObjectiveElementEvidenceCommandHandler : IRequestHandler<PostObjectiveElementEvidenceCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostObjectiveElementEvidenceCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PostObjectiveElementEvidenceCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        if (request.Body is null)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Post,
            requestBody: request.Body,
            cancellationToken: cancellationToken);
    }
}
