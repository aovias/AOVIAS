namespace Aovias.Deployment.Contracts.Building;

/// <summary>
/// Synthesizes deployment contracts from legacy package metadata when contract files are absent.
/// </summary>
public static class LegacyContractCompatibilityBuilder
{
    public static ServiceDeploymentBundle FromLegacy(
        string serviceId,
        string displayName,
        string version,
        string imageName,
        string imageTag,
        string containerName,
        string entryPoint,
        string healthEndpoint,
        IReadOnlyList<int> ports,
        IReadOnlyList<string> dependencies,
        int healthTimeoutSeconds,
        int healthRetries,
        IReadOnlyCollection<string> configurationKeys)
    {
        var request = new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = displayName,
            Version = version,
            ImageReference = $"{imageName}:{imageTag}",
            ContainerName = containerName,
            EntryPoint = entryPoint,
            HealthEndpoint = healthEndpoint,
            Ports = ports,
            Dependencies = dependencies,
            HealthTimeoutSeconds = healthTimeoutSeconds,
            HealthRetries = healthRetries,
            HealthStabilizationSeconds = healthTimeoutSeconds,
            ConfigurationKeys = configurationKeys
        };

        var bundle = ServiceContractBuilder.Build(request);
        return new ServiceDeploymentBundle
        {
            Manifest = bundle.Manifest,
            Deployment = bundle.Deployment,
            Filesystem = bundle.Filesystem,
            Certificate = bundle.Certificate,
            Database = bundle.Database,
            Secret = bundle.Secret,
            Configuration = bundle.Configuration,
            Dependency = bundle.Dependency,
            Health = bundle.Health,
            Network = bundle.Network,
            Volume = bundle.Volume,
            Feature = bundle.Feature,
            RuntimeCapabilities = bundle.RuntimeCapabilities,
            IsLegacySynthesized = true
        };
    }
}
