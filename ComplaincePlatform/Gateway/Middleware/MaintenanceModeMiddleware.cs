using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace Gateway.Middleware;

/// <summary>
/// Phase 7.6 — when maintenance is active, short-circuit non-exempt traffic with HTTP 503 + Retry-After.
/// Activated by env <c>MAINTENANCE_MODE=true</c> or file <c>App_Data/maintenance.lock</c>.
/// </summary>
public sealed class MaintenanceModeMiddleware
{
    private const string MaintenanceEnvVar = "MAINTENANCE_MODE";
    private const string LockRelativePath = "App_Data/maintenance.lock";
    private const string RetryAfterSeconds = "60";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MaintenanceModeMiddleware> _logger;

    public MaintenanceModeMiddleware(
        RequestDelegate next,
        IWebHostEnvironment env,
        ILogger<MaintenanceModeMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsMaintenanceActive() || IsExemptPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        _logger.LogWarning(
            "Maintenance mode active — rejecting {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = RetryAfterSeconds;
        context.Response.ContentType = "application/json";

        var body = ApiResponseFactory.Failure<object>(
            ApiResponseCode.SERVICE_UNAVAILABLE,
            context.TraceIdentifier);

        // Override message for operator clarity (registry text targets downstream outages).
        body.Message = "Platform is under maintenance. Please retry after the Retry-After interval.";

        await context.Response.WriteAsJsonAsync(body);
    }

    private bool IsMaintenanceActive()
    {
        var envFlag = Environment.GetEnvironmentVariable(MaintenanceEnvVar);
        if (string.Equals(envFlag, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(envFlag, "1", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var lockPath = Path.Combine(_env.ContentRootPath, LockRelativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(lockPath);
    }

    private static bool IsExemptPath(PathString path)
    {
        var value = path.Value ?? string.Empty;

        // Health / readiness probes
        if (value.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/liveness", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/readiness", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Platform registration + AOVIAS catalog
        if (value.StartsWith("/platform", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // First-run / setup surfaces
        if (value.StartsWith("/api/v1/setup", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/setup", StringComparison.OrdinalIgnoreCase)
               && value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Ops observability / backup probes used during upgrades
        if (value.StartsWith("/api/v1/ops/", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/api/v1.0/ops/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
