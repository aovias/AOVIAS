namespace Aovias.Deployment.Contracts.Validation;

/// <summary>
/// Validates a <see cref="ServiceDeploymentBundle"/> and produces structured results for DRV and dashboards.
/// </summary>
public static class ContractValidator
{
    public static ContractValidationReport Validate(ServiceDeploymentBundle bundle)
    {
        var results = new List<ContractValidationItemResult>
        {
            ValidateManifest(bundle),
            ValidateFilesystem(bundle.Filesystem),
            ValidateCertificates(bundle.Certificate),
            ValidateDatabase(bundle.Database),
            ValidateSecrets(bundle.Secret),
            ValidateConfiguration(bundle.Configuration),
            ValidateDependencies(bundle.Dependency),
            ValidateHealth(bundle.Health, bundle.Feature),
            ValidateVolumes(bundle.Volume, bundle.Filesystem),
            ValidateCapabilities(bundle.RuntimeCapabilities, bundle.Feature)
        };

        return new ContractValidationReport
        {
            ServiceId = bundle.Manifest.ServiceId,
            Version = bundle.Manifest.Version,
            IsLegacySynthesized = bundle.IsLegacySynthesized,
            Results = results,
            Passed = results.All(result => result.Passed)
        };
    }

    private static ContractValidationItemResult ValidateManifest(ServiceDeploymentBundle bundle)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(bundle.Manifest.ServiceId))
        {
            errors.Add("serviceId is required.");
        }

        if (bundle.Manifest.Docker.Ports.Count == 0 && bundle.Feature.HttpService)
        {
            errors.Add("At least one container port is required for HTTP services.");
        }

        if (string.IsNullOrWhiteSpace(bundle.Manifest.Docker.Image))
        {
            errors.Add("docker.image is required.");
        }

        return Item("Manifest", errors);
    }

    private static ContractValidationItemResult ValidateFilesystem(FilesystemContract contract)
    {
        var errors = new List<string>();
        if (contract.Directories.Count == 0 && contract.ReadOnlyDirectories.Count == 0)
        {
            errors.Add("At least one filesystem directory must be declared.");
        }

        foreach (var directory in contract.Directories.Concat(contract.ReadOnlyDirectories))
        {
            if (!directory.Path.StartsWith("/", StringComparison.Ordinal))
            {
                errors.Add($"Directory '{directory.Path}' must be an absolute Linux path.");
            }
        }

        return Item("Filesystem", errors);
    }

    private static ContractValidationItemResult ValidateCertificates(CertificateContract contract)
    {
        var errors = new List<string>();
        foreach (var certificate in contract.Certificates.Where(item => item.Required))
        {
            if (string.IsNullOrWhiteSpace(certificate.MountPath))
            {
                errors.Add($"Certificate '{certificate.Id}' requires mountPath.");
            }

            if (string.IsNullOrWhiteSpace(certificate.SecretKey))
            {
                errors.Add($"Certificate '{certificate.Id}' requires secretKey.");
            }
        }

        return Item("Certificates", errors);
    }

    private static ContractValidationItemResult ValidateDatabase(DatabaseContract contract)
    {
        var errors = new List<string>();
        if (contract.Required && string.Equals(contract.Provider, "None", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Database provider cannot be 'None' when database is required.");
        }

        if (contract.Required && string.IsNullOrWhiteSpace(contract.ConnectionSetting))
        {
            errors.Add("Database connectionSetting is required.");
        }

        return Item("Database", errors);
    }

    private static ContractValidationItemResult ValidateSecrets(SecretContract contract)
    {
        var errors = new List<string>();
        foreach (var secret in contract.Secrets.Where(item => item.Required))
        {
            if (string.IsNullOrWhiteSpace(secret.ConfigurationKey))
            {
                errors.Add($"Secret '{secret.Id}' requires configurationKey.");
            }
        }

        return Item("Secrets", errors);
    }

    private static ContractValidationItemResult ValidateConfiguration(ConfigurationContract contract)
    {
        var errors = new List<string>();
        foreach (var item in contract.Required)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                errors.Add("Configuration required keys cannot be empty.");
            }
        }

        return Item("Configuration", errors);
    }

    private static ContractValidationItemResult ValidateDependencies(DependencyContract contract)
    {
        var errors = new List<string>();
        foreach (var dependency in contract.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency.Service))
            {
                errors.Add("Dependency service name cannot be empty.");
            }
        }

        return Item("Dependencies", errors);
    }

    private static ContractValidationItemResult ValidateHealth(HealthContract contract, FeatureContract feature)
    {
        var errors = new List<string>();
        if (feature.HttpService && contract.RequiresHttpProbe && string.IsNullOrWhiteSpace(contract.HealthUrl))
        {
            errors.Add("healthUrl is required for HTTP services.");
        }

        if (contract.TimeoutSeconds <= 0 || contract.Retries <= 0)
        {
            errors.Add("Health timeout and retries must be greater than zero.");
        }

        return Item("Health", errors);
    }

    private static ContractValidationItemResult ValidateVolumes(VolumeContract volume, FilesystemContract filesystem)
    {
        var errors = new List<string>();
        var seenContainerPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var mount in volume.Mounts)
        {
            if (string.IsNullOrWhiteSpace(mount.HostToken) || string.IsNullOrWhiteSpace(mount.ContainerPath))
            {
                errors.Add("Volume mounts require hostToken and containerPath.");
                continue;
            }

            if (!mount.ContainerPath.StartsWith("/", StringComparison.Ordinal))
            {
                errors.Add($"Volume mount containerPath '{mount.ContainerPath}' must be an absolute Linux path.");
            }

            if (!seenContainerPaths.Add(mount.ContainerPath))
            {
                errors.Add($"Duplicate volume mount declared for container path '{mount.ContainerPath}'.");
            }
        }

        if (volume.Mounts.Count == 0 && filesystem.Directories.Any(dir => dir.Path.StartsWith("/opt/aovias/runtime", StringComparison.Ordinal)))
        {
            errors.Add("Runtime filesystem directories require corresponding volume mounts.");
        }

        foreach (var directory in filesystem.Directories)
        {
            if (!IsCoveredByWritableMount(directory.Path, volume.Mounts))
            {
                errors.Add($"Filesystem directory '{directory.Path}' is not covered by a writable volume mount.");
            }
        }

        foreach (var directory in filesystem.ReadOnlyDirectories)
        {
            if (!IsCoveredByReadOnlyMount(directory.Path, volume.Mounts))
            {
                errors.Add($"Read-only filesystem directory '{directory.Path}' is not covered by a read-only volume mount.");
            }
        }

        return Item("Volumes", errors);
    }

    private static bool IsCoveredByWritableMount(string containerPath, IReadOnlyList<VolumeMountContract> mounts)
        => mounts.Any(mount => !mount.ReadOnly && IsSameOrChildPath(containerPath, mount.ContainerPath));

    private static bool IsCoveredByReadOnlyMount(string containerPath, IReadOnlyList<VolumeMountContract> mounts)
        => mounts.Any(mount => mount.ReadOnly && IsSameOrChildPath(containerPath, mount.ContainerPath));

    private static bool IsSameOrChildPath(string path, string mountRoot)
    {
        if (string.Equals(path, mountRoot, StringComparison.Ordinal))
        {
            return true;
        }

        return path.StartsWith(mountRoot.TrimEnd('/') + "/", StringComparison.Ordinal);
    }

    private static ContractValidationItemResult ValidateCapabilities(RuntimeCapabilities capabilities, FeatureContract feature)
    {
        var errors = new List<string>();
        if (feature.RequiresSqlServer && !capabilities.SqlServer)
        {
            errors.Add("Runtime capabilities must declare sqlserver when the service requires SQL Server.");
        }

        if (feature.RequiresRabbitMq && !capabilities.RabbitMq)
        {
            errors.Add("Runtime capabilities must declare rabbitmq when the service requires RabbitMQ.");
        }

        if (feature.RequiresNginx && !capabilities.Nginx)
        {
            errors.Add("Runtime capabilities must declare nginx when the service requires nginx.");
        }

        return Item("Capabilities", errors);
    }

    private static ContractValidationItemResult Item(string name, IReadOnlyList<string> errors)
        => new()
        {
            Name = name,
            Passed = errors.Count == 0,
            Errors = errors
        };
}

public sealed class ContractValidationReport
{
    public required string ServiceId { get; init; }

    public required string Version { get; init; }

    public bool IsLegacySynthesized { get; init; }

    public required IReadOnlyList<ContractValidationItemResult> Results { get; init; }

    public bool Passed { get; init; }
}

public sealed class ContractValidationItemResult
{
    public required string Name { get; init; }

    public bool Passed { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}
