using IdentityService.Api;
using Shared.Infrastructure.Api;
using System.Net;

namespace IdentityService.Middleware;

/// <summary>
/// Middleware to handle rate limiting responses and return standardized error format
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == (int)HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Path} from {RemoteIp}",
                context.Request.Path,
                context.Connection.RemoteIpAddress);

            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";

                var errorResponse = AuthApiResponses.Failure<object>(
                    ApiResponseCode.TOO_MANY_REQUESTS,
                    "Too many requests. Please wait a moment before trying again.",
                    context.TraceIdentifier);

                context.Response.StatusCode = errorResponse.StatusCode ?? (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsJsonAsync(errorResponse.Value);
            }
        }
    }
}
