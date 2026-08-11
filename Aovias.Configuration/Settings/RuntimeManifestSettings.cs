using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Runtime manifest file location settings.
/// </summary>
public sealed class RuntimeManifestSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "RuntimeManifest";

    [Required]
    public string ManifestPath { get; set; } = string.Empty;
}
