using ComplianceServiceManager.Models.LocalResources;

namespace ComplianceServiceManager.Services.LocalResources;

public sealed class LocalResourceMonitoringOptions
{
    /// <summary>Default polling interval for UI refresh (observation only).</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Path used to resolve the monitored disk volume (typically AOVIAS development root).
    /// Prefer a configurable path; avoid hard-coding drive letters as product assumptions.
    /// </summary>
    public string DiskProbePath { get; set; } = @"V:\DevelopmentRepo";

    public LocalResourceThresholds Thresholds { get; set; } = LocalResourceThresholds.Default;
}
