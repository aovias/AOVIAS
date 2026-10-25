using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Root runtime profile metadata and active profile selection.
/// </summary>
public sealed class AoviasPlatformOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Aovias";

    /// <summary>
    /// Gets or sets the configuration root directory path.
    /// </summary>
    [Required]
    public string ConfigRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the profiles directory name relative to <see cref="ConfigRoot"/>.
    /// </summary>
    [Required]
    public string ProfilesDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the active runtime profile name.
    /// </summary>
    [Required]
    public string ActiveProfile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secrets directory name relative to <see cref="ConfigRoot"/>.
    /// </summary>
    [Required]
    public string SecretsDirectory { get; set; } = string.Empty;
}
