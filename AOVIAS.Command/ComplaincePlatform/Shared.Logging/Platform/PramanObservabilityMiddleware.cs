using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shared.Logging.Platform;

/// <summary>
/// Emits structured observability events consumable by the AOVIAS Observability Platform.
/// </summary>
public sealed class PramanObservabilityMiddleware
{
    private readonly RequestDelegate next;
    private readonly PramanServiceRegistration registration;
    private readonly ILogger<PramanObservabilityMiddleware> logger;

    public PramanObservabilityMiddleware(
        RequestDelegate next,
        PramanServiceRegistration registration,
        ILogger<PramanObservabilityMiddleware> logger)
    {
        this.next = next;
        this.registration = registration;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Aovias-Application"] = registration.ApplicationId;
        context.Response.Headers["X-Aovias-Service-Id"] = registration.ServiceId;
        context.Response.Headers["X-Aovias-Service-Version"] = registration.Version;

        var started = Stopwatch.GetTimestamp();
        await next(context);
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (context.Request.Path.StartsWithSegments("/health")
            || context.Request.Path.StartsWithSegments("/liveness")
            || context.Request.Path.StartsWithSegments("/readiness")
            || context.Request.Path.StartsWithSegments("/platform"))
        {
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var statusCode = context.Response.StatusCode;
        var method = context.Request.Method;

        Activity.Current?.SetTag("aovias.service_id", registration.ServiceId);
        Activity.Current?.SetTag("http.response.status_code", statusCode);
        Activity.Current?.SetTag("aovias.duration_ms", elapsedMs);

        logger.LogInformation(
            "AOVIAS_OBSERVABILITY HttpRequestCompleted {ServiceId} {Method} {Path} {StatusCode} {DurationMs}",
            registration.ServiceId,
            method,
            path,
            statusCode,
            elapsedMs);
    }
}

/// <summary>
/// Middleware registration helpers for PRAMAN observability.
/// </summary>
public static class PramanObservabilityExtensions
{
    /// <summary>
    /// Adds structured request observability for AOVIAS log and event ingestion.
    /// </summary>
    public static IApplicationBuilder UsePramanObservability(this IApplicationBuilder app)
        => app.UseMiddleware<PramanObservabilityMiddleware>();
}
