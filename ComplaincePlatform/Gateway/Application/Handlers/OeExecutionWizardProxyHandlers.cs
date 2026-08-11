using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public sealed class GetOeExecutionWizardQueryHandler : IRequestHandler<GetOeExecutionWizardQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetOeExecutionWizardQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IActionResult> Handle(GetOeExecutionWizardQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/oe-execution-wizard";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class SaveOeExecutionWizardDraftCommandHandler : IRequestHandler<SaveOeExecutionWizardDraftCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SaveOeExecutionWizardDraftCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IActionResult> Handle(SaveOeExecutionWizardDraftCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/oe-execution-wizard";
        return _proxyService.ProxyToComplianceServiceAsync(
            endpoint,
            HttpMethod.Put,
            request.Body,
            null,
            cancellationToken);
    }
}

public sealed class CompleteOeExecutionWizardCommandHandler : IRequestHandler<CompleteOeExecutionWizardCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompleteOeExecutionWizardCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<IActionResult> Handle(CompleteOeExecutionWizardCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            return Task.FromResult<IActionResult>(
                ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }

        var qs = string.IsNullOrEmpty(request.QuerySuffix) ? string.Empty : request.QuerySuffix;
        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/oe-execution-wizard/complete{qs}";
        return _proxyService.ProxyToComplianceServiceAsync(
            endpoint,
            HttpMethod.Post,
            request.Body,
            null,
            cancellationToken);
    }
}
