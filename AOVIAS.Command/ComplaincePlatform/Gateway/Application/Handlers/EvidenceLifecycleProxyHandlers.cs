using Gateway.Application.Commands;
using Gateway.Application.Http;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class GetObjectiveElementEvidenceDetailQueryHandler : IRequestHandler<GetObjectiveElementEvidenceDetailQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetObjectiveElementEvidenceDetailQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetObjectiveElementEvidenceDetailQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty || request.EvidenceId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence/{request.EvidenceId}";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Get, null, null, cancellationToken);
    }
}

public class GetObjectiveElementEvidenceDownloadQueryHandler : IRequestHandler<GetObjectiveElementEvidenceDownloadQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetObjectiveElementEvidenceDownloadQueryHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(GetObjectiveElementEvidenceDownloadQuery request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty || request.EvidenceId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence/{request.EvidenceId}/download";
        return await _proxyService.ProxyComplianceServiceStreamGetAsync(endpoint, cancellationToken);
    }
}

public class PostObjectiveElementEvidenceVerifyCommandHandler : IRequestHandler<PostObjectiveElementEvidenceVerifyCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostObjectiveElementEvidenceVerifyCommandHandler(IProxyService proxyService, IHttpContextAccessor httpContextAccessor)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Handle(PostObjectiveElementEvidenceVerifyCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty || request.EvidenceId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        if (request.Body is null)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence/{request.EvidenceId}/verify";
        return await _proxyService.ProxyToComplianceServiceAsync(endpoint, HttpMethod.Post, request.Body, null, cancellationToken);
    }
}

public class PostObjectiveElementEvidenceVersionMultipartCommandHandler : IRequestHandler<PostObjectiveElementEvidenceVersionMultipartCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PostObjectiveElementEvidenceVersionMultipartCommandHandler> _logger;

    public PostObjectiveElementEvidenceVersionMultipartCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostObjectiveElementEvidenceVersionMultipartCommandHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(PostObjectiveElementEvidenceVersionMultipartCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty || request.EvidenceId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var httpContext = _httpContextAccessor.HttpContext!;
        var (content, bodyByteLength) = await MultipartProxyRequestBody.CreateStreamContentAsync(
            httpContext.Request,
            cancellationToken,
            _logger);
        _logger.LogInformation(
            "Multipart evidence-version proxy. CorrelationId={CorrelationId} TraceId={TraceId} JourneyId={JourneyId} ObjectiveElementId={ObjectiveElementId} EvidenceId={EvidenceId} " +
            "IncomingContentLength={IncomingContentLength} BufferedBodyBytes={BufferedBodyBytes} ContentType={ContentType}",
            EvidenceMultipartLogging.GetCorrelationId(httpContext),
            traceId,
            request.JourneyId,
            request.ObjectiveElementId,
            request.EvidenceId,
            httpContext.Request.ContentLength,
            bodyByteLength,
            httpContext.Request.ContentType);

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence/{request.EvidenceId}/versions";
        return await _proxyService.ProxyToComplianceServiceRawAsync(endpoint, HttpMethod.Post, content, null, cancellationToken);
    }
}

public class PostObjectiveElementEvidenceBulkMultipartCommandHandler : IRequestHandler<PostObjectiveElementEvidenceBulkMultipartCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PostObjectiveElementEvidenceBulkMultipartCommandHandler> _logger;

    public PostObjectiveElementEvidenceBulkMultipartCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostObjectiveElementEvidenceBulkMultipartCommandHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(PostObjectiveElementEvidenceBulkMultipartCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
        {
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
        }

        var httpContext = _httpContextAccessor.HttpContext!;
        var (content, bodyByteLength) = await MultipartProxyRequestBody.CreateStreamContentAsync(
            httpContext.Request,
            cancellationToken,
            _logger);
        _logger.LogInformation(
            "Multipart evidence-bulk proxy. CorrelationId={CorrelationId} TraceId={TraceId} JourneyId={JourneyId} ObjectiveElementId={ObjectiveElementId} " +
            "IncomingContentLength={IncomingContentLength} BufferedBodyBytes={BufferedBodyBytes} ContentType={ContentType}",
            EvidenceMultipartLogging.GetCorrelationId(httpContext),
            traceId,
            request.JourneyId,
            request.ObjectiveElementId,
            httpContext.Request.ContentLength,
            bodyByteLength,
            httpContext.Request.ContentType);

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence/bulk";
        return await _proxyService.ProxyToComplianceServiceRawAsync(endpoint, HttpMethod.Post, content, null, cancellationToken);
    }
}
