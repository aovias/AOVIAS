namespace IdentityService.Configuration;

/// <summary>
/// Structured startup failure for Identity signing credential misconfiguration.
/// </summary>
public sealed class SigningCredentialValidationException : Exception
{
    public SigningCredentialValidationException(
        string reason,
        string expected,
        string? configuredCertificate = null)
        : base(FormatMessage(reason, expected, configuredCertificate))
    {
        Reason = reason;
        Expected = expected;
        ConfiguredCertificate = configuredCertificate;
    }

    public string Category => "Signing Credential";

    public string Reason { get; }

    public string Expected { get; }

    public string? ConfiguredCertificate { get; }

    public static SigningCredentialValidationException PasswordIncorrect(
        string secretName,
        string configuredCertificate)
        => new(
            "Certificate password incorrect.",
            $"Secret: {secretName}",
            configuredCertificate);

    public static SigningCredentialValidationException MissingCertificate(
        string expectedPath,
        string secretName)
        => new(
            "Signing certificate is unavailable.",
            $"Deploy '{expectedPath}' and provide secret '{secretName}' in settings.secrets.json.",
            expectedPath);

    public static SigningCredentialValidationException MissingPasswordSecret(string secretName, string configuredCertificate)
        => new(
            "Certificate password secret is missing.",
            $"Secret: {secretName}",
            configuredCertificate);

    private static string FormatMessage(string reason, string expected, string? configuredCertificate)
    {
        const string category = "Signing Credential";
        var certificateLine = string.IsNullOrWhiteSpace(configuredCertificate)
            ? string.Empty
            : $"\nConfigured Certificate: {configuredCertificate}";
        return
            $"{category}\nFAIL\nReason: {reason}\nExpected: {expected}{certificateLine}\nDeployment Blocked";
    }
}
