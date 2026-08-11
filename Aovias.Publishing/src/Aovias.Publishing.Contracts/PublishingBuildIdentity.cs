namespace Aovias.Publishing.Contracts;

/// <summary>
/// Authoritative build identity for a single publish run (M1.2 D3).
/// </summary>
public sealed class PublishingBuildIdentity
{
    public required string SourceRevision { get; init; }

    public string? GitCommit { get; init; }

    public required DateTimeOffset BuildUtc { get; init; }

    public required string BuildVersion { get; init; }

    public required string PublishingInputFingerprint { get; init; }

    public string? PackageFingerprint { get; set; }

    public string? DockerImageIdentity { get; set; }

    public required string PublishingEngineVersion { get; init; }

    public required int FingerprintPolicyVersion { get; init; }

    public string BuildConfiguration { get; init; } = "Release";

    public string? TargetFramework { get; init; }

    public string? RuntimeIdentifier { get; init; }

    public string Profile { get; init; } = "local";
}

/// <summary>
/// Machine-readable publishing build manifest written into every deployment package (M1.2 D7).
/// </summary>
public sealed class PublishingBuildManifest
{
    public int SchemaVersion { get; init; } = 1;

    public required string SourceRevision { get; init; }

    public string? GitCommit { get; init; }

    public required DateTimeOffset BuildUtc { get; init; }

    public required string BuildVersion { get; init; }

    public required string PublishingInputFingerprint { get; init; }

    public string? PackageFingerprint { get; init; }

    public string? DockerImageIdentity { get; init; }

    public required string PublishingEngineVersion { get; init; }

    public required int FingerprintPolicyVersion { get; init; }

    public string BuildConfiguration { get; init; } = "Release";

    public string? TargetFramework { get; init; }

    public string? RuntimeIdentifier { get; init; }

    public required string Profile { get; init; }

    public IReadOnlyDictionary<string, string> ArtifactFingerprints { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public static PublishingBuildManifest FromIdentity(
        PublishingBuildIdentity identity,
        IReadOnlyDictionary<string, string>? artifactFingerprints = null)
        => new()
        {
            SourceRevision = identity.SourceRevision,
            GitCommit = identity.GitCommit,
            BuildUtc = identity.BuildUtc,
            BuildVersion = identity.BuildVersion,
            PublishingInputFingerprint = identity.PublishingInputFingerprint,
            PackageFingerprint = identity.PackageFingerprint,
            DockerImageIdentity = identity.DockerImageIdentity,
            PublishingEngineVersion = identity.PublishingEngineVersion,
            FingerprintPolicyVersion = identity.FingerprintPolicyVersion,
            BuildConfiguration = identity.BuildConfiguration,
            TargetFramework = identity.TargetFramework,
            RuntimeIdentifier = identity.RuntimeIdentifier,
            Profile = identity.Profile,
            ArtifactFingerprints = artifactFingerprints ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
}
