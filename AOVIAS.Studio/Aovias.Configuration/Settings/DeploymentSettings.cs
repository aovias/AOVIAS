using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Deployment pipeline settings for autonomous package deployment.
/// </summary>
public sealed class DeploymentSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Deployment";

    public bool Enabled { get; set; }

    [Required]
    public string AgentVersion { get; set; } = string.Empty;

    [Required]
    public string PackageRoot { get; set; } = string.Empty;

    [Required]
    public string HealthCheckBaseUrl { get; set; } = string.Empty;

    [Range(1, 3600)]
    public int HealthCheckTimeoutSeconds { get; set; }

    [Range(0, 60)]
    public int HealthCheckRetryCount { get; set; }

    [Range(0, 60000)]
    public int HealthCheckRetryDelayMilliseconds { get; set; }

    public bool AutomaticRollbackEnabled { get; set; }

    [Required]
    public string ComposeProjectNamePrefix { get; set; } = string.Empty;

    [Range(1, 3600)]
    public int ContainerHealthTimeoutSeconds { get; set; }

    [Range(500, 60000)]
    public int ContainerHealthPollIntervalMilliseconds { get; set; }
}
