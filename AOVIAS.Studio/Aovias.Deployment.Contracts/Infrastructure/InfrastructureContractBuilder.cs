using Aovias.Deployment.Contracts.Building;

namespace Aovias.Deployment.Contracts.Infrastructure;

/// <summary>
/// Builds a unified infrastructure contract from a service deployment bundle.
/// </summary>
public static class InfrastructureContractBuilder
{
    public static InfrastructureContract Build(ServiceDeploymentBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        return new InfrastructureContract
        {
            ServiceId = bundle.Manifest.ServiceId,
            ContractVersion = ServiceContractBuilder.ContractVersion,
            Filesystem = bundle.Filesystem,
            Certificates = bundle.Certificate,
            Secrets = bundle.Secret,
            Database = bundle.Database,
            Docker = BuildDocker(bundle),
            Network = bundle.Network,
            Storage = bundle.Volume,
            Providers = BuildProviders(bundle),
            Dependencies = bundle.Dependency,
            Health = bundle.Health
        };
    }

    private static DockerInfrastructureContract BuildDocker(ServiceDeploymentBundle bundle)
        => new()
        {
            RequireEngine = bundle.RuntimeCapabilities.Docker,
            RequireCompose = bundle.RuntimeCapabilities.Docker,
            ImageReference = bundle.Manifest.Docker.Image,
            RestartPolicy = bundle.Manifest.Deployment.RestartPolicy,
            CreateNetworkIfMissing = true,
            RequiredImages = string.IsNullOrWhiteSpace(bundle.Manifest.Docker.Image)
                ? []
                : [bundle.Manifest.Docker.Image]
        };

    private static IReadOnlyList<ExternalProviderContract> BuildProviders(ServiceDeploymentBundle bundle)
    {
        var providers = new List<ExternalProviderContract>();

        if (bundle.Feature.RequiresRabbitMq)
        {
            providers.Add(new ExternalProviderContract
            {
                Id = "rabbitmq",
                Provider = "RabbitMQ",
                Required = true,
                SecretName = "RabbitMQ:Password",
                ConfigurationKey = "RabbitMQ:Password"
            });
        }

        if (bundle.Feature.RequiresServiceBus)
        {
            var serviceBusSecret = bundle.Secret.Secrets.FirstOrDefault(item =>
                item.ConfigurationKey.Contains("ServiceBus", StringComparison.OrdinalIgnoreCase));
            providers.Add(new ExternalProviderContract
            {
                Id = "service-bus",
                Provider = "AzureServiceBus",
                Required = serviceBusSecret?.Required ?? true,
                SecretName = serviceBusSecret?.Name ?? "ConnectionStrings:ServiceBus",
                ConfigurationKey = serviceBusSecret?.ConfigurationKey ?? "ConnectionStrings:ServiceBus"
            });
        }

        if (string.Equals(bundle.Manifest.ServiceId, "ai", StringComparison.OrdinalIgnoreCase))
        {
            providers.AddRange(
            [
                new ExternalProviderContract
                {
                    Id = "azure-openai",
                    Provider = "AzureOpenAI",
                    Required = false,
                    SecretName = "AzureOpenAI:ApiKey",
                    ConfigurationKey = "AzureOpenAI:ApiKey"
                },
                new ExternalProviderContract
                {
                    Id = "sarvam",
                    Provider = "Sarvam",
                    Required = false,
                    SecretName = "Sarvam:ApiKey",
                    ConfigurationKey = "Sarvam:ApiKey"
                }
            ]);
        }

        if (bundle.Feature.RequiresNginx)
        {
            providers.Add(new ExternalProviderContract
            {
                Id = "nginx",
                Provider = "Nginx",
                Required = false,
                ConfigurationKey = "nginx"
            });
        }

        return providers;
    }
}
