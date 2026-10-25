namespace Aovias.Publishing.Contracts;

/// <summary>
/// Deploy package manifest written to <c>manifest.json</c>.
/// </summary>
public sealed class DeployManifest
{
    public required string Service { get; init; }

    public required string Version { get; init; }

    public required string Type { get; init; }

    public required string Container { get; init; }

    public required string Health { get; init; }

    public required string EntryPoint { get; init; }

    public IReadOnlyList<int> Ports { get; init; } = [];

    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public required string Configuration { get; init; }

    public required string ImageName { get; init; }

    public required string ImageTag { get; init; }

    public string? SourceRevision { get; init; }

    public string? PublishingInputFingerprint { get; init; }

    public string? DockerImageIdentity { get; init; }
}

/// <summary>
/// Health definition written to <c>health.json</c>.
/// </summary>
public sealed class DeployHealthDefinition
{
    public required string Endpoint { get; init; }

    public int ExpectedStatus { get; init; } = 200;

    public int TimeoutSeconds { get; init; } = 15;

    public int Retries { get; init; } = 3;
}

/// <summary>
/// Environment-neutral package metadata written to <c>metadata.json</c>.
/// </summary>
public sealed class DeployPackageMetadata
{
    public required string Publisher { get; init; }

    public required string PublishingEngineVersion { get; init; }

    public required string PackageVersion { get; init; }

    public required string PackageType { get; init; }

    public required DateTimeOffset CreatedUtc { get; init; }

    public string? SourceRevision { get; init; }

    public string? PublishingInputFingerprint { get; init; }

    public string? PackageFingerprint { get; init; }

    public string? DockerImageIdentity { get; init; }

    public int FingerprintPolicyVersion { get; init; } = FingerprintPolicyVersionDefault;

    public const int FingerprintPolicyVersionDefault = 1;
}

/// <summary>
/// Deployment readiness certificate written after DRV passes.
/// </summary>
public sealed class DeploymentReadinessCertificate
{
    public required string ValidationId { get; init; }

    public required DateTimeOffset ValidatedUtc { get; init; }

    public required string DrvVersion { get; init; }

    public required string RuntimeCompatibility { get; init; }

    public required string PackageChecksum { get; init; }

    public required int ValidationScore { get; init; }

    public IReadOnlyList<ValidationStageResult> ValidatorResults { get; init; } = [];
}

/// <summary>
/// Result of verifying a generated <c>.aovdeploy</c> package (D6).
/// </summary>
public sealed class DeployPackageVerificationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> VerifiedFiles { get; init; } = [];

    public int ChecksumCount { get; init; }

    public bool HasCertificate { get; init; }

    public static DeployPackageVerificationResult Success(IReadOnlyList<string> verifiedFiles, int checksumCount, bool hasCertificate)
        => new()
        {
            VerifiedFiles = verifiedFiles,
            ChecksumCount = checksumCount,
            HasCertificate = hasCertificate
        };

    public static DeployPackageVerificationResult Failure(IReadOnlyList<string> errors)
        => new() { Errors = errors };
}

/// <summary>
/// Parsed contents of a <c>.aovdeploy</c> package.
/// </summary>
public sealed class DeployPackageContents
{
    public required string PackagePath { get; init; }

    public required DeployManifest Manifest { get; init; }

    public required DeployHealthDefinition Health { get; init; }

    public required DeployPackageMetadata Metadata { get; init; }

    public required IReadOnlyDictionary<string, string> Checksums { get; init; }

    public required IReadOnlyDictionary<string, string> ConfigurationTemplate { get; init; }

    public string ConfigurationTemplateContent { get; init; } = string.Empty;

    public string ComposeContent { get; init; } = string.Empty;

    public string EnvTemplateContent { get; init; } = string.Empty;

    public bool HasServiceImage { get; init; }

    public DeploymentReadinessCertificate? ReadinessCertificate { get; init; }

    public Aovias.Deployment.Contracts.ServiceDeploymentBundle? DeploymentContracts { get; init; }
}
