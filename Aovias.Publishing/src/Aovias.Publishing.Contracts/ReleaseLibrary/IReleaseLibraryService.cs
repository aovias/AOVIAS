namespace Aovias.Publishing.Contracts.ReleaseLibrary;

/// <summary>
/// The single authoritative Release Library implementation (Sprint M1.5). Replaces
/// the prior parallel copy implementations (categorized <c>ReleaseLibraryOrganizer</c>
/// and the CSM flat-copy <c>AddToReleaseLibraryAsync</c>) with one service that both
/// now delegate to. Every method that advances the lifecycle is named for, and
/// intended to be called only by, its owning subsystem — see the M1.5 report §4.
/// </summary>
public interface IReleaseLibraryService : IReleaseLibraryQueries
{
    /// <summary>
    /// Loads (or lazily creates) the index for <paramref name="releaseLibraryRoot"/>.
    /// All query methods on this instance operate against the most recently loaded
    /// index for that root; call this before querying a specific root.
    /// </summary>
    Task LoadAsync(string releaseLibraryRoot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a newly produced artifact and copies its package into the
    /// library's versioned storage. Owned by Publishing. Fails (D10) on a
    /// duplicate artifact ID with a different fingerprint, a duplicate
    /// fingerprint registered under a different service, or a corrupted index.
    /// </summary>
    Task<ReleaseArtifactRecord> RegisterPublishedAsync(
        string releaseLibraryRoot,
        ReleaseArtifactRegistration registration,
        CancellationToken cancellationToken = default);

    /// <summary>Owned by package verification. Building/Published → Verified.</summary>
    Task<ReleaseArtifactRecord> AdvanceToVerifiedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Owned by deployment certification. Published/Verified → Certified.</summary>
    Task<ReleaseArtifactRecord> AdvanceToCertifiedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Owned by the Release Library's own integrity/lifecycle gates. Certified → Ready.</summary>
    Task<ReleaseArtifactRecord> AdvanceToReadyAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Owned by the Runtime Agent / deployment execution (out of scope for M1.5; API only). Ready → Deployed.</summary>
    Task<ReleaseArtifactRecord> MarkDeployedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Owned by the promotion workflow (out of scope for M1.5; API only). Deployed → Promoted.</summary>
    Task<ReleaseArtifactRecord> MarkPromotedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Owned by whichever subsystem supersedes this artifact with a newer one.</summary>
    Task<ReleaseArtifactRecord> MarkDeprecatedAsync(string releaseLibraryRoot, string artifactId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Owned by the retention policy.</summary>
    Task<ReleaseArtifactRecord> ArchiveAsync(string releaseLibraryRoot, string artifactId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compatibility facade for the pre-M1.5 categorized copy
    /// (<c>ReleaseLibraryOrganizer</c>) and the CSM flat-copy
    /// (<c>AddToReleaseLibraryAsync</c>) call sites: registers the artifact (if not
    /// already registered) and returns the stored package path. Both call sites
    /// now share this single implementation instead of duplicating copy logic.
    /// </summary>
    Task<string> RegisterOrganizedCopyAsync(
        string releaseLibraryRoot,
        DeployableProject project,
        string packagePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Overload of <see cref="RegisterOrganizedCopyAsync(string, DeployableProject, string, CancellationToken)"/>
    /// for callers that only have a finished package on disk and no discovered
    /// <see cref="DeployableProject"/> (e.g. the CSM ad hoc "add to Release Library"
    /// workspace action). Service identity, display name, and category are derived
    /// entirely from the package's own manifest/metadata — no values are invented
    /// or duplicated from a separate discovery pass.
    /// </summary>
    Task<string> RegisterOrganizedCopyAsync(
        string releaseLibraryRoot,
        string packagePath,
        CancellationToken cancellationToken = default);

    /// <summary>Sprint M1.5 D5: verify one artifact's stored package against its recorded metadata.</summary>
    Task<ReleaseIntegrityReport> VerifyIntegrityAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default);

    /// <summary>Sprint M1.5 D5: verify every artifact currently in the index.</summary>
    Task<IReadOnlyList<ReleaseIntegrityReport>> VerifyAllIntegrityAsync(string releaseLibraryRoot, CancellationToken cancellationToken = default);

    /// <summary>Sprint M1.5 D7: evaluate (without mutating anything) what retention would do.</summary>
    ReleaseRetentionPlan EvaluateRetention(string releaseLibraryRoot, ReleaseRetentionPolicy policy);

    /// <summary>Sprint M1.5 D7: apply a previously evaluated retention plan (archive only; never deletes protected artifacts).</summary>
    Task ApplyRetentionAsync(string releaseLibraryRoot, ReleaseRetentionPlan plan, CancellationToken cancellationToken = default);

    /// <summary>Sprint M1.5 D8: services registered as belonging to the given solution/platform version.</summary>
    IReadOnlyList<ReleaseArtifactRecord> GetSolutionComposition(string releaseLibraryRoot, string solution, string platformVersion);
}
