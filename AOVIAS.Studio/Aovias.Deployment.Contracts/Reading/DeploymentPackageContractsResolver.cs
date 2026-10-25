namespace Aovias.Deployment.Contracts.Reading;

using Aovias.Deployment.Contracts.Building;

/// <summary>
/// Resolves deployment contracts from a package directory, synthesizing legacy contracts when needed.
/// </summary>
public static class DeploymentPackageContractsResolver
{
    public static ServiceDeploymentBundle ResolveFromDirectory(
        string packageDirectory,
        LegacyPackageMetadata? legacy = null)
    {
        var existing = ServiceDeploymentBundleIO.TryReadDirectory(packageDirectory);
        if (existing is not null)
        {
            return existing;
        }

        if (legacy is null)
        {
            throw new InvalidOperationException("Deployment contracts are missing and no legacy metadata was supplied.");
        }

        return LegacyContractCompatibilityBuilder.FromLegacy(
            legacy.ServiceId,
            legacy.DisplayName,
            legacy.Version,
            legacy.ImageName,
            legacy.ImageTag,
            legacy.ContainerName,
            legacy.EntryPoint,
            legacy.HealthEndpoint,
            legacy.Ports,
            legacy.Dependencies,
            legacy.HealthTimeoutSeconds,
            legacy.HealthRetries,
            legacy.ConfigurationKeys);
    }
}

public sealed class LegacyPackageMetadata
{
    public required string ServiceId { get; init; }

    public required string DisplayName { get; init; }

    public required string Version { get; init; }

    public required string ImageName { get; init; }

    public required string ImageTag { get; init; }

    public required string ContainerName { get; init; }

    public required string EntryPoint { get; init; }

    public required string HealthEndpoint { get; init; }

    public IReadOnlyList<int> Ports { get; init; } = [];

    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public int HealthTimeoutSeconds { get; init; } = 15;

    public int HealthRetries { get; init; } = 3;

    public IReadOnlyCollection<string> ConfigurationKeys { get; init; } = Array.Empty<string>();
}
