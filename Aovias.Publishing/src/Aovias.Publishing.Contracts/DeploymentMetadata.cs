namespace Aovias.Publishing.Contracts;

/// <summary>
/// Declarative deployment metadata for a deployable service. This is the
/// publishing pipeline's single source of truth for ports, health, dependencies,
/// and solution membership.
/// </summary>
public sealed class DeploymentMetadata
{
    public int SchemaVersion { get; init; } = 1;

    public required string ServiceId { get; init; }

    public string? DisplayName { get; init; }

    public string ReleaseLibraryCategory { get; init; } = "Services";

    public bool IncludeInDemoBundle { get; init; }

    public bool IncludeInSolutionDeployment { get; init; } = true;

    public required DeploymentPortMetadata Ports { get; init; }

    public required DeploymentHealthMetadata Health { get; init; }

    public IReadOnlyList<DeploymentDependencyMetadata> Dependencies { get; init; } = [];

    public IReadOnlyList<string> GetRequiredServiceDependencies()
        => Dependencies
            .Where(dependency => dependency.Required
                                 && string.Equals(dependency.Type, "service", StringComparison.OrdinalIgnoreCase))
            .Select(dependency => dependency.Service)
            .ToList();
}

public sealed class DeploymentPortMetadata
{
    public int ContainerPort { get; init; }

    public int? HostPort { get; init; }

    public int ResolvedHostPort => HostPort ?? ContainerPort;
}

public sealed class DeploymentHealthMetadata
{
    public string Endpoint { get; init; } = "/health";

    public int ExpectedStatus { get; init; } = 200;

    public int TimeoutSeconds { get; init; } = 15;

    public int Retries { get; init; } = 3;

    public bool RequiresHttpProbe { get; init; } = true;
}

public sealed class DeploymentDependencyMetadata
{
    public required string Service { get; init; }

    public bool Required { get; init; } = true;

    public string Type { get; init; } = "service";
}
