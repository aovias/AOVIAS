using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Network topology settings for the active profile.
/// </summary>
public sealed class NetworkSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Network";

    [Required]
    public string DockerNetwork { get; set; } = string.Empty;

    [Required]
    public string Subnet { get; set; } = string.Empty;

    [Required]
    public string Gateway { get; set; } = string.Empty;
}
