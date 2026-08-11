namespace Aovias.Publishing.Contracts.ReleaseLibrary;

/// <summary>
/// Secondary promotion ladder (Sprint M1.5 D6). Distinct from
/// <see cref="ArtifactLifecycleState"/>: the lifecycle state tracks the artifact's
/// physical existence and deployment history, while promotion status tracks its
/// environment-approval standing. This sprint establishes the states only; the
/// approval workflow that drives transitions between them is out of scope.
/// </summary>
public enum ArtifactPromotionStatus
{
    /// <summary>Newly published; not yet reviewed.</summary>
    Draft = 0,

    /// <summary>Passed package/contract verification.</summary>
    Verified = 1,

    /// <summary>Passed deployment certification (DRV + readiness certificate).</summary>
    Certified = 2,

    /// <summary>Explicitly approved for production use (future workflow; not automated by M1.5).</summary>
    ProductionApproved = 3,

    /// <summary>No longer recommended for new deployments.</summary>
    Deprecated = 4
}
