using System.Diagnostics;
using FluentValidation;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Application.Handlers;

/// <summary>
/// Handler for RefreshTokenQuery that proxies to IdentityService.
/// </summary>
public class RefreshTokenQueryHandler : IRequestHandler<RefreshTokenQuery, IActionResult>
{
    private readonly IProxyService _proxyService;
    private readonly IValidator<RefreshTokenQuery> _validator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RefreshTokenQueryHandler> _logger;

    public RefreshTokenQueryHandler(
        IProxyService proxyService,
        IValidator<RefreshTokenQuery> validator,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RefreshTokenQueryHandler> logger)
    {
        _proxyService = proxyService;
        _validator = validator;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<IActionResult> Handle(RefreshTokenQuery request, CancellationToken cancellationToken)
    {
        var correlationId = GetCorrelationId();
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Endpoint"] = "identity/refresh"
        });

        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ApiError { Field = e.PropertyName, Message = e.ErrorMessage })
                .ToList();
            _logger.LogWarning(
                "identity/refresh validation failed: {ErrorSummary}",
                string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var envelope = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, errors, correlationId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return new ObjectResult(envelope) { StatusCode = meta.HttpStatus };
        }

        _logger.LogInformation("identity/refresh: forwarding to IdentityService");

        var sw = Stopwatch.StartNew();
        var result = await _proxyService.ProxyToIdentityServiceAsync(
            endpoint: "/api/v1/auth/refresh",
            httpMethod: HttpMethod.Post,
            requestBody: request.RequestBody,
            cancellationToken: cancellationToken);
        sw.Stop();

        _logger.LogInformation(
            "identity/refresh: proxy completed in {ElapsedMs}ms",
            sw.ElapsedMilliseconds);

        return result;
    }

    private string GetCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request?.Headers != null)
        {
            var id = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                     ?? httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(id))
                return id;
        }

        return httpContext?.TraceIdentifier ?? Guid.NewGuid().ToString();
    }
}
