using Duende.IdentityServer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace IdentityService.Configuration;

/// <summary>
/// Configures Duende IdentityServer signing credentials based on environment and configuration.
/// </summary>
public static class IdentitySigningCredentialConfigurer
{
    private const string CertificateFileName = "identity-signing.pfx";
    private const string DefaultCertificatesRoot = "/opt/aovias/runtime/config/certificates";
    internal const string CertificatePasswordSecretKey = "certificatePassword";
    internal const string FallbackDemoCertificatePassword = "aovias-demo-signing-cert";

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var options = BindOptions(configuration);
        ValidateNonDevelopmentOptions(options, environment.EnvironmentName);
        ValidateCredentialAvailability(configuration, options);
    }

    public static IIdentityServerBuilder Configure(
        IIdentityServerBuilder builder,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            builder.AddDeveloperSigningCredential();
            return builder;
        }

        var options = BindOptions(configuration);
        ValidateNonDevelopmentOptions(options, environment.EnvironmentName);
        ValidateCredentialAvailability(configuration, options);

        switch (options.Mode)
        {
            case IdentitySigningMode.Certificate:
                builder.AddSigningCredential(LoadSigningCertificate(configuration, options));
                break;

            case IdentitySigningMode.KeyFile:
                var credentials = LoadKeyFileSigningCredentials(options.KeyFile!);
                builder.AddSigningCredential(credentials.Key, credentials.Algorithm);
                break;

            default:
                throw new InvalidOperationException(
                    BuildMissingCredentialMessage(environment.EnvironmentName));
        }

        return builder;
    }

    private static IdentitySigningOptions BindOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection(IdentitySigningOptions.SectionName).Get<IdentitySigningOptions>();
        if (options is null)
        {
            throw new InvalidOperationException(BuildMissingCredentialMessage(null));
        }

        return ResolveOptions(configuration, options);
    }

    private static IdentitySigningOptions ResolveOptions(IConfiguration configuration, IdentitySigningOptions options)
    {
        if (options.Mode is not IdentitySigningMode.Certificate)
        {
            return options;
        }

        options.CertificatePath = ResolveCertificatePath(configuration, options.CertificatePath);
        return options;
    }

    private static X509Certificate2 LoadSigningCertificate(IConfiguration configuration, IdentitySigningOptions options)
    {
        if (!TryLoadSigningCertificate(configuration, options, out var certificate, out var error))
        {
            throw error ?? new InvalidOperationException(
                "Unable to load Identity signing certificate from configured or fallback locations.");
        }

        return certificate;
    }

    private static bool TryLoadSigningCertificate(
        IConfiguration configuration,
        IdentitySigningOptions options,
        out X509Certificate2 certificate,
        out Exception? error)
    {
        certificate = null!;
        error = null;
        var passwordCandidates = BuildCertificatePasswordCandidates(configuration, options).ToList();
        var pathCandidates = BuildCertificatePathCandidates(configuration, options.CertificatePath).ToList();
        var existingPaths = pathCandidates.Where(File.Exists).ToList();

        if (existingPaths.Count == 0)
        {
            var expectedPath = pathCandidates.FirstOrDefault()
                ?? Path.Combine(
                    configuration["AOVIAS_CONFIG_ROOT"] ?? "/opt/aovias/runtime/config",
                    "certificates",
                    CertificateFileName);
            error = SigningCredentialValidationException.MissingCertificate(expectedPath, CertificatePasswordSecretKey);
            return false;
        }

        if (string.IsNullOrWhiteSpace(ResolveCertificatePassword(configuration, options))
            && string.IsNullOrWhiteSpace(options.CertificatePassword))
        {
            error = SigningCredentialValidationException.MissingPasswordSecret(
                CertificatePasswordSecretKey,
                Path.GetFileName(existingPaths[0]));
            return false;
        }

        var passwordFailures = 0;
        foreach (var path in existingPaths)
        {
            foreach (var password in passwordCandidates)
            {
                try
                {
                    certificate = LoadCertificate(path, password);
                    options.CertificatePath = path;
                    return true;
                }
                catch (CryptographicException)
                {
                    passwordFailures++;
                }
            }
        }

        if (passwordFailures > 0)
        {
            error = SigningCredentialValidationException.PasswordIncorrect(
                CertificatePasswordSecretKey,
                Path.GetFileName(existingPaths[0]));
            return false;
        }

        error = SigningCredentialValidationException.MissingCertificate(
            existingPaths[0],
            CertificatePasswordSecretKey);
        return false;
    }

    private static IEnumerable<string> BuildCertificatePathCandidates(IConfiguration configuration, string? configuredPath)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            candidates.Add(configuredPath);
        }

        var configRoot = configuration["AOVIAS_CONFIG_ROOT"]
            ?? Environment.GetEnvironmentVariable("AOVIAS_CONFIG_ROOT")
            ?? "/opt/aovias/runtime/config";
        candidates.Add(Path.Combine(configRoot, "certificates", CertificateFileName));
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "signing", CertificateFileName));

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string?> BuildCertificatePasswordCandidates(
        IConfiguration configuration,
        IdentitySigningOptions options)
    {
        var seen = new HashSet<string?>(StringComparer.Ordinal);
        foreach (var candidate in new[]
                 {
                     options.CertificatePassword,
                     ResolveCertificatePassword(configuration, options),
                     FallbackDemoCertificatePassword,
                     string.Empty
                 })
        {
            if (seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static string ResolveCertificatePath(IConfiguration configuration, string? configuredPath)
    {
        foreach (var candidate in BuildCertificatePathCandidates(configuration, configuredPath))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return BuildCertificatePathCandidates(configuration, configuredPath).FirstOrDefault()
            ?? Path.Combine(
                configuration["AOVIAS_CONFIG_ROOT"] ?? "/opt/aovias/runtime/config",
                "certificates",
                CertificateFileName);
    }

    private static string? ResolveCertificatePassword(IConfiguration configuration, IdentitySigningOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CertificatePassword))
        {
            return options.CertificatePassword;
        }

        var configRoot = configuration["AOVIAS_CONFIG_ROOT"]
            ?? Environment.GetEnvironmentVariable("AOVIAS_CONFIG_ROOT");
        if (string.IsNullOrWhiteSpace(configRoot))
        {
            return null;
        }

        var secretsDirectoryName = configuration["AOVIAS_SECRETS_DIRECTORY"] ?? "secrets";
        var secretsRoot = Path.Combine(configRoot, secretsDirectoryName);
        if (!Directory.Exists(secretsRoot))
        {
            return null;
        }

        foreach (var secretsFile in Directory.EnumerateFiles(secretsRoot, "settings.secrets.json", SearchOption.AllDirectories))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(secretsFile));
                if (document.RootElement.TryGetProperty("certificatePassword", out var passwordElement)
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
                // Ignore malformed secrets files and continue searching.
            }
        }

        return null;
    }

    private static void ValidateNonDevelopmentOptions(IdentitySigningOptions options, string environmentName)
    {
        if (options.Mode is IdentitySigningMode.Development)
        {
            throw new InvalidOperationException(
                $"Identity:Signing:Mode cannot be 'Development' when ASPNETCORE_ENVIRONMENT is '{environmentName}'. " +
                "Configure Identity:Signing:Mode as 'Certificate' or 'KeyFile' for deployed environments.");
        }

        if (options.Mode is IdentitySigningMode.Hsm)
        {
            throw new InvalidOperationException(
                "Identity:Signing:Mode 'Hsm' is reserved for future HSM integration and is not yet supported.");
        }

        switch (options.Mode)
        {
            case IdentitySigningMode.Certificate:
                if (string.IsNullOrWhiteSpace(options.CertificatePath))
                {
                    throw new InvalidOperationException(
                        "Identity:Signing:CertificatePath is required when Mode is 'Certificate'. " +
                        "Set Identity__Signing__CertificatePath or mount the certificate at " +
                        $"'{DefaultCertificatesRoot}/{CertificateFileName}'.");
                }

                break;

            case IdentitySigningMode.KeyFile:
                if (string.IsNullOrWhiteSpace(options.KeyFile))
                {
                    throw new InvalidOperationException(
                        "Identity:Signing:KeyFile is required when Mode is 'KeyFile'.");
                }

                break;

            default:
                throw new InvalidOperationException(BuildMissingCredentialMessage(environmentName));
        }
    }

    private static void ValidateCredentialAvailability(IConfiguration configuration, IdentitySigningOptions options)
    {
        switch (options.Mode)
        {
            case IdentitySigningMode.Certificate:
                if (!TryLoadSigningCertificate(configuration, options, out _, out var error))
                {
                    throw error ?? new InvalidOperationException(
                        "Identity signing certificate is unavailable. Mount or deploy the signing credential before starting IdentityService.");
                }

                break;

            case IdentitySigningMode.KeyFile:
                EnsureReadableFile(options.KeyFile!, "Identity:Signing:KeyFile");
                break;
        }
    }

    private static void EnsureReadableFile(string path, string settingName)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"{settingName} points to a missing file: '{path}'. " +
                "Mount or deploy the signing credential before starting IdentityService. " +
                $"Expected contract path: '{DefaultCertificatesRoot}/{CertificateFileName}'.");
        }
    }

    private static X509Certificate2 LoadCertificate(string path, string? password)
    {
        var extension = Path.GetExtension(path);

        if (string.Equals(extension, ".pfx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".p12", StringComparison.OrdinalIgnoreCase))
        {
            return X509CertificateLoader.LoadPkcs12FromFile(path, password ?? string.Empty);
        }

        return X509CertificateLoader.LoadCertificateFromFile(path);
    }

    private static SigningCredentials LoadKeyFileSigningCredentials(string path)
    {
        var content = File.ReadAllText(path).Trim();

        if (content.StartsWith('{'))
        {
            var jwk = new JsonWebKey(content);
            var algorithm = string.IsNullOrWhiteSpace(jwk.Alg)
                ? SecurityAlgorithms.RsaSha256
                : jwk.Alg;

            return new SigningCredentials(jwk, algorithm);
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(content);

        return new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256);
    }

    private static string BuildMissingCredentialMessage(string? environmentName)
    {
        var environmentSuffix = string.IsNullOrWhiteSpace(environmentName)
            ? string.Empty
            : $" (environment: {environmentName})";

        return
            "Identity signing credentials must be configured for non-development deployments" +
            environmentSuffix +
            ". Set Identity:Signing:Mode to 'Certificate' or 'KeyFile' and provide the required path. " +
            "Developer signing credentials are permitted only when ASPNETCORE_ENVIRONMENT is Development.";
    }
}
