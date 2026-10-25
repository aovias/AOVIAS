using Aovias.Configuration.Settings;
using Serilog;
using Serilog.Events;

namespace Aovias.Runtime.Agent.Infrastructure.Logging;

/// <summary>
/// Configures Serilog rolling file logging for the runtime agent host.
/// </summary>
public static class SerilogBootstrapper
{
    /// <summary>
    /// Creates the Serilog logger from validated logging settings.
    /// </summary>
    /// <param name="settings">Validated logging settings.</param>
    /// <returns>The configured Serilog logger.</returns>
    public static Serilog.ILogger CreateLogger(ResolvedLoggingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!Enum.TryParse<LogEventLevel>(settings.MinimumLevel, ignoreCase: true, out var minimumLevel))
        {
            minimumLevel = LogEventLevel.Information;
        }

        return new LoggerConfiguration()
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Aovias.Runtime.Agent")
            .WriteTo.File(
                Path.Combine(settings.LogDirectory, $"{settings.FileNamePrefix}.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: settings.RetainedFileCountLimit,
                shared: true)
            .CreateLogger();
    }
}
