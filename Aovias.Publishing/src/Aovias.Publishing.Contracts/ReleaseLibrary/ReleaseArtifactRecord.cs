namespace Aovias.Publishing.Contracts.ReleaseLibrary;

/// <summary>
/// A single lifecycle-state transition, recorded for traceability (Sprint M1.5 D2).
/// </summary>
public sealed record ArtifactLifecycleTransition(
    ArtifactLifecycleState FromState,
    ArtifactLifecycleState ToState,
    DateTimeOffset TransitionUtc,
    string Owner,
    string? Reason = null);

/// <summary>
/// Authoritative release metadata for one artifact (Sprint M1.5 D3). Every field is
/// sourced from an existing publishing computation (build identity, package
/// verification, deployment certificate) rather than recomputed here — see the
/// M1.5 implementation report §5 for the field-by-field provenance.
/// </summary>
public sealed record ReleaseArtifactRecord
{
    /// <summary>Stable natural key: <c>{ServiceId}-{Version}</c>, lowercase.</summary>
    public required string ArtifactId { get; init; }

    public required string ServiceId { get; init; }

    public required string Service { get; init; }

    public required string Version { get; init; }

    /// <summary>Composite build identity: <c>{SourceRevision}@{BuildUtc:O}</c>.</summary>
    public required string BuildIdentity { get; init; }

    public required string SourceRevision { get; init; }

    /// <summary>The M1.2 deterministic package fingerprint (content-based, environment-neutral).</summary>
    public required string PublishingFingerprint { get; init; }

    /// <summary>Raw SHA-256 of the stored <c>.aovdeploy</c> file bytes (tamper/corruption detection).</summary>
    public required string PackageHash { get; init; }

    public required long PackageSizeBytes { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    /// <summary>Logical solution/platform this artifact belongs to (single-solution today).</summary>
    public string Solution { get; init; } = "AOVIAS-Platform";

    /// <summary>Version of the solution bundle this artifact was published as part of, if any.</summary>
    public string? PlatformVersion { get; init; }

    public required string ContractVersion { get; init; }

    public required string PublishingEngineVersion { get; init; }

    public required string SupportedRuntimeVersion { get; init; }

    /// <summary>Derived compatibility tag: contract/engine/fingerprint-policy versions joined.</summary>
    public required string DeploymentCompatibility { get; init; }

    public string ReleaseLibraryCategory { get; init; } = "Services";

    /// <summary>Path to the stored package, relative to the release library root.</summary>
    public required string RelativePackagePath { get; init; }

    /// <summary>Service IDs this artifact depends on (from the dependency contract).</summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public ArtifactLifecycleState LifecycleState { get; init; } = ArtifactLifecycleState.Building;

    public ArtifactPromotionStatus PromotionStatus { get; init; } = ArtifactPromotionStatus.Draft;

    public bool IsCertified { get; init; }

    public DateTimeOffset? CertifiedUtc { get; init; }

    /// <summary>
    /// Never eligible for retention deletion (deployed, certified, or an explicit
    /// rollback candidate). Archival (state transition) is still permitted.
    /// </summary>
    public bool IsProtected { get; init; }

    public IReadOnlyList<ArtifactLifecycleTransition> Transitions { get; init; } = [];

    public ReleaseArtifactRecord WithTransition(ArtifactLifecycleState toState, string owner, string? reason = null)
        => this with
        {
            LifecycleState = toState,
            Transitions = [.. Transitions, new ArtifactLifecycleTransition(LifecycleState, toState, DateTimeOffset.UtcNow, owner, reason)]
        };
}
