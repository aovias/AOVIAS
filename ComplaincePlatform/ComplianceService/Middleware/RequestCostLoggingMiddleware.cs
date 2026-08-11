using System.Diagnostics;

namespace ComplianceService.Middleware;

/// <summary>
/// Lightweight request cost telemetry for journey performance rollouts.
/// Controlled by configuration key: PerformanceObservability:Enabled.
/// </summary>
public sealed class RequestCostLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCostLoggingMiddleware> _logger;
    private readonly IConfiguration _configuration;

    public RequestCostLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestCostLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var enabled = _configuration.GetValue<bool>("PerformanceObservability:Enabled");
        if (!enabled)
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        var queryCount = 0;
        if (context.Items.TryGetValue("perf.queryCount", out var value) && value is int boxed)
        {
            queryCount = boxed;
        }

        var payloadSize = context.Response.ContentLength ?? -1;
        _logger.LogInformation(
            "RequestCost traceId={TraceId} path={Path} status={Status} queryCount={QueryCount} elapsedMs={ElapsedMs} payloadBytes={PayloadBytes}",
            context.TraceIdentifier,
            context.Request.Path.Value,
            context.Response.StatusCode,
            queryCount,
            sw.ElapsedMilliseconds,
            payloadSize);
    }
}

