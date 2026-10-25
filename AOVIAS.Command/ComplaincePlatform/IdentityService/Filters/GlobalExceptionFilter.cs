using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using IdentityService.Api;
using Shared.Infrastructure.Api;

namespace IdentityService.Filters;

/// <summary>
/// Global exception filter to return standardized error responses
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;

        _logger.LogError(exception, "Unhandled exception in {Path}", context.HttpContext.Request.Path);

        context.Result = AuthApiResponses.Failure<object>(
            ApiResponseCode.INTERNAL_ERROR,
            "An unexpected error occurred. Please try again later.",
            context.HttpContext.TraceIdentifier);
        context.ExceptionHandled = true;
    }
}
