using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

// CLEF files use the *rendered* compact formatter so the "@m" field holds a human-readable
// message (consumed by the Compliance Service Manager and AI diagnostics) in addition to
// the structured properties.

namespace Shared.Logging;

/// <summary>
/// Centralised Serilog configuration shared by every PRAMAN service and worker.
///
/// Usage (top of Program.cs):
///   SerilogBootstrap.CreateBootstrapLogger("ComplianceService");
///   var builder = WebApplication.CreateBuilder(args);
///   builder.ConfigureSerilog("ComplianceService");
/// </summary>
public static class SerilogBootstrap
{
    private const long FileSizeLimitBytes = 100L * 1024 * 1024; // 100 MB per file before size roll
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

    /// <summary>
    /// Stage 1: install a minimal bootstrap logger BEFORE the host is built so that
    /// failures during configuration / DI wiring / startup migrations are captured.
    /// Replaced by the fully-configured logger once the host is built.
    /// </summary>
    public static void CreateBootstrapLogger(string serviceName)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                              ?? "Production";
        var root = LogPathResolver.ResolveBootstrapRoot(environmentName);

        Directory.CreateDirectory(LogPathResolver.ServiceLogDirectory(root, serviceName));

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty(LoggingConstants.Properties.Service, serviceName)
            .Enrich.WithProperty(LoggingConstants.Properties.LogType, LoggingConstants.LogTypes.Operational)
            .WriteTo.Console()
            .WriteTo.File(
                formatter: new RenderedCompactJsonFormatter(),
                path: Path.Combine(LogPathResolver.ServiceLogDirectory(root, serviceName), $"{Slug(serviceName)}-bootstrap-.clef"),
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: RetentionPeriod,
                shared: true)
            .CreateBootstrapLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Fatal(ex, "Unhandled domain exception in {Service}. {EventCode}", serviceName, EventCode.StartupFailed);
            }
            Log.CloseAndFlush();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, _) => Log.CloseAndFlush();
    }

    /// <summary>
    /// Stage 2: register the fully-configured logger in DI. Works for both
    /// <c>WebApplicationBuilder</c> and the generic <c>HostApplicationBuilder</c>
    /// (worker), since both implement <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public static TBuilder ConfigureSerilog<TBuilder>(this TBuilder builder, string serviceName)
        where TBuilder : IHostApplicationBuilder
    {
        var root = LogPathResolver.Resolve(builder.Configuration, builder.Environment.EnvironmentName);
        var environmentName = builder.Environment.EnvironmentName;
        var isDevelopment = builder.Environment.IsDevelopment();

        builder.Services.AddSerilog((_, loggerConfiguration) =>
            ConfigureLogger(loggerConfiguration, builder.Configuration, serviceName, environmentName, root, isDevelopment));

        return builder;
    }

    private static void ConfigureLogger(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string serviceName,
        string environmentName,
        string root,
        bool isDevelopment)
    {
        var serviceDir = LogPathResolver.ServiceLogDirectory(root, serviceName);
        var auditDir = LogPathResolver.AuditLogDirectory(root);
        Directory.CreateDirectory(serviceDir);
        Directory.CreateDirectory(auditDir);

        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProperty(LoggingConstants.Properties.Service, serviceName)
            .Enrich.WithProperty(LoggingConstants.Properties.Environment, environmentName)
            // Default every event to the operational stream; the audit logger overrides this.
            .Enrich.WithProperty(LoggingConstants.Properties.LogType, LoggingConstants.LogTypes.Operational)
            // Allow per-environment overrides (e.g. MinimumLevel) via a "Serilog" config section.
            .ReadFrom.Configuration(configuration)
            // Primary per-service operational CLEF file.
            .WriteTo.File(
                formatter: new RenderedCompactJsonFormatter(),
                path: Path.Combine(serviceDir, $"{Slug(serviceName)}-.clef"),
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: FileSizeLimitBytes,
                retainedFileTimeLimit: RetentionPeriod,
                shared: true)
            // Dedicated, separate audit stream (security / audit events only).
            .WriteTo.Conditional(
                IsAuditEvent,
                wt => wt.File(
                    formatter: new RenderedCompactJsonFormatter(),
                    path: Path.Combine(auditDir, $"{Slug(serviceName)}-audit-.clef"),
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    retainedFileTimeLimit: RetentionPeriod,
                    shared: true));

        if (isDevelopment)
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {EventCode} {Message:lj} {NewLine}{Exception}");
        }
    }

    private static bool IsAuditEvent(LogEvent logEvent)
        => logEvent.Properties.TryGetValue(LoggingConstants.Properties.LogType, out var value)
           && value is ScalarValue { Value: string logType }
           && string.Equals(logType, LoggingConstants.LogTypes.Audit, StringComparison.Ordinal);

    private static string Slug(string serviceName) => serviceName.ToLowerInvariant();
}
