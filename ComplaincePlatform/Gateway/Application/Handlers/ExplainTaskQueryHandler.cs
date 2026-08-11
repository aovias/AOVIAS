using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using System.Net;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for ExplainTaskQuery that proxies to AIService.
/// </summary>
    public class ExplainTaskQueryHandler : IRequestHandler<ExplainTaskQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly ILogger<ExplainTaskQueryHandler> _logger;

    public ExplainTaskQueryHandler(
        IProxyService proxyService,
        ILogger<ExplainTaskQueryHandler> logger)
    {
        _proxyService = proxyService;
        _logger = logger;
    }

        public async Task<IActionResult> Handle(ExplainTaskQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing explain task request for hospital {HospitalId}", request.HospitalId);

        var additionalHeaders = new Dictionary<string, string>
        {
            { "X-Hospital-Id", request.HospitalId.ToString() }
        };

        var result = await _proxyService.ProxyToAIServiceAsync(
            endpoint: "/internal/explain-task",
            httpMethod: HttpMethod.Post,
            requestBody: request.RequestBody,
            additionalHeaders: additionalHeaders,
            cancellationToken: cancellationToken);

        // Handle rate limiting
        if (result is StatusCodeResult statusResult && statusResult.StatusCode == 429)
        {
            _logger.LogWarning("Rate limit exceeded for hospital {HospitalId}", request.HospitalId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.TOO_MANY_REQUESTS];
            var envelope = ApiResponseFactory.Failure<object>(ApiResponseCode.TOO_MANY_REQUESTS);

            return new ObjectResult(envelope)
            {
                StatusCode = meta.HttpStatus
            };
        }

        return result;
    }
}
