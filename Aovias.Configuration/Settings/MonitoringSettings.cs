using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Monitoring and telemetry settings.
/// </summary>
public sealed class MonitoringSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Monitoring";

    public bool Enabled { get; set; }

    [Required]
    public string MetricsEndpoint { get; set; } = string.Empty;

    [Range(1, 3600)]
    public int CollectionIntervalSeconds { get; set; }
}
