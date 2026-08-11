using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Docker engine connectivity and storage settings.
/// </summary>
public sealed class DockerSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Docker";

    public bool Enabled { get; set; }

    [Required]
    public string Host { get; set; } = string.Empty;

    [Required]
    public string Socket { get; set; } = string.Empty;

    [Required]
    public string ApiVersion { get; set; } = string.Empty;

    [Required]
    public string Network { get; set; } = string.Empty;

    [Required]
    public string ComposeRoot { get; set; } = string.Empty;

    [Required]
    public string VolumesRoot { get; set; } = string.Empty;
}
