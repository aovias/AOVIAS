using Gateway.Application.Commands;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class SyncComplianceTasksCommandHandler : IRequestHandler<SyncComplianceTasksCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SyncComplianceTasksCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IActionResult> Handle(SyncComplianceTasksCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/compliance-tasks/sync";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, requestBody: null, cancellationToken: cancellationToken);
    }
}
