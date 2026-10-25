namespace Aovias.Publishing.Contracts.ReleaseLibrary;

/// <summary>
/// Primary artifact lifecycle (Sprint M1.5 D2). Every artifact registered in the
/// Release Library occupies exactly one of these states at a time. Transitions are
/// forward-only and validated by <c>ArtifactLifecycleTransitions</c>; see the M1.5
/// implementation report for the ownership of each transition.
/// </summary>
public enum ArtifactLifecycleState
{
    /// <summary>The publishing pipeline is producing this artifact; not yet registered.</summary>
    Building = 0,

    /// <summary>Publishing produced a complete, checksummed package. Owned by Publishing.</summary>
    Published = 1,

    /// <summary>Package verification (required files, checksums, contracts) passed.</summary>
    Verified = 2,

    /// <summary>Deployment certification produced a deployment-readiness certificate.</summary>
    Certified = 3,

    /// <summary>Certified and passes every Release Library integrity/lifecycle gate; deployable.</summary>
    Ready = 4,

    /// <summary>A Runtime Agent deployment of this artifact succeeded. Owned by Deployment (out of scope for M1.5).</summary>
    Deployed = 5,

    /// <summary>Promoted to a higher environment tier. Owned by the promotion workflow (out of scope for M1.5).</summary>
    Promoted = 6,

    /// <summary>Superseded by a newer artifact; retained for rollback/audit only.</summary>
    Deprecated = 7,

    /// <summary>Retained for historical/audit purposes only; excluded from deployable queries.</summary>
    Archived = 8
}
