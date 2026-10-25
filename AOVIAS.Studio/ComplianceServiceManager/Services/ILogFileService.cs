using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

/// <summary>
/// Reads the structured per-service CLEF log files written by the PRAMAN services
/// under the shared log root (mirrors Shared.Logging.LogPathResolver).
/// </summary>
public interface ILogFileService
{
    /// <summary>Absolute path to the shared log root (e.g. &lt;solution&gt;/logs).</summary>
    string ResolveLogRoot();

    /// <summary>Service names that currently have a log folder.</summary>
    IReadOnlyList<string> ListServices();

    /// <summary>CLEF files for a service, newest first.</summary>
    IReadOnlyList<string> ListLogFiles(string serviceName);

    /// <summary>Reads up to <paramref name="maxEntries"/> most recent entries for a service.</summary>
    Task<IReadOnlyList<ServiceLogEntry>> ReadEntriesAsync(string serviceName, int maxEntries, CancellationToken cancellationToken);

    /// <summary>Reads the most recent failures (Error/Fatal) for a service.</summary>
    Task<IReadOnlyList<ServiceLogEntry>> GetLatestFailuresAsync(string serviceName, int maxEntries, CancellationToken cancellationToken);

    /// <summary>Zips the service's log files to a temp location and returns the archive path.</summary>
    Task<string?> ExportAsync(string serviceName, CancellationToken cancellationToken);
}
