using System.Net.Http;
using System.Text.Json;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Middleware;

/// <summary>
/// Global exception handling middleware for Gateway.
/// Converts unhandled exceptions in BFF endpoints into standardized ApiResponse envelopes.
/// Does not interfere with proxied downstream responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            // Request aborted while processing — not a server fault.
            return;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var requestId = context.TraceIdentifier;
        var root = Unwrap(exception);

        _logger.LogError(exception, "Unhandled exception in Gateway while processing request {Path}", context.Request.Path);

        ApiResponseCode code = root switch
        {
            UnauthorizedAccessException => ApiResponseCode.UNAUTHORIZED,
            HttpRequestException => ApiResponseCode.SERVICE_UNAVAILABLE,
            _ => ApiResponseCode.INTERNAL_ERROR
        };

        var meta = ApiResponseCodes.Registry[code];

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = meta.HttpStatus;

        var errors = new List<ApiError>
        {
            new ApiError
            {
                Message = root.Message
            }
        };

        var response = ApiResponseFactory.Failure<object>(code, errors, requestId);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static Exception Unwrap(Exception exception)
    {
        while (true)
        {
            if (exception is AggregateException { InnerExceptions.Count: 1 } aggregate)
            {
                exception = aggregate.InnerExceptions[0];
                continue;
            }

            if (exception.InnerException != null
                && exception is not UnauthorizedAccessException
                && exception is not HttpRequestException
                && exception.GetType().Name is "TargetInvocationException" or "HttpResponseException")
            {
                exception = exception.InnerException;
                continue;
            }

            return exception;
        }
    }
}

