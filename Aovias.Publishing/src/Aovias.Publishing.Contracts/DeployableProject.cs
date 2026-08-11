namespace Aovias.Publishing.Contracts;

/// <summary>
/// Deployable application discovered from the platform solution.
/// </summary>
public sealed class DeployableProject
{
    public required string Name { get; init; }

    public required string ServiceId { get; init; }

    public required string ProjectPath { get; init; }

    public required string DiscoveryPath { get; init; }

    public required PublishingProjectType Type { get; init; }

    public string? Dockerfile { get; init; }

    public required string OutputPath { get; init; }

    public required string Version { get; init; }

    public int StartupOrder { get; init; }

    public int ContainerPort { get; init; } = 8080;

    public string HealthEndpoint { get; init; } = "/health";

    public IReadOnlyList<string> DependsOn { get; init; } = [];

    public string ReleaseLibraryCategory { get; init; } = "Services";

    public bool IncludeInDemoBundle { get; init; }

    public bool IncludeInSolutionDeployment { get; init; } = true;
}
