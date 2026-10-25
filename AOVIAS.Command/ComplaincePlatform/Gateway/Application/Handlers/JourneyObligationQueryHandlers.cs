using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetJourneyObligationOverviewQueryHandler
    : IRequestHandler<GetJourneyObligationOverviewQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneyObligationOverviewQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetJourneyObligationOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/journey-obligations/overview?hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetJourneyObligationsQueryHandler
    : IRequestHandler<GetJourneyObligationsQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneyObligationsQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetJourneyObligationsQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/journey-obligations?hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetJourneyObligationDetailQueryHandler
    : IRequestHandler<GetJourneyObligationDetailQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneyObligationDetailQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetJourneyObligationDetailQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.ObligationId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/journey-obligations/{request.ObligationId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetJourneyReadinessQueryHandler
    : IRequestHandler<GetJourneyReadinessQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetJourneyReadinessQueryHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(
        GetJourneyReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.HospitalId == Guid.Empty || request.FrameworkId == Guid.Empty)
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);

        var endpoint = $"/internal/v1/journey-obligations/readiness?hospitalId={request.HospitalId}&frameworkId={request.FrameworkId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}
