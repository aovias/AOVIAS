namespace Aovias.Deployment.Contracts.Identity;

/// <summary>
/// Unified Identity deployment contract published alongside standard service contracts.
/// </summary>
public sealed class IdentityDeploymentContract
{
    public string Service { get; init; } = "identity";

    public string ContractVersion { get; init; } = "1.0";

    public required SigningCredentialContract SigningCredential { get; init; }

    public IReadOnlyList<IdentityFilesystemEntryContract> Filesystem { get; init; } = [];

    public IdentityDatabaseContract Database { get; init; } = new();

    public IdentityHealthContract Health { get; init; } = new();

    public IdentityJwtContract Jwt { get; init; } = new();

    public IReadOnlyList<string> Dependencies { get; init; } = [];
}

public sealed class SigningCredentialContract
{
    public string Type { get; init; } = "Pfx";

    public required string Path { get; init; }

    public string? PrivateKeyPath { get; init; }

    public string PasswordSecret { get; init; } = "certificatePassword";

    public string? ExpectedThumbprint { get; init; }
}

public sealed class IdentityFilesystemEntryContract
{
    public required string Path { get; init; }

    public string Permission { get; init; } = "775";

    public string Purpose { get; init; } = string.Empty;
}

public sealed class IdentityDatabaseContract
{
    public string Provider { get; init; } = "SqlServer";

    public bool Required { get; init; }

    public string ConnectionSetting { get; init; } = "ConnectionStrings:DefaultConnection";

    public string Migration { get; init; } = "Automatic";

    public string Seed { get; init; } = "DevelopmentOnly";
}

public sealed class IdentityHealthContract
{
    public string HealthUrl { get; init; } = "/health";

    public int ExpectedStatus { get; init; } = 200;

    public int TimeoutSeconds { get; init; } = 15;

    public int Retries { get; init; } = 3;
}

public sealed class IdentityJwtContract
{
    public string IssuerConfigurationKey { get; init; } = "JwtSettings:Issuer";

    public string AudienceConfigurationKey { get; init; } = "JwtSettings:Audience";

    public string SecretKeyConfigurationKey { get; init; } = "JwtSettings:SecretKey";
}

/// <summary>
/// Explicit secrets contract for Identity deployments.
/// </summary>
public sealed class IdentitySecretsContract
{
    public string Service { get; init; } = "identity";

    public string ContractVersion { get; init; } = "1.0";

    public IReadOnlyList<IdentitySecretEntryContract> Secrets { get; init; } = [];
}

public sealed class IdentitySecretEntryContract
{
    public required string Name { get; init; }

    public bool Required { get; init; }

    public string Source { get; init; } = "secrets/settings.secrets.json";

    public string ConfigurationKey { get; init; } = string.Empty;
}
