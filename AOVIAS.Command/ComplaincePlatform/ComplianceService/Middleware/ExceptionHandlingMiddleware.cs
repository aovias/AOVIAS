using System.Net;
using System.Text.Json;
using ComplianceService.Application.Exceptions;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Middleware;

/// <summary>
/// Global exception handling middleware for ComplianceService.
/// Ensures all unhandled exceptions are converted into standardized ApiResponse envelopes.
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
            // HttpContext.RequestAborted (client disconnect, proxy timeout, duplicate fetch) — not a server fault.
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

        _logger.LogError(exception, "Unhandled exception occurred while processing request {Path}", context.Request.Path);

        ApiResponseCode code;

        if (exception is StageLockedException stageLocked)
        {
            code = ApiResponseCode.STAGE_LOCKED;
            // Log at Info level — this is expected domain behaviour, not an error.
            _logger.LogInformation(
                "Stage gate enforced: Stage={Stage}, Score={Score:F1}%, Required={Required:F1}%",
                stageLocked.Stage, stageLocked.CurrentScore, stageLocked.RequiredScore);
        }
        else if (exception is UnauthorizedAccessException)
        {
            code = ApiResponseCode.UNAUTHORIZED;
        }
        else if (exception is InvalidOperationException)
        {
            // Treat InvalidOperationException as validation/semantic failure for now
            code = ApiResponseCode.VALIDATION_FAILED;
        }
        else
        {
            code = ApiResponseCode.INTERNAL_ERROR;
        }

        var meta = ApiResponseCodes.Registry[code];

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = meta.HttpStatus;

        var errors = new List<ApiError>
        {
            new ApiError
            {
                Message = exception.Message
            }
        };

        var response = ApiResponseFactory.Failure<object>(code, errors, requestId);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

