using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Shared.Logging.Audit;
using Shared.Logging.Correlation;

namespace Shared.Logging;

/// <summary>
/// One-call registration and pipeline wiring for PRAMAN structured logging.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Registers logging support services: HTTP context accessor (needed for correlation
    /// forwarding), the correlation-forwarding HttpClient handler, and the audit logger.
    /// </summary>
    public static IServiceCollection AddPramanLogging(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<CorrelationForwardingHandler>();
        services.AddSingleton<IAuditLogger, AuditLogger>();
        return services;
    }

    /// <summary>
    /// Wires the request-scoped logging pipeline: correlation id first (so every log line
    /// for the request is enriched), then Serilog request logging. Call this early, before
    /// routing/authentication, in every web service.
    /// </summary>
    public static IApplicationBuilder UsePramanLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex is not null || httpContext.Response.StatusCode >= 500)
                {
                    return Serilog.Events.LogEventLevel.Error;
                }

                // Keep health probe noise out of the Information stream.
                var path = httpContext.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                {
                    return Serilog.Events.LogEventLevel.Debug;
                }

                return httpContext.Response.StatusCode >= 400
                    ? Serilog.Events.LogEventLevel.Warning
                    : Serilog.Events.LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set(LoggingConstants.Properties.EventCode, EventCode.ApiResponse);
                diagnosticContext.Set(LoggingConstants.Properties.Category, LoggingConstants.Categories.Request);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            };
        });

        return app;
    }

    /// <summary>
    /// Logs application started / stopping lifecycle events with stable event codes.
    /// </summary>
    public static IApplicationBuilder UsePramanLifecycleLogging(this IApplicationBuilder app, string serviceName)
    {
        var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger("Lifecycle");

        lifetime?.ApplicationStarted.Register(() =>
            logger.LogInformation("{Service} started. {EventCode}", serviceName, EventCode.ApplicationStarted));

        lifetime?.ApplicationStopping.Register(() =>
            logger.LogInformation("{Service} stopping. {EventCode}", serviceName, EventCode.ApplicationStopped));

        return app;
    }

    /// <summary>
    /// Validates that required configuration keys are present at startup. Logs and
    /// throws (fatal) if any are missing so the failure is captured by the bootstrap logger.
    /// </summary>
    public static void ValidateRequiredConfiguration(IConfiguration configuration, string serviceName, params string[] requiredKeys)
    {
        var missing = requiredKeys
            .Where(key => string.IsNullOrWhiteSpace(configuration[key]))
            .ToArray();

        if (missing.Length == 0)
        {
            return;
        }

        var joined = string.Join(", ", missing);
        Log.Fatal("{Service} is missing required configuration: {MissingKeys}. {EventCode}",
            serviceName, joined, EventCode.ConfigMissing);
        throw new InvalidOperationException($"{serviceName} is missing required configuration: {joined}");
    }
}
