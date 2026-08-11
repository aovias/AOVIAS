namespace Aovias.Publishing.Contracts.ReleaseLibrary;

/// <summary>
/// Persisted, authoritative Release Index document (Sprint M1.5 D4). One document
/// exists per release-library root, stored as <c>release-index.json</c>.
/// </summary>
public sealed record ReleaseIndexDocument
{
    public int SchemaVersion { get; init; } = 1;

    public DateTimeOffset LastUpdatedUtc { get; init; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<ReleaseArtifactRecord> Artifacts { get; init; } = [];
}

/// <summary>
/// Configurable retention policy (Sprint M1.5 D7).
/// </summary>
public sealed class ReleaseRetentionPolicy
{
    /// <summary>How many most-recent versions per service to always keep, regardless of state.</summary>
    public int KeepLatestVersionsPerService { get; init; } = 5;

    /// <summary>Artifacts older than this are eligible for archival (not deletion) if not protected.</summary>
    public TimeSpan? ArchiveAfter { get; init; }

    /// <summary>Service IDs whose artifacts are never deleted or archived by policy.</summary>
    public IReadOnlyCollection<string> ProtectedServiceIds { get; init; } = [];

    public static ReleaseRetentionPolicy Default { get; } = new();
}

/// <summary>
/// Outcome of evaluating retention policy against the index (Sprint M1.5 D7).
/// </summary>
public sealed record ReleaseRetentionPlan
{
    public IReadOnlyList<string> ArchiveCandidateArtifactIds { get; init; } = [];

    public IReadOnlyList<string> DeleteCandidateArtifactIds { get; init; } = [];

    public IReadOnlyList<string> RetainedProtectedArtifactIds { get; init; } = [];
}

/// <summary>
/// Result of verifying one stored artifact's integrity (Sprint M1.5 D5).
/// </summary>
public sealed record ReleaseIntegrityReport
{
    public required string ArtifactId { get; init; }

    public bool IsIntact => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ReleaseIntegrityReport Success(string artifactId) => new() { ArtifactId = artifactId };

    public static ReleaseIntegrityReport Failure(string artifactId, IReadOnlyList<string> errors)
        => new() { ArtifactId = artifactId, Errors = errors };
}

/// <summary>
/// Input for registering a newly produced artifact (Sprint M1.5 D3). Every value
/// here must be sourced from an existing pipeline computation — see
/// <c>ReleaseLibraryService.RegisterPublishedAsync</c> callers.
/// </summary>
public sealed class ReleaseArtifactRegistration
{
    public required string ServiceId { get; init; }

    public required string ServiceDisplayName { get; init; }

    public required string Version { get; init; }

    public required string PackagePath { get; init; }

    public required string SourceRevision { get; init; }

    public required DateTimeOffset BuildUtc { get; init; }

    public required string PublishingFingerprint { get; init; }

    public required string ContractVersion { get; init; }

    public required string PublishingEngineVersion { get; init; }

    public string? SupportedRuntimeVersion { get; init; }

    public int FingerprintPolicyVersion { get; init; }

    public string ReleaseLibraryCategory { get; init; } = "Services";

    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public string? PlatformVersion { get; init; }

    public string? Solution { get; init; }
}

/// <summary>
/// Search &amp; query surface (Sprint M1.5 D9). The Runtime Agent and future
/// promotion/rollback/CI-CD consumers are expected to call these instead of
/// scanning the release-library filesystem directly.
/// </summary>
public interface IReleaseLibraryQueries
{
    ReleaseArtifactRecord? FindByArtifactId(string releaseLibraryRoot, string artifactId);

    ReleaseArtifactRecord? FindByVersion(string releaseLibraryRoot, string serviceId, string version);

    IReadOnlyList<ReleaseArtifactRecord> FindByService(string releaseLibraryRoot, string serviceId);

    IReadOnlyList<ReleaseArtifactRecord> FindByPlatformVersion(string releaseLibraryRoot, string platformVersion);

    ReleaseArtifactRecord? FindByFingerprint(string releaseLibraryRoot, string fingerprint);

    IReadOnlyList<ReleaseArtifactRecord> FindBySourceRevision(string releaseLibraryRoot, string sourceRevision);

    /// <summary>Highest version, most-recently-certified artifact for a service.</summary>
    ReleaseArtifactRecord? FindLatestCertified(string releaseLibraryRoot, string serviceId);

    /// <summary>Highest version artifact for a service, certified or not.</summary>
    ReleaseArtifactRecord? FindLatest(string releaseLibraryRoot, string serviceId);

    /// <summary>The newest solution-scoped set of artifacts sharing the highest platform version.</summary>
    IReadOnlyList<ReleaseArtifactRecord> FindLatestPlatformBundle(string releaseLibraryRoot);

    /// <summary>The most recent certified Runtime Agent artifact compatible with the given contract version.</summary>
    ReleaseArtifactRecord? FindCompatibleRuntimeAgent(string releaseLibraryRoot, string contractVersion);

    /// <summary>
    /// The best rollback candidate for a service: the newest Certified/Deployed
    /// artifact strictly older than <paramref name="currentVersion"/>.
    /// </summary>
    ReleaseArtifactRecord? FindRollbackCandidate(string releaseLibraryRoot, string serviceId, string currentVersion);
}
