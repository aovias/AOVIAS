using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Logging.Diagnostics;

/// <summary>
/// Emits structured timing logs for dashboard and command-center HTTP endpoints.
/// Always active in Development; in other environments enable via
/// <c>PerformanceObservability:DashboardTimingEnabled</c>.
/// </summary>
public sealed class DashboardRequestTimingMiddleware
{
    private static readonly string[] DashboardPathSegments =
    [
        "/dashboard",
        "/compliance/snapshots",
        "/control-center/compliance",
        "/compliance/maturity-dashboard",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<DashboardRequestTimingMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public DashboardRequestTimingMiddleware(
        RequestDelegate next,
        ILogger<DashboardRequestTimingMiddleware> logger,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!IsDashboardPath(path))
        {
            await _next(context);
            return;
        }

        if (!IsEnabled())
        {
            await _next(context);
            return;
        }

        var started = Stopwatch.GetTimestamp();
        await _next(context);
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        var queryCount = context.Items.TryGetValue("perf.queryCount", out var qc) && qc is int count
            ? count
            : 0;

        var payload = new
        {
            category = "DashboardPerformance",
            traceId = context.TraceIdentifier,
            method = context.Request.Method,
            path,
            statusCode = context.Response.StatusCode,
            durationMs = Math.Round(elapsedMs, 2),
            queryCount,
            payloadBytes = context.Response.ContentLength,
        };

        _logger.LogInformation(
            "DashboardRequestTiming {Category} traceId={TraceId} path={Path} status={Status} durationMs={DurationMs} queryCount={QueryCount}",
            payload.category,
            payload.traceId,
            payload.path,
            payload.statusCode,
            payload.durationMs,
            payload.queryCount);

        if (_environment.IsDevelopment())
        {
            Console.WriteLine($"AOVIAS_DASHBOARD_TIMING {JsonSerializer.Serialize(payload, JsonOptions)}");
        }
    }

    private bool IsEnabled()
        => _environment.IsDevelopment()
           || _configuration.GetValue<bool>("PerformanceObservability:DashboardTimingEnabled");

    private static bool IsDashboardPath(string path)
        => DashboardPathSegments.Any(segment =>
            path.Contains(segment, StringComparison.OrdinalIgnoreCase));
}

public static class DashboardRequestTimingExtensions
{
    public static IApplicationBuilder UseDashboardRequestTiming(this IApplicationBuilder app)
        => app.UseMiddleware<DashboardRequestTimingMiddleware>();
}
