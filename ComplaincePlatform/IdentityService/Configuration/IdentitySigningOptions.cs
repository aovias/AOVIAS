namespace IdentityService.Configuration;

/// <summary>
/// IdentityServer token signing configuration for non-development deployments.
/// </summary>
public sealed class IdentitySigningOptions
{
    public const string SectionName = "Identity:Signing";

    /// <summary>
    /// Development uses ephemeral developer credentials.
    /// Certificate loads an X509/PFX file. KeyFile loads an existing PEM or JWK key.
    /// HSM is reserved for future integration.
    /// </summary>
    public IdentitySigningMode Mode { get; set; } = IdentitySigningMode.Development;

    public string? CertificatePath { get; set; }

    public string? CertificatePassword { get; set; }

    public string? KeyFile { get; set; }
}

public enum IdentitySigningMode
{
    Development,
    Certificate,
    KeyFile,
    Hsm
}
