using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Discovery;

namespace Aovias.Publishing.Core.Generation;

/// <summary>
/// Generates canonical deployment contracts for a publish run.
/// </summary>
public static class ServiceContractGenerator
{
    public static ServiceDeploymentBundle Generate(PublishingContext context)
    {
        var discovery = context.Discovery ?? throw new InvalidOperationException("Discovery is required.");
        var manifest = context.Manifest ?? throw new InvalidOperationException("Manifest is required.");
        var health = context.Health ?? throw new InvalidOperationException("Health definition is required.");
        var metadata = context.DeploymentMetadata
                       ?? throw new InvalidOperationException("Deployment metadata is required.");
        var serviceId = metadata.ServiceId;
        var version = context.Request.Version ?? manifest.Version ?? PublishingVersionPolicy.DefaultPackageVersion;
        var imageName = context.DockerImageName ?? manifest.ImageName;
        var imageTag = context.DockerImageTag ?? manifest.ImageTag;

        var request = new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = metadata.DisplayName ?? DeployableProjectNaming.ToDisplayName(discovery, serviceId),
            Version = version,
            ImageReference = $"{imageName}:{imageTag}",
            ContainerName = manifest.Container,
            EntryPoint = manifest.EntryPoint,
            HealthEndpoint = health.Endpoint,
            Ports = manifest.Ports,
            ExpectedHealthStatus = health.ExpectedStatus,
            HealthTimeoutSeconds = health.TimeoutSeconds,
            HealthRetries = health.Retries,
            HealthStabilizationSeconds = health.TimeoutSeconds,
            Dependencies = metadata.GetRequiredServiceDependencies(),
            ConfigurationKeys = context.ConfigurationTemplate.Keys.ToList()
        };

        return ServiceContractBuilder.Build(request);
    }
}
