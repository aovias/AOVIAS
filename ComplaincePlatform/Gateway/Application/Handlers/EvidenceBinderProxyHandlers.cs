using Gateway.Application.Commands;
using Gateway.Application.Http;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public sealed class GetEvidenceBinderSummaryQueryHandler : IRequestHandler<GetEvidenceBinderSummaryQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderSummaryQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderSummaryQuery request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/evidence-binder/summary?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceBinderChaptersQueryHandler : IRequestHandler<GetEvidenceBinderChaptersQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderChaptersQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderChaptersQuery request, CancellationToken cancellationToken)
    {
        var endpoint = $"/api/v{request.ApiVersion}/evidence-binder/chapters?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceBinderObjectivesQueryHandler : IRequestHandler<GetEvidenceBinderObjectivesQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderObjectivesQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderObjectivesQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence-binder/chapters/{request.ChapterId}/objectives?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceBinderOverviewQueryHandler : IRequestHandler<GetEvidenceBinderOverviewQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderOverviewQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderOverviewQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence-binder/objective-elements/{request.ObjectiveElementId}/overview?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceBinderEvidencesQueryHandler : IRequestHandler<GetEvidenceBinderEvidencesQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderEvidencesQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderEvidencesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence-binder/objective-elements/{request.ObjectiveElementId}/evidences?frameworkId={request.FrameworkId}&page={page}&pageSize={pageSize}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceBinderChecklistQueryHandler : IRequestHandler<GetEvidenceBinderChecklistQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderChecklistQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderChecklistQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence-binder/objective-elements/{request.ObjectiveElementId}/checklist?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceBinderAiMappingQueryHandler : IRequestHandler<GetEvidenceBinderAiMappingQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderAiMappingQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderAiMappingQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence-binder/objective-elements/{request.ObjectiveElementId}/ai-mapping?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class GetEvidenceBinderHistoryQueryHandler : IRequestHandler<GetEvidenceBinderHistoryQuery, IActionResult>
{
    private readonly IProxyService _proxyService;

    public GetEvidenceBinderHistoryQueryHandler(IProxyService proxyService) => _proxyService = proxyService;

    public Task<IActionResult> Handle(GetEvidenceBinderHistoryQuery request, CancellationToken cancellationToken)
    {
        var endpoint =
            $"/api/v{request.ApiVersion}/evidence-binder/objective-elements/{request.ObjectiveElementId}/history?frameworkId={request.FrameworkId}";
        return _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public sealed class PostEvidenceBinderOeEvidenceMultipartCommandHandler
    : IRequestHandler<PostEvidenceBinderOeEvidenceMultipartCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PostEvidenceBinderOeEvidenceMultipartCommandHandler> _logger;

    public PostEvidenceBinderOeEvidenceMultipartCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostEvidenceBinderOeEvidenceMultipartCommandHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(PostEvidenceBinderOeEvidenceMultipartCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.FrameworkId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var httpContext = _httpContextAccessor.HttpContext!;
        var correlationId = EvidenceMultipartLogging.GetCorrelationId(httpContext);
        var (content, bodyByteLength) = await MultipartProxyRequestBody.CreateStreamContentAsync(
            httpContext.Request,
            cancellationToken,
            _logger);

        _logger.LogInformation(
            "Evidence binder multipart proxy. CorrelationId={CorrelationId} TraceId={TraceId} FrameworkId={FrameworkId} ObjectiveElementId={ObjectiveElementId} " +
            "IncomingContentLength={IncomingContentLength} BufferedBodyBytes={BufferedBodyBytes} ContentType={ContentType}",
            correlationId,
            traceId,
            request.FrameworkId,
            request.ObjectiveElementId,
            httpContext.Request.ContentLength,
            bodyByteLength,
            httpContext.Request.ContentType);

        var endpoint =
            $"/api/v{request.ApiVersion}/evidence-binder/objective-elements/{request.ObjectiveElementId}/evidences?frameworkId={request.FrameworkId}";

        return await _proxyService.ProxyToComplianceServiceRawAsync(
            endpoint,
            HttpMethod.Post,
            content,
            cancellationToken: cancellationToken);
    }
}
