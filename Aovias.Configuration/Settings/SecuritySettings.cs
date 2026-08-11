using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Settings;

/// <summary>
/// Security settings including JWT placeholders and API keys.
/// </summary>
public sealed class SecuritySettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Security";

    [Required]
    public string JwtIssuer { get; set; } = string.Empty;

    [Required]
    public string JwtAudience { get; set; } = string.Empty;

    [Required]
    public string SigningKey { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional client certificate thumbprint for mTLS connections to the runtime agent.
    /// </summary>
    public string CertificateThumbprint { get; set; } = string.Empty;
}
