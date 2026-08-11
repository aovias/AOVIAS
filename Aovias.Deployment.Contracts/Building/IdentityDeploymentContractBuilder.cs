using Aovias.Deployment.Contracts.Identity;

namespace Aovias.Deployment.Contracts.Building;

/// <summary>
/// Builds Identity-specific deployment contract documents from a canonical service bundle.
/// </summary>
public static class IdentityDeploymentContractBuilder
{
    public const string IdentityServiceId = "identity-service";

    public static bool IsIdentityService(ServiceDeploymentBundle bundle)
        => string.Equals(bundle.Manifest.ServiceId, IdentityServiceId, StringComparison.OrdinalIgnoreCase);

    public static IdentityDeploymentContract BuildIdentityContract(ServiceDeploymentBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var signingRequirement = bundle.Certificate.Certificates.FirstOrDefault(item =>
            item.Required && string.Equals(item.Category, "Signing", StringComparison.OrdinalIgnoreCase))
            ?? bundle.Certificate.Certificates.FirstOrDefault(item => item.Required)
            ?? throw new InvalidOperationException("Identity deployment contract requires a signing certificate requirement.");

        var signingPath = $"{signingRequirement.MountPath.TrimEnd('/')}/{signingRequirement.FileName}";

        return new IdentityDeploymentContract
        {
            Service = "identity",
            ContractVersion = ServiceContractBuilder.ContractVersion,
            SigningCredential = new SigningCredentialContract
            {
                Type = NormalizeSigningType(signingRequirement.Type),
                Path = signingPath,
                PasswordSecret = signingRequirement.SecretKey,
                ExpectedThumbprint = null
            },
            Filesystem = bundle.Filesystem.Directories
                .Concat(bundle.Filesystem.ReadOnlyDirectories)
                .Select(item => new IdentityFilesystemEntryContract
                {
                    Path = item.Path,
                    Permission = item.Permission,
                    Purpose = item.Purpose
                })
                .ToList(),
            Database = new IdentityDatabaseContract
            {
                Provider = bundle.Database.Provider,
                Required = bundle.Database.Required,
                ConnectionSetting = bundle.Database.ConnectionSetting,
                Migration = bundle.Database.Migration,
                Seed = bundle.Database.Seed
            },
            Health = new IdentityHealthContract
            {
                HealthUrl = bundle.Health.HealthUrl,
                ExpectedStatus = bundle.Health.ExpectedStatus,
                TimeoutSeconds = bundle.Health.TimeoutSeconds,
                Retries = bundle.Health.Retries
            },
            Jwt = new IdentityJwtContract(),
            Dependencies = bundle.Dependency.Dependencies
                .Where(item => item.Required)
                .Select(item => item.Service)
                .ToList()
        };
    }

    public static IdentitySecretsContract BuildIdentitySecretsContract(ServiceDeploymentBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        return new IdentitySecretsContract
        {
            Service = "identity",
            ContractVersion = ServiceContractBuilder.ContractVersion,
            Secrets = bundle.Secret.Secrets
                .Select(item => new IdentitySecretEntryContract
                {
                    Name = item.Name,
                    Required = item.Required,
                    Source = item.Source,
                    ConfigurationKey = item.ConfigurationKey
                })
                .ToList()
        };
    }

    private static string NormalizeSigningType(string type)
        => type.ToUpperInvariant() switch
        {
            "PFX" or "P12" or "PKCS12" => "Pfx",
            "PEM" => "Pem",
            "KEYFILE" or "JWK" => "KeyFile",
            _ => type
        };
}
