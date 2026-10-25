using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Certificate management settings.
/// </summary>
public sealed class CertificateSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Certificate";

    [Required]
    public string StoreLocation { get; set; } = string.Empty;

    [Required]
    public string CertificatePath { get; set; } = string.Empty;

    [Required]
    public string Thumbprint { get; set; } = string.Empty;
}
