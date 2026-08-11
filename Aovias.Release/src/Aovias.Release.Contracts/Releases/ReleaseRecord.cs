using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Identity;
using Aovias.Release.Contracts.Packages;

namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Persisted release record tracked by the release manager.
/// </summary>
public sealed record ReleaseRecord
{
    public required ReleaseIdentity Identity { get; init; }

    public required ReleaseState State { get; init; }

    public required string PackagePath { get; init; }

    public required PackageManifest Manifest { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public string? FailureReason { get; init; }

    public DeploymentPlan? LastPlan { get; init; }
}
