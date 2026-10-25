using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shared.Logging.OpenTelemetry;

/// <summary>
/// AOVIAS OpenTelemetry registration — exporters are skipped when endpoints/connection strings are unset.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds ASP.NET Core + HttpClient instrumentation. Configures OTLP when
    /// <c>OpenTelemetry:OtlpEndpoint</c> is set, and Azure Monitor when
    /// <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c> or <c>ApplicationInsights:ConnectionString</c> is set.
    /// </summary>
    public static WebApplicationBuilder AddAoviasOpenTelemetry(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var configuration = builder.Configuration;
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        var appInsights =
            configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? configuration["ApplicationInsights:ConnectionString"]
            ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

        var useOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);
        var useAzureMonitor = !string.IsNullOrWhiteSpace(appInsights);

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (useOtlp)
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (useOtlp)
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
                }
            });

        if (useAzureMonitor)
        {
            openTelemetry.UseAzureMonitor(options => options.ConnectionString = appInsights);
        }

        return builder;
    }
}
