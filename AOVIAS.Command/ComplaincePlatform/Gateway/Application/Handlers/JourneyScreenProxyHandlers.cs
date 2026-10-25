using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetJourneySessionQueryHandler : IRequestHandler<GetJourneySessionQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneySessionQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetJourneySessionQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var qs = string.IsNullOrEmpty(request.QuerySuffix) ? string.Empty : request.QuerySuffix;
        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/session{qs}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetJourneyDashboardQueryHandler : IRequestHandler<GetJourneyDashboardQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneyDashboardQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetJourneyDashboardQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var qs = string.IsNullOrEmpty(request.QuerySuffix) ? string.Empty : request.QuerySuffix;
        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/dashboard{qs}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetJourneyChaptersSplitQueryHandler : IRequestHandler<GetJourneyChaptersSplitQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneyChaptersSplitQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetJourneyChaptersSplitQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var qs = string.IsNullOrEmpty(request.QuerySuffix) ? string.Empty : request.QuerySuffix;
        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/chapters{qs}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetObjectiveElementDetailQueryHandler : IRequestHandler<GetObjectiveElementDetailQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetObjectiveElementDetailQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetObjectiveElementDetailQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var qs = string.IsNullOrEmpty(request.QuerySuffix) ? string.Empty : request.QuerySuffix;
        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}{qs}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class PatchJourneyOperationalModeCommandHandler : IRequestHandler<PatchJourneyOperationalModeCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatchJourneyOperationalModeCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PatchJourneyOperationalModeCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint = $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/session/operational-mode";
        return await _proxyService.ProxyToComplianceServiceAsync(
            endpoint,
            HttpMethod.Patch,
            request.Body,
            null,
            cancellationToken);
    }
}

public class DeleteObjectiveElementEvidenceCommandHandler : IRequestHandler<DeleteObjectiveElementEvidenceCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteObjectiveElementEvidenceCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(DeleteObjectiveElementEvidenceCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty || request.EvidenceId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence/{request.EvidenceId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Delete, null, null, cancellationToken);
    }
}
