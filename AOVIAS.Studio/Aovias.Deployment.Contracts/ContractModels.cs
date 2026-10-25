namespace Aovias.Deployment.Contracts;

public sealed class ServiceDeploymentBundle
{
    public required ServiceManifest Manifest { get; init; }

    public required DeploymentContract Deployment { get; init; }

    public required FilesystemContract Filesystem { get; init; }

    public required CertificateContract Certificate { get; init; }

    public required DatabaseContract Database { get; init; }

    public required SecretContract Secret { get; init; }

    public required ConfigurationContract Configuration { get; init; }

    public required DependencyContract Dependency { get; init; }

    public required HealthContract Health { get; init; }

    public required NetworkContract Network { get; init; }

    public required VolumeContract Volume { get; init; }

    public required FeatureContract Feature { get; init; }

    public required RuntimeCapabilities RuntimeCapabilities { get; init; }

    public bool IsLegacySynthesized { get; init; }
}

public sealed class FilesystemContract
{
    public int RequiredUser { get; init; } = PlatformRuntimeDefaults.ContainerUserId;

    public int RequiredGroup { get; init; } = PlatformRuntimeDefaults.ContainerUserId;

    public IReadOnlyList<FilesystemDirectoryContract> Directories { get; init; } = [];

    public IReadOnlyList<FilesystemDirectoryContract> ReadOnlyDirectories { get; init; } = [];
}

public sealed class FilesystemDirectoryContract
{
    public required string Path { get; init; }

    public string Permission { get; init; } = "775";

    public string Owner { get; init; } = PlatformRuntimeDefaults.ContainerUserId.ToString();

    public string Purpose { get; init; } = string.Empty;
}

public sealed class CertificateContract
{
    public IReadOnlyList<CertificateRequirementContract> Certificates { get; init; } = [];
}

public sealed class CertificateRequirementContract
{
    public required string Id { get; init; }

    public bool Required { get; init; }

    public string Type { get; init; } = "PFX";

    public string Category { get; init; } = "Signing";

    public string MountPath { get; init; } = PlatformRuntimeDefaults.CertificatesRoot;

    public string FileName { get; init; } = "identity-signing.pfx";

    public string SecretKey { get; init; } = "certificatePassword";

    public bool SupportsRotation { get; init; }

    public bool Optional { get; init; }
}

public sealed class DatabaseContract
{
    public string Provider { get; init; } = "None";

    public bool Required { get; init; }

    public string ConnectionSetting { get; init; } = "ConnectionStrings:DefaultConnection";

    public string Migration { get; init; } = "None";

    public string Seed { get; init; } = "None";

    public bool HealthCheck { get; init; }

    public string StartupPolicy { get; init; } = "Required";

    public string RollbackPolicy { get; init; } = "Manual";
}

public sealed class SecretContract
{
    public IReadOnlyList<SecretRequirementContract> Secrets { get; init; } = [];
}

public sealed class SecretRequirementContract
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public bool Required { get; init; }

    public string Source { get; init; } = "secrets/settings.secrets.json";

    public string ConfigurationKey { get; init; } = string.Empty;
}

public sealed class ConfigurationContract
{
    public IReadOnlyList<ConfigurationRequirementContract> Required { get; init; } = [];

    public IReadOnlyList<ConfigurationRequirementContract> Optional { get; init; } = [];
}

public sealed class ConfigurationRequirementContract
{
    public required string Key { get; init; }

    public string DefaultValue { get; init; } = string.Empty;

    public string ValidationRule { get; init; } = "non-empty";
}

public sealed class DependencyContract
{
    public IReadOnlyList<DependencyRequirementContract> Dependencies { get; init; } = [];
}

public sealed class DependencyRequirementContract
{
    public required string Service { get; init; }

    public bool Required { get; init; } = true;

    public string Type { get; init; } = "service";
}

public sealed record HealthContract
{
    public required string HealthUrl { get; init; }

    public string ReadinessUrl { get; init; } = "/health/ready";

    public string LivenessUrl { get; init; } = "/health";

    public int ExpectedStatus { get; init; } = 200;

    public int TimeoutSeconds { get; init; } = 15;

    public int Retries { get; init; } = 3;

    public int StabilizationSeconds { get; init; } = 15;

    public int StartupTimeoutSeconds { get; init; } = 120;

    public int ShutdownTimeoutSeconds { get; init; } = 30;

    public bool RequiresHttpProbe { get; init; } = true;

    public IReadOnlyList<HealthProbeEndpointContract> AdditionalProbes { get; init; } = [];
}

public sealed class HealthProbeEndpointContract
{
    public required string Method { get; init; }

    public required string Path { get; init; }

    public int ExpectedStatus { get; init; } = 200;

    public bool Required { get; init; }
}

public sealed class NetworkContract
{
    public string NetworkNameToken { get; init; } = "${AOVIAS_DOCKER_NETWORK}";

    public bool ExternalNetwork { get; init; } = true;
}

public sealed class VolumeContract
{
    public IReadOnlyList<VolumeMountContract> Mounts { get; init; } = [];
}

public sealed class VolumeMountContract
{
    public required string HostToken { get; init; }

    public required string ContainerPath { get; init; }

    public bool ReadOnly { get; init; }
}

public sealed class FeatureContract
{
    public bool HttpService { get; init; } = true;

    public bool WorkerService { get; init; }

    public bool RequiresNginx { get; init; }

    public bool RequiresSignalR { get; init; }

    public bool RequiresIdentity { get; init; }

    public bool RequiresSqlServer { get; init; }

    public bool RequiresRabbitMq { get; init; }

    public bool RequiresServiceBus { get; init; }

    public bool RequiresWritableEvidence { get; init; }

    public bool RequiresBootstrapDiagnostics { get; init; }
}

public sealed class RuntimeCapabilities
{
    public bool Docker { get; init; } = true;

    public bool SqlServer { get; init; }

    public bool Redis { get; init; }

    public bool RabbitMq { get; init; }

    public bool ServiceBus { get; init; }

    public bool Certificates { get; init; }

    public bool SignalR { get; init; }

    public bool Nginx { get; init; }
}
