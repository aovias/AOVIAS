using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Middleware;

namespace Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring standardized API responses and middleware.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds global exception handling middleware to the pipeline.
    /// Should be called early in the pipeline configuration.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }

    /// <summary>
    /// Adds validation exception handling middleware to the pipeline.
    /// Should be called after UseGlobalExceptionHandling.
    /// </summary>
    public static IApplicationBuilder UseValidationExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ValidationExceptionMiddleware>();
    }

    /// <summary>
    /// Adds correlation ID middleware to ensure X-Correlation-ID header is set.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.TraceIdentifier;
            
            // Ensure X-Correlation-ID header is set
            if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
            }

            // Forward correlation ID to downstream services if present in request
            if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var incomingCorrelationId))
            {
                context.TraceIdentifier = incomingCorrelationId.ToString();
            }

            await next();
        });
    }
}
