using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Deployment host and container path settings used when generating .env.template artifacts.
/// </summary>
public sealed class DeploymentHostSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DeploymentHost";

    [Required]
    public string DotNetEnvironment { get; set; } = "Production";

    [Required]
    public string ConfigRoot { get; set; } = string.Empty;

    [Required]
    public string PackagesRoot { get; set; } = string.Empty;

    [Required]
    public string LogsRoot { get; set; } = string.Empty;

    [Required]
    public string HostConfigRoot { get; set; } = string.Empty;

    [Required]
    public string HostPackagesRoot { get; set; } = string.Empty;

    [Required]
    public string HostLogsRoot { get; set; } = string.Empty;

    [Required]
    public string DockerNetwork { get; set; } = string.Empty;

    public string ProfilesDirectory { get; set; } = "profiles";

    public string SecretsDirectory { get; set; } = "secrets";
}
