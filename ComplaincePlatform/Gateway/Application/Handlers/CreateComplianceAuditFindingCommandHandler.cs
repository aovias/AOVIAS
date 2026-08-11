using Gateway.Application.Commands;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class CreateComplianceAuditFindingCommandHandler : IRequestHandler<CreateComplianceAuditFindingCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateComplianceAuditFindingCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(CreateComplianceAuditFindingCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        if (request.Body.ObjectiveElementId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/audit-findings";

        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint: endpoint,
            httpMethod: HttpMethod.Post,
            requestBody: request.Body,
            cancellationToken: cancellationToken);
    }
}
