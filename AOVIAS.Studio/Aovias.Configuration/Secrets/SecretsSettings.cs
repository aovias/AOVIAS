using System.ComponentModel.DataAnnotations;

namespace Aovias.Configuration.Secrets;

/// <summary>
/// Dedicated secrets and key material for the active profile.
/// </summary>
public sealed class SecretsSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Secrets";

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string JwtSigningKey { get; set; } = string.Empty;

    [Required]
    public string DatabaseConnectionString { get; set; } = string.Empty;

    [Required]
    public string DockerRegistryPassword { get; set; } = string.Empty;

    [Required]
    public string CertificatePassword { get; set; } = string.Empty;

    [Required]
    public string EncryptionKey { get; set; } = string.Empty;
}
