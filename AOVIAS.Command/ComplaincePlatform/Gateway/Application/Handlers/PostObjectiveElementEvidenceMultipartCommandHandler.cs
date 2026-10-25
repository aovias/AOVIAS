using Gateway.Application.Commands;
using Gateway.Application.Http;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;

namespace Gateway.Application.Handlers;

public class PostObjectiveElementEvidenceMultipartCommandHandler : IRequestHandler<PostObjectiveElementEvidenceMultipartCommand, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PostObjectiveElementEvidenceMultipartCommandHandler> _logger;

    public PostObjectiveElementEvidenceMultipartCommandHandler(
        IProxyService proxyService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostObjectiveElementEvidenceMultipartCommandHandler> logger)
    {
        _proxyService = proxyService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(PostObjectiveElementEvidenceMultipartCommand request, CancellationToken cancellationToken)
    {
        var traceId = _httpContextAccessor.HttpContext?.TraceIdentifier;
        if (request.JourneyId == Guid.Empty || request.ObjectiveElementId == Guid.Empty)
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
            "Multipart evidence proxy (outbound to Compliance). CorrelationId={CorrelationId} TraceId={TraceId} JourneyId={JourneyId} ObjectiveElementId={ObjectiveElementId} " +
            "IncomingContentLength={IncomingContentLength} BufferedBodyBytes={BufferedBodyBytes} ContentType={ContentType}",
            correlationId,
            traceId,
            request.JourneyId,
            request.ObjectiveElementId,
            httpContext.Request.ContentLength,
            bodyByteLength,
            httpContext.Request.ContentType);

        var endpoint =
            $"/api/v{request.ApiVersion}/journeys/{request.JourneyId}/objective-elements/{request.ObjectiveElementId}/evidence";

        return await _proxyService.ProxyToComplianceServiceRawAsync(
            endpoint,
            HttpMethod.Post,
            content,
            cancellationToken: cancellationToken);
    }
}
