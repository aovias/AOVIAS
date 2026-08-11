namespace Aovias.Deployment.Contracts.Building;

/// <summary>
/// Builds canonical deployment contracts for a service package.
/// </summary>
public static class ServiceContractBuilder
{
    public const string ContractVersion = "1.0";

    public static ServiceDeploymentBundle Build(ServiceContractBuildRequest request)
    {
        var serviceId = request.ServiceId.ToLowerInvariant();
        var template = ServiceContractRegistry.GetTemplate(serviceId, request);
        var manifest = CreateManifest(request, template);
        var deployment = new DeploymentContract
        {
            ServiceId = serviceId,
            Version = request.Version,
            ContractVersion = ContractVersion,
            Files = new ServiceContractReferences()
        };

        return new ServiceDeploymentBundle
        {
            Manifest = manifest,
            Deployment = deployment,
            Filesystem = template.Filesystem,
            Certificate = template.Certificate,
            Database = template.Database,
            Secret = MergeSecrets(template.Secret, request.ConfigurationKeys),
            Configuration = MergeConfiguration(template.Configuration, request.ConfigurationKeys),
            Dependency = template.Dependency,
            Health = CreateHealth(request, template.Health),
            Network = template.Network,
            Volume = template.Volume,
            Feature = template.Feature,
            RuntimeCapabilities = template.RuntimeCapabilities,
            IsLegacySynthesized = false
        };
    }

    private static ServiceManifest CreateManifest(ServiceContractBuildRequest request, ServiceContractTemplate template)
    {
        var serviceId = request.ServiceId.ToLowerInvariant();
        return new ServiceManifest
        {
            ServiceId = serviceId,
            DisplayName = request.DisplayName,
            Version = request.Version,
            Docker = new ServiceDockerSpecification
            {
                Image = request.ImageReference,
                ContainerUser = PlatformRuntimeDefaults.ContainerUserId,
                HealthEndpoint = request.HealthEndpoint,
                Ports = request.Ports,
                EntryPoint = request.EntryPoint,
                ContainerName = request.ContainerName
            },
            Deployment = new ServiceDeploymentSpecification
            {
                StartupTimeoutSeconds = template.Health.StartupTimeoutSeconds,
                ShutdownTimeoutSeconds = template.Health.ShutdownTimeoutSeconds,
                RestartPolicy = "unless-stopped"
            },
            Contracts = new ServiceContractReferences()
        };
    }

    private static HealthContract CreateHealth(ServiceContractBuildRequest request, HealthContract template)
        => template with
        {
            HealthUrl = request.HealthEndpoint,
            ExpectedStatus = request.ExpectedHealthStatus,
            TimeoutSeconds = request.HealthTimeoutSeconds > 0 ? request.HealthTimeoutSeconds : template.TimeoutSeconds,
            Retries = request.HealthRetries > 0 ? request.HealthRetries : template.Retries,
            StabilizationSeconds = request.HealthStabilizationSeconds > 0
                ? request.HealthStabilizationSeconds
                : template.StabilizationSeconds
        };

    private static SecretContract MergeSecrets(SecretContract template, IReadOnlyCollection<string> configurationKeys)
    {
        var secrets = template.Secrets.ToList();
        foreach (var key in configurationKeys)
        {
            if (!IsSecretKey(key))
            {
                continue;
            }

            if (secrets.Any(secret => secret.ConfigurationKey.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            secrets.Add(new SecretRequirementContract
            {
                Id = ToSecretId(key),
                Name = key,
                Required = true,
                ConfigurationKey = key
            });
        }

        return new SecretContract { Secrets = secrets };
    }

    private static ConfigurationContract MergeConfiguration(
        ConfigurationContract template,
        IReadOnlyCollection<string> configurationKeys)
    {
        var required = template.Required.ToDictionary(item => item.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var key in configurationKeys)
        {
            if (IsSecretKey(key))
            {
                continue;
            }

            required.TryAdd(key, new ConfigurationRequirementContract
            {
                Key = key,
                ValidationRule = "non-empty"
            });
        }

        return new ConfigurationContract
        {
            Required = required.Values.ToList(),
            Optional = template.Optional
        };
    }

    private static bool IsSecretKey(string key)
    {
        var normalized = key.Replace(":", string.Empty, StringComparison.Ordinal);
        return normalized.Contains("password", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("secret", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("apikey", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("connectionstring", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("token", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToSecretId(string key)
        => key.Replace(':', '-').Replace('.', '-').ToLowerInvariant();
}

public sealed class ServiceContractBuildRequest
{
    public required string ServiceId { get; init; }

    public required string DisplayName { get; init; }

    public required string Version { get; init; }

    public required string ImageReference { get; init; }

    public required string ContainerName { get; init; }

    public required string EntryPoint { get; init; }

    public required string HealthEndpoint { get; init; }

    public IReadOnlyList<int> Ports { get; init; } = [];

    public int ExpectedHealthStatus { get; init; } = 200;

    public int HealthTimeoutSeconds { get; init; }

    public int HealthRetries { get; init; }

    public int HealthStabilizationSeconds { get; init; }

    public IReadOnlyList<string> Dependencies { get; init; } = [];

    public IReadOnlyCollection<string> ConfigurationKeys { get; init; } = Array.Empty<string>();
}
