namespace Aovias.Release.Contracts.Packages;

/// <summary>
/// service.json content describing the deployable service.
/// </summary>
public sealed class ServiceDescription
{
    public required string ServiceId { get; init; }

    public required string DisplayName { get; init; }

    public required string ContainerName { get; init; }

    public required string ComposeFile { get; init; }

    public required string HealthEndpoint { get; init; }

    public int StartupOrder { get; init; }

    public required string Category { get; init; }

    public required string Description { get; init; }
}
