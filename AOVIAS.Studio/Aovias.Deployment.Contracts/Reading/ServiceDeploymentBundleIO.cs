namespace Aovias.Deployment.Contracts.Reading;

using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Serialization;

/// <summary>
/// Reads and writes deployment contract files from a package directory.
/// </summary>
public static class ServiceDeploymentBundleIO
{
    public static ServiceDeploymentBundle? TryReadDirectory(string packageDirectory)
    {
        var manifestPath = Path.Combine(packageDirectory, DeploymentPackageLayout.ServiceManifestFile);
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        var manifest = DeploymentContractJson.ReadFile<ServiceManifest>(manifestPath)
            ?? throw new InvalidOperationException("service.manifest.json is invalid.");

        return new ServiceDeploymentBundle
        {
            Manifest = manifest,
            Deployment = ReadRequired<DeploymentContract>(packageDirectory, DeploymentPackageLayout.DeploymentContractFile),
            Filesystem = ReadRequired<FilesystemContract>(packageDirectory, DeploymentPackageLayout.FilesystemContractFile),
            Certificate = ReadRequired<CertificateContract>(packageDirectory, DeploymentPackageLayout.CertificateContractFile),
            Database = ReadRequired<DatabaseContract>(packageDirectory, DeploymentPackageLayout.DatabaseContractFile),
            Secret = ReadRequired<SecretContract>(packageDirectory, DeploymentPackageLayout.SecretContractFile),
            Configuration = ReadRequired<ConfigurationContract>(packageDirectory, DeploymentPackageLayout.ConfigurationContractFile),
            Dependency = ReadRequired<DependencyContract>(packageDirectory, DeploymentPackageLayout.DependencyContractFile),
            Health = ReadRequired<HealthContract>(packageDirectory, DeploymentPackageLayout.HealthContractFile),
            Network = ReadRequired<NetworkContract>(packageDirectory, DeploymentPackageLayout.NetworkContractFile),
            Volume = ReadRequired<VolumeContract>(packageDirectory, DeploymentPackageLayout.VolumeContractFile),
            Feature = ReadRequired<FeatureContract>(packageDirectory, DeploymentPackageLayout.FeatureContractFile),
            RuntimeCapabilities = ReadRequired<RuntimeCapabilities>(packageDirectory, DeploymentPackageLayout.RuntimeCapabilitiesFile),
            IsLegacySynthesized = false
        };
    }

    public static void WriteDirectory(string packageDirectory, ServiceDeploymentBundle bundle)
    {
        Directory.CreateDirectory(packageDirectory);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.ServiceManifestFile), bundle.Manifest);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.DeploymentContractFile), bundle.Deployment);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.FilesystemContractFile), bundle.Filesystem);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.CertificateContractFile), bundle.Certificate);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.DatabaseContractFile), bundle.Database);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.SecretContractFile), bundle.Secret);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.ConfigurationContractFile), bundle.Configuration);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.DependencyContractFile), bundle.Dependency);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.HealthContractFile), bundle.Health);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.NetworkContractFile), bundle.Network);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.VolumeContractFile), bundle.Volume);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.FeatureContractFile), bundle.Feature);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.RuntimeCapabilitiesFile), bundle.RuntimeCapabilities);
        if (IdentityDeploymentContractBuilder.IsIdentityService(bundle))
        {
            DeploymentContractJson.WriteFile(
                Path.Combine(packageDirectory, DeploymentPackageLayout.IdentityContractFile),
                IdentityDeploymentContractBuilder.BuildIdentityContract(bundle));
            DeploymentContractJson.WriteFile(
                Path.Combine(packageDirectory, DeploymentPackageLayout.IdentitySecretsContractFile),
                IdentityDeploymentContractBuilder.BuildIdentitySecretsContract(bundle));
        }
    }

    private static T ReadRequired<T>(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Required deployment contract file '{fileName}' is missing.");
        }

        return DeploymentContractJson.ReadFile<T>(path)
            ?? throw new InvalidOperationException($"Deployment contract file '{fileName}' is invalid.");
    }
}
