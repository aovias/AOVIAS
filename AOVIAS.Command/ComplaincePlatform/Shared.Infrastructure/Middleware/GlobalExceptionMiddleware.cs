using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.DTO;
using System.Net;
using System.Text.Json;

namespace Shared.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and converts them to standardized ApiResponse format.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
            // Request aborted — not a server fault.
            return;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        ApiError error;
        int statusCode;

        // Map exceptions to appropriate HTTP status codes and error types
        switch (exception)
        {
            case ArgumentException argEx:
                error = ApiError.ValidationError(
                    "Invalid argument provided",
                    new Dictionary<string, string[]> { { "argument", new[] { argEx.Message } } },
                    argEx.ParamName);
                statusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning(exception, "Argument exception: {Message} (TraceId: {TraceId})", argEx.Message, traceId);
                break;

            case KeyNotFoundException keyEx:
                error = ApiError.NotFound(
                    "RESOURCE_NOT_FOUND",
                    keyEx.Message ?? "The requested resource was not found",
                    null);
                statusCode = (int)HttpStatusCode.NotFound;
                _logger.LogWarning(exception, "Resource not found: {Message} (TraceId: {TraceId})", keyEx.Message, traceId);
                break;

            case UnauthorizedAccessException authEx:
                error = ApiError.Forbidden(
                    authEx.Message ?? "You do not have permission to access this resource",
                    null);
                statusCode = (int)HttpStatusCode.Forbidden;
                _logger.LogWarning(exception, "Unauthorized access: {Message} (TraceId: {TraceId})", authEx.Message, traceId);
                break;

            case InvalidOperationException invalidOpEx:
                // Check if it's a business rule violation (409) or general error (400)
                if (invalidOpEx.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                    invalidOpEx.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
                {
                    error = ApiError.Conflict(
                        "BUSINESS_RULE_VIOLATION",
                        invalidOpEx.Message,
                        null);
                    statusCode = (int)HttpStatusCode.Conflict;
                }
                else
                {
                    error = ApiError.ValidationError(
                        "Invalid operation",
                        new Dictionary<string, string[]> { { "operation", new[] { invalidOpEx.Message } } },
                        null);
                    statusCode = (int)HttpStatusCode.BadRequest;
                }
                _logger.LogWarning(exception, "Invalid operation: {Message} (TraceId: {TraceId})", invalidOpEx.Message, traceId);
                break;

            case NotImplementedException notImplEx:
                error = ApiError.InternalServerError("Feature not implemented");
                statusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(exception, "Not implemented: {Message} (TraceId: {TraceId})", notImplEx.Message, traceId);
                break;

            default:
                // All other exceptions are treated as internal server errors
                var env = context.RequestServices.GetService(typeof(IHostEnvironment)) as IHostEnvironment;
                var isDevelopment = env?.IsDevelopment() == true;
                error = ApiError.InternalServerError(
                    isDevelopment
                        ? exception.ToString()
                        : null);
                statusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(exception, "Unhandled exception: {Message} (TraceId: {TraceId})", exception.Message, traceId);
                break;
        }

        // Create standardized error response
        var response = ApiResponse<object>.ErrorResponse(error, traceId);

        // Set response properties
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Add correlation ID header
        context.Response.Headers.Append("X-Correlation-ID", traceId);

        // Serialize and write response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
