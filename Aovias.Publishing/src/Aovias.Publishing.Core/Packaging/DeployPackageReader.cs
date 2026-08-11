using System.IO.Compression;
using System.Text.Json;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Reading;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Reads <c>.aovdeploy</c> packages.
/// </summary>
public sealed class DeployPackageReader : IDeployPackageReader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async Task<DeployPackageContents?> ReadAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath) || !packagePath.EndsWith(DeployPackageLayout.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "aovdeploy-read", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            ZipFile.ExtractToDirectory(packagePath, tempDirectory);
            var manifest = ReadJson<DeployManifest>(Path.Combine(tempDirectory, DeployPackageLayout.ManifestFile));
            var health = ReadJson<DeployHealthDefinition>(Path.Combine(tempDirectory, DeployPackageLayout.HealthFile));
            var metadata = ReadJson<DeployPackageMetadata>(Path.Combine(tempDirectory, DeployPackageLayout.MetadataFile));
            var configPath = Path.Combine(tempDirectory, DeployPackageLayout.ConfigTemplateFile);
            var configContent = File.Exists(configPath) ? await File.ReadAllTextAsync(configPath, cancellationToken) : string.Empty;
            var config = ReadConfigurationTemplate(configPath);
            var checksums = ReadJson<Dictionary<string, string>>(Path.Combine(tempDirectory, DeployPackageLayout.ChecksumsFile)) ?? [];
            var composePath = Path.Combine(tempDirectory, DeployPackageLayout.ComposeFile);
            var composeContent = File.Exists(composePath) ? await File.ReadAllTextAsync(composePath, cancellationToken) : string.Empty;
            var envTemplatePath = Path.Combine(tempDirectory, DeployPackageLayout.EnvTemplateFile);
            var envTemplateContent = File.Exists(envTemplatePath) ? await File.ReadAllTextAsync(envTemplatePath, cancellationToken) : string.Empty;
            var imagePath = Path.Combine(tempDirectory, DeployPackageLayout.ServiceImageTar);
            var certificatePath = Path.Combine(tempDirectory, DeployPackageLayout.DeploymentReadinessCertificateFile);
            var certificate = ReadJson<DeploymentReadinessCertificate>(certificatePath);

            ServiceDeploymentBundle? deploymentContracts = null;
            try
            {
                deploymentContracts = ServiceDeploymentBundleIO.TryReadDirectory(tempDirectory);
            }
            catch (InvalidOperationException)
            {
                deploymentContracts = null;
            }

            if (manifest is null || health is null || metadata is null)
            {
                return null;
            }

            deploymentContracts ??= DeploymentPackageContractsResolver.ResolveFromDirectory(
                tempDirectory,
                new LegacyPackageMetadata
                {
                    ServiceId = manifest.Service,
                    DisplayName = manifest.Service,
                    Version = manifest.Version,
                    ImageName = manifest.ImageName,
                    ImageTag = manifest.ImageTag,
                    ContainerName = manifest.Container,
                    EntryPoint = manifest.EntryPoint,
                    HealthEndpoint = health.Endpoint,
                    Ports = manifest.Ports,
                    Dependencies = manifest.Dependencies.ToList(),
                    HealthTimeoutSeconds = health.TimeoutSeconds,
                    HealthRetries = health.Retries,
                    ConfigurationKeys = config.Keys.ToList()
                });

            return new DeployPackageContents
            {
                PackagePath = packagePath,
                Manifest = manifest,
                Health = health,
                Metadata = metadata,
                Checksums = checksums,
                ConfigurationTemplate = config,
                ConfigurationTemplateContent = configContent,
                ComposeContent = composeContent,
                EnvTemplateContent = envTemplateContent,
                HasServiceImage = File.Exists(imagePath),
                ReadinessCertificate = certificate,
                DeploymentContracts = deploymentContracts
            };
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup.
            }
        }
    }

    private static Dictionary<string, string> ReadConfigurationTemplate(string path)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path), JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static T? ReadJson<T>(string path)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
    }
}

/// <summary>
/// Validates <c>.aovdeploy</c> packages.
/// </summary>
public sealed class DeployPackageValidator : IDeployPackageValidator
{
    private readonly IDeployPackageReader packageReader;
    private readonly IDeploymentPackageContractReader contractReader;

    public DeployPackageValidator(IDeployPackageReader packageReader, IDeploymentPackageContractReader contractReader)
    {
        this.packageReader = packageReader;
        this.contractReader = contractReader;
    }

    /// <inheritdoc />
    public async Task<DeployPackageValidationResult> ValidateAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!File.Exists(packagePath))
        {
            return DeployPackageValidationResult.Invalid("Package file was not found.");
        }

        if (!packagePath.EndsWith(DeployPackageLayout.Extension, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Package extension must be .aovdeploy.");
        }

        using var archive = ZipFile.OpenRead(packagePath);
        foreach (var required in DeployPackageLayout.RequiredFiles)
        {
            if (archive.GetEntry(required) is null)
            {
                errors.Add($"Missing required file: {required}");
            }
        }

        try
        {
            _ = contractReader.ReadFromPackage(packagePath);
        }
        catch (DeploymentPackageContractException exception)
        {
            errors.Add(exception.Message);
        }

        var contents = await packageReader.ReadAsync(packagePath, cancellationToken);
        if (contents is null)
        {
            errors.Add("Unable to parse deployment package.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(contents.Manifest.Service))
            {
                errors.Add("Manifest service name is required.");
            }

            if (!contents.HasServiceImage)
            {
                errors.Add("service-image.tar is missing or empty.");
            }

            if (string.IsNullOrWhiteSpace(contents.ComposeContent))
            {
                errors.Add("docker-compose.yml is empty.");
            }

            if (string.IsNullOrWhiteSpace(contents.EnvTemplateContent))
            {
                errors.Add(".env.template is empty.");
            }

            if (contents.Manifest.Ports.Count > 0
                && !contents.ComposeContent.Contains("ports:", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("docker-compose.yml must expose service ports.");
            }
        }

        return errors.Count == 0
            ? DeployPackageValidationResult.Valid(warnings)
            : DeployPackageValidationResult.Invalid(errors.ToArray());
    }
}
