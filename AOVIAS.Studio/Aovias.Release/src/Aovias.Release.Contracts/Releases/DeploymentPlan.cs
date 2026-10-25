using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Identity;
using Aovias.Release.Contracts.Packages;

namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Deployment plan generated before any deployment execution.
/// </summary>
public sealed class DeploymentPlan
{
    public required string PlanId { get; init; }

    public required ReleaseIdentity Release { get; init; }

    public required PackageIdentity Package { get; init; }

    public required bool CompatibilityPassed { get; init; }

    public required IReadOnlyList<string> CompatibilityMessages { get; init; }

    public required bool DependenciesValidated { get; init; }

    public required IReadOnlyList<string> DependencyMessages { get; init; }

    public required IReadOnlyList<DeploymentAction> RequiredActions { get; init; }

    public required TimeSpan EstimatedDuration { get; init; }

    public required DeploymentRiskLevel RiskLevel { get; init; }

    public required bool RollbackAvailable { get; init; }

    public required IReadOnlyList<string> AffectedServices { get; init; }

    public required bool OperatorConfirmationRequired { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DeploymentInspectionDetails? InspectionDetails { get; init; }
}
