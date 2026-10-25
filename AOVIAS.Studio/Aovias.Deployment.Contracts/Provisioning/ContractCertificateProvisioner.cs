using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Aovias.Deployment.Contracts.Provisioning;

/// <summary>
/// Provisions required certificates declared by a certificate contract.
/// </summary>
public static class ContractCertificateProvisioner
{
    public const string DefaultDemoPassword = "aovias-demo-signing-cert";

    public static void EnsureProvisioned(string configRoot, CertificateContract contract)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configRoot);
        ArgumentNullException.ThrowIfNull(contract);

        foreach (var requirement in contract.Certificates.Where(item => item.Required))
        {
            EnsureCertificateRequirement(configRoot, requirement);
        }
    }

    private static void EnsureCertificateRequirement(string configRoot, CertificateRequirementContract requirement)
    {
        var certificatePath = ResolveCertificatePath(configRoot, requirement);
        var password = ResolveCertificatePassword(configRoot, requirement.SecretKey) ?? DefaultDemoPassword;
        if (File.Exists(certificatePath) && CertificateLoads(certificatePath, password))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(certificatePath)!);
        File.WriteAllBytes(certificatePath, CreateSelfSignedPfx(password));
    }

    public static string ResolveCertificatePath(string configRoot, CertificateRequirementContract requirement)
    {
        var mountPath = requirement.MountPath.TrimEnd('/');
        if (mountPath.StartsWith(PlatformRuntimeDefaults.ConfigRoot, StringComparison.Ordinal))
        {
            var relative = mountPath[PlatformRuntimeDefaults.ConfigRoot.Length..].TrimStart('/');
            var hostRoot = Path.Combine(configRoot, relative.Replace('/', Path.DirectorySeparatorChar));
            return Path.Combine(hostRoot, requirement.FileName);
        }

        return Path.Combine(configRoot, requirement.FileName);
    }

    private static bool CertificateLoads(string certificatePath, string password)
    {
        try
        {
            using var certificate = X509CertificateLoader.LoadPkcs12FromFile(certificatePath, password);
            return certificate.HasPrivateKey;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public static string? ResolveCertificatePassword(string configRoot, string secretKey)
    {
        var secretsRoot = Path.Combine(configRoot, "secrets");
        if (!Directory.Exists(secretsRoot))
        {
            return null;
        }

        foreach (var secretsFile in Directory.EnumerateFiles(secretsRoot, "settings.secrets.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(secretsFile));
                if (document.RootElement.TryGetProperty(secretKey, out var passwordElement)
                    && passwordElement.ValueKind == JsonValueKind.String)
                {
                    var password = passwordElement.GetString();
                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        return password;
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed secrets during provisioning.
            }
        }

        return null;
    }

    private static byte[] CreateSelfSignedPfx(string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Aovias Signing",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));

        return certificate.Export(X509ContentType.Pfx, password);
    }
}
