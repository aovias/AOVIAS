namespace Aovias.Deployment.Contracts;

/// <summary>
/// Canonical deployment specification for a service package.
/// </summary>
public sealed class ServiceManifest
{
    public required string ServiceId { get; init; }

    public required string DisplayName { get; init; }

    public required string Version { get; init; }

    public required ServiceDockerSpecification Docker { get; init; }

    public required ServiceDeploymentSpecification Deployment { get; init; }

    public required ServiceContractReferences Contracts { get; init; }
}

public sealed class ServiceDockerSpecification
{
    public required string Image { get; init; }

    public int ContainerUser { get; init; } = PlatformRuntimeDefaults.ContainerUserId;

    public required string HealthEndpoint { get; init; }

    public IReadOnlyList<int> Ports { get; init; } = [];

    public string? EntryPoint { get; init; }

    public string ContainerName { get; init; } = string.Empty;
}

public sealed class ServiceDeploymentSpecification
{
    public int StartupTimeoutSeconds { get; init; } = 120;

    public int ShutdownTimeoutSeconds { get; init; } = 30;

    public string RestartPolicy { get; init; } = "unless-stopped";
}

public sealed class ServiceContractReferences
{
    public string Deployment { get; init; } = DeploymentPackageLayout.DeploymentContractFile;

    public string Filesystem { get; init; } = DeploymentPackageLayout.FilesystemContractFile;

    public string Certificate { get; init; } = DeploymentPackageLayout.CertificateContractFile;

    public string Database { get; init; } = DeploymentPackageLayout.DatabaseContractFile;

    public string Secret { get; init; } = DeploymentPackageLayout.SecretContractFile;

    public string Configuration { get; init; } = DeploymentPackageLayout.ConfigurationContractFile;

    public string Dependency { get; init; } = DeploymentPackageLayout.DependencyContractFile;

    public string Health { get; init; } = DeploymentPackageLayout.HealthContractFile;

    public string Network { get; init; } = DeploymentPackageLayout.NetworkContractFile;

    public string Volume { get; init; } = DeploymentPackageLayout.VolumeContractFile;

    public string Feature { get; init; } = DeploymentPackageLayout.FeatureContractFile;

    public string RuntimeCapabilities { get; init; } = DeploymentPackageLayout.RuntimeCapabilitiesFile;
}

/// <summary>
/// Aggregate deployment contract metadata written to <c>deployment.contract.json</c>.
/// </summary>
public sealed class DeploymentContract
{
    public required string ServiceId { get; init; }

    public required string Version { get; init; }

    public required string ContractVersion { get; init; }

    public required ServiceContractReferences Files { get; init; }

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;
}

public static class PlatformRuntimeDefaults
{
    public const int ContainerUserId = 1654;

    public const string ConfigRoot = "/opt/aovias/runtime/config";

    public const string LogsRoot = "/opt/aovias/runtime/logs";

    public const string PackagesRoot = "/opt/aovias/runtime/packages";

    public const string CertificatesRoot = "/opt/aovias/runtime/config/certificates";
}

/// <summary>
/// Canonical deployment storage paths consumed by the Runtime Agent and publishing contracts.
/// </summary>
public static class PlatformDeploymentDefaults
{
    public const string HostDeploymentsRootToken = "${AOVIAS_HOST_DEPLOYMENTS_ROOT}";

    public const string DeploymentRoot = "/opt/aovias/deployments";

    public const string IncomingRoot = "/opt/aovias/deployments/incoming";

    public const string ReleasesRoot = "/opt/aovias/deployments/releases";

    public const string TempRoot = "/opt/aovias/deployments/temp";

    public const string StagingRoot = "/opt/aovias/deployments/staging";

    public const string ExtractionRoot = "/opt/aovias/deployments/extraction";

    public const string ActiveRoot = "/opt/aovias/deployments/active";

    public const string RollbackRoot = "/opt/aovias/deployments/rollback";

    public const string ArchiveRoot = "/opt/aovias/deployments/archive";

    public const string AuditRoot = "/opt/aovias/deployments/audit";

    public static IReadOnlyList<(string Label, string ContainerPath)> RequiredWritableResources { get; } =
    [
        ("Deployment Root", DeploymentRoot),
        ("Incoming", IncomingRoot),
        ("Releases", ReleasesRoot),
        ("Temp", TempRoot)
    ];

    public static IReadOnlyList<(string Label, string ContainerPath, bool ReadOnly)> RequiredRuntimeResources { get; } =
    [
        ("Runtime Config", PlatformRuntimeDefaults.ConfigRoot, true),
        ("Runtime Packages", PlatformRuntimeDefaults.PackagesRoot, false),
        ("Runtime Logs", PlatformRuntimeDefaults.LogsRoot, false)
    ];
}
