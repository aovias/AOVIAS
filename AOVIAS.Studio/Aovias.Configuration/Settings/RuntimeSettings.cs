using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Runtime identity and version settings for the active profile.
/// </summary>
public sealed class RuntimeSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Runtime";

    [Required]
    public string Environment { get; set; } = string.Empty;

    [Required]
    public string ServerName { get; set; } = string.Empty;

    [Required]
    public string Hostname { get; set; } = string.Empty;

    [Required]
    public string PlatformVersion { get; set; } = string.Empty;

    [Required]
    public string RuntimeVersion { get; set; } = string.Empty;

    [Required]
    public string ServerId { get; set; } = string.Empty;

    [Required]
    public string AgentDisplayName { get; set; } = string.Empty;
}
