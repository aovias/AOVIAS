using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Aovias.Deployment.Contracts.Provisioning;

/// <summary>
/// Validates host-side certificates declared by a deployment contract before container startup.
/// </summary>
public static class ContractCertificateValidator
{
    public static CertificateValidationReport Validate(string configRoot, CertificateContract contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configRoot);
        ArgumentNullException.ThrowIfNull(contract);

        var results = contract.Certificates
            .Where(item => item.Required)
            .Select(item => ValidateRequirement(configRoot, item))
            .ToList();

        return results.All(item => item.Passed)
            ? CertificateValidationReport.Success(results)
            : CertificateValidationReport.Failure(results);
    }

    public static CertificateValidationItemResult ValidateRequirement(
        string configRoot,
        CertificateRequirementContract requirement)
    {
        var errors = new List<string>();
        var certificatePath = ContractCertificateProvisioner.ResolveCertificatePath(configRoot, requirement);
        var normalizedType = requirement.Type.ToUpperInvariant();

        if (!File.Exists(certificatePath))
        {
            errors.Add($"Certificate file does not exist: '{certificatePath}'.");
            return Failure(requirement.Id, certificatePath, errors);
        }

        try
        {
            using var stream = File.OpenRead(certificatePath);
            if (stream.Length == 0)
            {
                errors.Add($"Certificate file is empty: '{certificatePath}'.");
            }
        }
        catch (Exception exception)
        {
            errors.Add($"Certificate file is not readable: '{certificatePath}' ({exception.Message}).");
            return Failure(requirement.Id, certificatePath, errors);
        }

        if (IsPfxType(normalizedType))
        {
            return ValidatePfx(configRoot, requirement, certificatePath, errors);
        }

        if (string.Equals(normalizedType, "PEM", StringComparison.Ordinal))
        {
            return ValidatePem(requirement, certificatePath, errors);
        }

        errors.Add($"Unsupported certificate type '{requirement.Type}' for '{requirement.Id}'.");
        return Failure(requirement.Id, certificatePath, errors);
    }

    private static CertificateValidationItemResult ValidatePfx(
        string configRoot,
        CertificateRequirementContract requirement,
        string certificatePath,
        List<string> errors)
    {
        var password = ResolveSecretValue(configRoot, requirement.SecretKey);
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(
                $"Password secret '{requirement.SecretKey}' is missing from settings.secrets.json. " +
                $"Required to open '{requirement.FileName}'.");
            return Failure(requirement.Id, certificatePath, errors);
        }

        try
        {
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password);
            return ValidateLoadedCertificate(requirement, certificatePath, certificate, errors, passwordProvided: true);
        }
        catch (CryptographicException)
        {
            errors.Add(
                $"Certificate password is incorrect for '{requirement.FileName}'. " +
                $"Expected secret '{requirement.SecretKey}' in settings.secrets.json.");
            return Failure(requirement.Id, certificatePath, errors);
        }
    }

    private static CertificateValidationItemResult ValidatePem(
        CertificateRequirementContract requirement,
        string certificatePath,
        List<string> errors)
    {
        try
        {
            using var certificate = X509CertificateLoader.LoadCertificateFromFile(certificatePath);
            return ValidateLoadedCertificate(requirement, certificatePath, certificate, errors, passwordProvided: false);
        }
        catch (CryptographicException exception)
        {
            errors.Add($"PEM certificate could not be parsed: '{certificatePath}' ({exception.Message}).");
            return Failure(requirement.Id, certificatePath, errors);
        }
    }

    private static CertificateValidationItemResult ValidateLoadedCertificate(
        CertificateRequirementContract requirement,
        string certificatePath,
        X509Certificate2 certificate,
        List<string> errors,
        bool passwordProvided)
    {
        if (!certificate.HasPrivateKey)
        {
            errors.Add($"Certificate '{requirement.Id}' does not contain a private key.");
        }

        if (certificate.NotAfter <= DateTime.UtcNow)
        {
            errors.Add($"Certificate '{requirement.Id}' expired at {certificate.NotAfter:u}.");
        }

        if (certificate.NotBefore > DateTime.UtcNow)
        {
            errors.Add($"Certificate '{requirement.Id}' is not yet valid until {certificate.NotBefore:u}.");
        }

        if (errors.Count > 0)
        {
            return Failure(requirement.Id, certificatePath, errors, certificate.Thumbprint, certificate.NotAfter);
        }

        return new CertificateValidationItemResult
        {
            CertificateId = requirement.Id,
            Passed = true,
            ResolvedPath = certificatePath,
            Thumbprint = certificate.Thumbprint,
            NotAfterUtc = certificate.NotAfter
        };
    }

    private static bool IsPfxType(string normalizedType)
        => normalizedType is "PFX" or "P12" or "PKCS12";

    private static string? ResolveSecretValue(string configRoot, string secretKey)
    {
        var fromSecrets = ContractCertificateProvisioner.ResolveCertificatePassword(configRoot, secretKey);
        if (!string.IsNullOrWhiteSpace(fromSecrets))
        {
            return fromSecrets;
        }

        return null;
    }

    private static CertificateValidationItemResult Failure(
        string certificateId,
        string certificatePath,
        IReadOnlyList<string> errors,
        string? thumbprint = null,
        DateTimeOffset? notAfterUtc = null)
        => new()
        {
            CertificateId = certificateId,
            Passed = false,
            ResolvedPath = certificatePath,
            Errors = errors,
            Thumbprint = thumbprint,
            NotAfterUtc = notAfterUtc
        };
}
