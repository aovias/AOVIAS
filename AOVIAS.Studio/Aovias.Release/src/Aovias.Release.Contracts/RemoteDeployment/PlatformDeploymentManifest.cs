namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Top-level deployment manifest for a published solution (solution-deployment.json).
/// </summary>
public sealed class PlatformDeploymentManifest
{
    public required string Profile { get; init; }

    public required string Version { get; init; }

    public DateTimeOffset GeneratedUtc { get; init; }

    public IReadOnlyList<string> StartupOrder { get; init; } = [];

    public IReadOnlyList<PlatformDeploymentServiceEntry> Services { get; init; } = [];
}

/// <summary>
/// One deployable service entry inside <see cref="PlatformDeploymentManifest"/>.
/// </summary>
public sealed class PlatformDeploymentServiceEntry
{
    public required string ServiceId { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public int StartupOrder { get; init; }

    public IReadOnlyList<string> DependsOn { get; init; } = [];

    public int ContainerPort { get; init; }

    public required string HealthEndpoint { get; init; }

    public required string DockerImage { get; init; }

    public required string PackageFileName { get; init; }

    public string? PackagePath { get; init; }

    public bool DeploymentReady { get; init; }
}
