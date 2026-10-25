using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Provider engine configuration settings.
/// </summary>
public sealed class ProviderSettings
{
    public const string SectionName = "Providers";

    public bool DockerEnabled { get; set; }

    public bool FileSystemEnabled { get; set; }

    public bool HealthEnabled { get; set; }

    public bool NetworkEnabled { get; set; }

    public bool NginxEnabled { get; set; }

    public bool CertificateEnabled { get; set; }

    [Required]
    public string RuntimeEndpoint { get; set; } = string.Empty;
}
