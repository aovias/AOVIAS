using Aovias.Release.Contracts.Enums;

namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Pre-deployment inspection details surfaced in Release Center.
/// </summary>
public sealed class DeploymentInspectionDetails
{
    public required string ServiceId { get; init; }

    public required string PackageVersion { get; init; }

    public long PackageSizeBytes { get; init; }

    public string? DockerImageName { get; init; }

    public string? DockerImageTag { get; init; }

    public IReadOnlyList<string> ComposeServices { get; init; } = [];

    public required string HealthEndpoint { get; init; }

    public IReadOnlyList<string> ConfigTemplateKeys { get; init; } = [];

    public IReadOnlyList<string> Ports { get; init; } = [];

    public IReadOnlyList<string> Volumes { get; init; } = [];

    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public TimeSpan EstimatedDuration { get; init; }

    public DeploymentRiskLevel RiskLevel { get; init; }

    public bool RollbackAvailable { get; init; }
}
