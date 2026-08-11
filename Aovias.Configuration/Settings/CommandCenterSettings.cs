namespace Aovias.Configuration.Settings;

/// <summary>
/// Operations Command Center settings (Sprint M2.7) — polling cadence and offline detection for
/// <c>RuntimeAgentPollingService</c>. Deliberately minimal: the Command Center's own aggregation
/// endpoints (Fleet Health/Drift/Reconciliation/Metrics) read only the cache this polling loop
/// maintains, never calling an agent live, so these two values are the only knobs that matter for
/// "how fresh is fleet data" and "how long before an unresponsive agent is considered offline."
/// </summary>
public sealed class CommandCenterSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "CommandCenter";

    public int PollIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// An environment is considered offline once this many seconds have passed since its last
    /// successful heartbeat — deliberately a multiple of <see cref="PollIntervalSeconds"/> so a
    /// single missed cycle (e.g. a transient network blip) does not flip an environment offline.
    /// </summary>
    public int OfflineThresholdSeconds { get; set; } = 90;
}
