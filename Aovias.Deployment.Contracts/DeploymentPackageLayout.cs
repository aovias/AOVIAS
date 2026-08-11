namespace Aovias.Deployment.Contracts;

/// <summary>
/// Canonical file names for deployment contracts inside a <c>.aovdeploy</c> package.
/// </summary>
public static class DeploymentPackageLayout
{
    public const string ServiceManifestFile = "service.manifest.json";
    public const string DeploymentContractFile = "deployment.contract.json";
    public const string FilesystemContractFile = "filesystem.contract.json";
    public const string CertificateContractFile = "certificate.contract.json";
    public const string DatabaseContractFile = "database.contract.json";
    public const string SecretContractFile = "secret.contract.json";
    public const string ConfigurationContractFile = "configuration.contract.json";
    public const string DependencyContractFile = "dependency.contract.json";
    public const string HealthContractFile = "health.contract.json";
    public const string NetworkContractFile = "network.contract.json";
    public const string VolumeContractFile = "volume.contract.json";
    public const string FeatureContractFile = "feature.contract.json";
    public const string RuntimeCapabilitiesFile = "runtime-capabilities.json";

    public const string IdentityContractFile = "identity.contract.json";

    public const string IdentitySecretsContractFile = "identity.secrets.contract.json";

    public const string InfrastructureContractFile = "infrastructure.contract.json";

    public static readonly IReadOnlyList<string> IdentityContractFiles =
    [
        IdentityContractFile,
        IdentitySecretsContractFile
    ];

    public static readonly IReadOnlyList<string> ContractFiles =
    [
        ServiceManifestFile,
        DeploymentContractFile,
        FilesystemContractFile,
        CertificateContractFile,
        DatabaseContractFile,
        SecretContractFile,
        ConfigurationContractFile,
        DependencyContractFile,
        HealthContractFile,
        NetworkContractFile,
        VolumeContractFile,
        FeatureContractFile,
        RuntimeCapabilitiesFile,
        InfrastructureContractFile
    ];
}
