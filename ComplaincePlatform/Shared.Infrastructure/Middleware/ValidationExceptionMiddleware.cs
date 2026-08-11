using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.DTO;
using System.Net;
using System.Text.Json;

namespace Shared.Infrastructure.Middleware;

/// <summary>
/// Middleware to handle FluentValidation validation exceptions.
/// Converts ValidationException to standardized ApiResponse format.
/// </summary>
public class ValidationExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationExceptionMiddleware> _logger;

    public ValidationExceptionMiddleware(
        RequestDelegate next,
        ILogger<ValidationExceptionMiddleware> logger)
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
        catch (ValidationException validationEx)
        {
            await HandleValidationExceptionAsync(context, validationEx);
        }
    }

    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var traceId = context.TraceIdentifier;

        // Convert FluentValidation errors to dictionary format
        var validationErrors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var error = ApiError.ValidationError(
            "Validation failed",
            validationErrors,
            exception.Message);

        var response = ApiResponse<object>.ErrorResponse(error, traceId);

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";
        context.Response.Headers.Append("X-Correlation-ID", traceId);

        _logger.LogWarning(
            "Validation failed: {Errors} (TraceId: {TraceId})",
            string.Join(", ", exception.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")),
            traceId);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}
