using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Native AOVIAS observability platform settings.
/// </summary>
public sealed class ObservabilitySettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Observability";

    public bool Enabled { get; set; }

    [Range(1, 3600)]
    public int SnapshotIntervalSeconds { get; set; } = 60;

    [Range(1, 3600)]
    public int MetricIntervalSeconds { get; set; } = 30;

    [Range(100, 100000)]
    public int TimelineMaxEntries { get; set; } = 5000;

    [Range(10, 10000)]
    public int SnapshotRetentionCount { get; set; } = 100;

    [Range(1, 3600)]
    public int SignalRBroadcastDebounceMilliseconds { get; set; } = 250;

    [Range(1, 86400)]
    public int EventRetentionSeconds { get; set; } = 86400;

    public bool PollingDisabled { get; set; }

    public bool DockerEventListenerEnabled { get; set; }
}
