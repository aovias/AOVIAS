using ComplianceServiceManager.Models.LocalResources;

namespace ComplianceServiceManager.Services.LocalResources;

/// <summary>
/// Local host resource telemetry for AOVIAS Studio (sensor layer for future D5).
/// Does not select profiles, start/stop services, or reinterpret Runtime Agent metrics.
/// </summary>
public interface ILocalResourceMonitoringService : IDisposable
{
    LocalResourceMonitoringOptions Options { get; }

    LocalResourceSnapshot? LatestSnapshot { get; }

    event Action<LocalResourceSnapshot>? SnapshotUpdated;

    /// <summary>Starts the single centralized polling loop (idempotent).</summary>
    void Start();

    /// <summary>Stops polling without disposing the service permanently.</summary>
    void Stop();

    /// <summary>
    /// Captures one snapshot. Host CPU requires a prior sample for a meaningful percentage;
    /// the first call may return CpuPercent = null (warm-up).
    /// </summary>
    LocalResourceSnapshot CaptureSnapshot(IEnumerable<(string ServiceName, int? ProcessId)>? services = null);
}
