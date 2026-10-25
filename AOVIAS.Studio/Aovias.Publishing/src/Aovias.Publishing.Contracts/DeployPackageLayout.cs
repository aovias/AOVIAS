namespace Aovias.Publishing.Contracts;

/// <summary>
/// Canonical file names inside a <c>.aovdeploy</c> package.
/// </summary>
public static class DeployPackageLayout
{
    public const string Extension = ".aovdeploy";

    public const string ManifestFile = "manifest.json";

    public const string ComposeFile = "docker-compose.yml";

    public const string HealthFile = "health.json";

    public const string ConfigTemplateFile = "config-template.json";

    public const string EnvTemplateFile = ".env.template";

    public const string ServiceImageTar = "service-image.tar";

    public const string ChecksumsFile = "checksums.json";

    public const string MetadataFile = "metadata.json";

    public const string DeploymentReadinessCertificateFile = "deployment-readiness.json";

    public const string PublishingReportFile = "publishing-report.json";

    public const string PublishingBuildManifestFile = "publishing-build-manifest.json";

    public const string ServiceManifestFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.ServiceManifestFile;

    public const string DeploymentContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.DeploymentContractFile;

    public const string FilesystemContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.FilesystemContractFile;

    public const string CertificateContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.CertificateContractFile;

    public const string DatabaseContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.DatabaseContractFile;

    public const string SecretContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.SecretContractFile;

    public const string ConfigurationContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.ConfigurationContractFile;

    public const string DependencyContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.DependencyContractFile;

    public const string HealthContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.HealthContractFile;

    public const string NetworkContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.NetworkContractFile;

    public const string VolumeContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.VolumeContractFile;

    public const string FeatureContractFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.FeatureContractFile;

    public const string RuntimeCapabilitiesFile = Aovias.Deployment.Contracts.DeploymentPackageLayout.RuntimeCapabilitiesFile;

    public static readonly IReadOnlyList<string> ContractFiles = Aovias.Deployment.Contracts.DeploymentPackageLayout.ContractFiles;

    public static readonly IReadOnlyList<string> RequiredFiles =
    [
        ManifestFile,
        ComposeFile,
        HealthFile,
        ConfigTemplateFile,
        EnvTemplateFile,
        ServiceImageTar,
        ChecksumsFile,
        MetadataFile
    ];

    public static readonly IReadOnlyList<string> PostValidationFiles =
    [
        DeploymentReadinessCertificateFile,
        PublishingReportFile,
        PublishingBuildManifestFile
    ];
}

/// <summary>
/// Future bundle layout constants for <c>.aovbundle</c> releases.
/// </summary>
public static class BundleLayout
{
    public const string Extension = ".aovbundle";

    public const string BundleManifestFile = "bundle-manifest.json";

    public const string PackagesFolder = "packages";
}
