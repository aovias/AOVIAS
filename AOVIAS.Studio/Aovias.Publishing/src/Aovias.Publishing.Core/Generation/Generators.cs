using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Validation;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Generation.Compose;
using Aovias.Publishing.Core.Validation;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Generation;

/// <summary>
/// Generates deployment manifest JSON.
/// </summary>
public sealed class DeployManifestGenerator : IDeployManifestGenerator
{
    /// <inheritdoc />
    public DeployManifest Generate(PublishingContext context)
    {
        var discovery = context.Discovery ?? throw new InvalidOperationException("Discovery is required.");
        var metadata = context.DeploymentMetadata
                       ?? throw new InvalidOperationException("Deployment metadata is required.");
        var version = context.Request.Version ?? PublishingVersionPolicy.DefaultPackageVersion;
        var serviceId = metadata.ServiceId;
        var imageName = context.DockerImageName ?? PublishingImageNaming.GetImageName(serviceId);
        var imageTag = context.DockerImageTag ?? version;

        return new DeployManifest
        {
            Service = serviceId,
            Version = version,
            Type = discovery.ProjectType.ToString(),
            Container = serviceId.ToLowerInvariant(),
            Health = metadata.Health.Endpoint,
            EntryPoint = discovery.EntryPoint ?? $"{Path.GetFileNameWithoutExtension(discovery.ProjectPath)}.dll",
            Ports = [metadata.Ports.ContainerPort],
            Dependencies = metadata.GetRequiredServiceDependencies(),
            Configuration = DeployPackageLayout.ConfigTemplateFile,
            ImageName = imageName,
            ImageTag = imageTag
        };
    }
}

/// <summary>
/// Generates health definition JSON.
/// </summary>
public sealed class HealthDefinitionGenerator : IHealthDefinitionGenerator
{
    /// <inheritdoc />
    public DeployHealthDefinition Generate(PublishingContext context)
    {
        var metadata = context.DeploymentMetadata
                       ?? throw new InvalidOperationException("Deployment metadata is required.");

        return new DeployHealthDefinition
        {
            Endpoint = metadata.Health.Endpoint,
            ExpectedStatus = metadata.Health.ExpectedStatus,
            TimeoutSeconds = metadata.Health.TimeoutSeconds,
            Retries = metadata.Health.Retries
        };
    }
}

/// <summary>
/// Generates environment-neutral configuration templates.
/// </summary>
public sealed class ConfigTemplateGenerator : IConfigTemplateGenerator
{
    private static readonly string[] SecretTokens =
    [
        "password", "secret", "key", "token", "connectionstring", "credential", "apikey", "jwt"
    ];

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, string>> GenerateAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var discovery = context.Discovery
                        ?? throw new InvalidOperationException("Discovery is required to generate config-template.json.");

        var flattened = DeploymentArtifactSanitizer.IsRuntimeAgentService(context)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : CollectApplicationSettings(discovery);

        var template = BuildTemplate(flattened);
        DeploymentArtifactSanitizer.SanitizeTemplateNode(template, string.Empty);
        var json = DeploymentArtifactSanitizer.PrepareConfigurationTemplateJson(template.ToJsonString(JsonOptions));

        context.ConfigurationTemplateJson = json;
        context.ConfigurationTemplate.Clear();
        foreach (var pair in flattened)
        {
            context.ConfigurationTemplate[pair.Key] = pair.Value;
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>>(flattened);
    }

    private static Dictionary<string, string> CollectApplicationSettings(ProjectDiscoveryResult discovery)
    {
        var flattened = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in EnumerateAppsettingsFiles(discovery))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            FlattenJson(document.RootElement, Path.GetFileNameWithoutExtension(file), flattened);
        }

        foreach (var key in flattened.Keys.ToList())
        {
            if (IsBootstrapKey(key))
            {
                flattened.Remove(key);
                continue;
            }

            if (IsSecretKey(key))
            {
                flattened[key] = $"${{{PlaceholderFormatter.ToPlaceholder(key)}}}";
                continue;
            }

            flattened[key] = DeploymentArtifactSanitizer.NeutralizeConfigurationValue(key, flattened[key]);
        }

        return flattened;
    }

    private static IEnumerable<string> EnumerateAppsettingsFiles(ProjectDiscoveryResult discovery)
    {
        var candidates = new List<string>();
        if (Directory.Exists(discovery.ProjectRoot))
        {
            candidates.AddRange(Directory.EnumerateFiles(discovery.ProjectRoot, "appsettings*.json", SearchOption.TopDirectoryOnly));
        }

        var projectDirectory = Path.GetDirectoryName(discovery.ProjectPath);
        if (!string.IsNullOrWhiteSpace(projectDirectory)
            && Directory.Exists(projectDirectory)
            && !string.Equals(projectDirectory, discovery.ProjectRoot, StringComparison.OrdinalIgnoreCase))
        {
            candidates.AddRange(Directory.EnumerateFiles(projectDirectory, "appsettings*.json", SearchOption.TopDirectoryOnly));
        }

        return candidates
            .Where(file => !file.EndsWith(".Development.json", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static JsonObject BuildTemplate(IReadOnlyDictionary<string, string> flattened)
    {
        var template = new JsonObject
        {
            ["Aovias"] = new JsonObject
            {
                ["ConfigRoot"] = "${AOVIAS_CONFIG_ROOT}",
                ["ProfilesDirectory"] = "${AOVIAS_PROFILES_DIRECTORY}",
                ["SecretsDirectory"] = "${AOVIAS_SECRETS_DIRECTORY}",
                ["ActiveProfile"] = "${AOVIAS_ACTIVE_PROFILE}"
            }
        };

        if (flattened.Count == 0)
        {
            return template;
        }

        var application = new JsonObject();
        foreach (var pair in flattened)
        {
            application[pair.Key.Replace(':', '_')] = pair.Value;
        }

        template["Application"] = application;
        return template;
    }

    private static void FlattenJson(JsonElement element, string prefix, IDictionary<string, string> target)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";
                    FlattenJson(property.Value, key, target);
                }

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    FlattenJson(item, $"{prefix}:{index}", target);
                    index++;
                }

                break;
            default:
                target[prefix] = element.ToString();
                break;
        }
    }

    private static bool IsSecretKey(string key)
        => SecretTokens.Any(token => key.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static bool IsBootstrapKey(string key)
        => key.StartsWith("appsettings:Aovias:", StringComparison.OrdinalIgnoreCase)
           && (key.EndsWith(":ConfigRoot", StringComparison.OrdinalIgnoreCase)
               || key.EndsWith(":ActiveProfile", StringComparison.OrdinalIgnoreCase)
               || key.EndsWith(":ProfilesDirectory", StringComparison.OrdinalIgnoreCase)
               || key.EndsWith(":SecretsDirectory", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Generates the environment-neutral deployment file (.env.template). Concrete
/// profile values are resolved at deploy time by the Runtime Agent.
/// </summary>
public sealed class EnvironmentFileGenerator : IEnvTemplateGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var content = DeploymentEnvironmentTemplate.Generate();
        context.EnvTemplateContent = content;
        return Task.FromResult(content);
    }
}

/// <summary>
/// Generates docker-compose.yml from a schema-driven object model serialized
/// with YamlDotNet. No template files, no token replacement, no version field.
/// </summary>
public sealed class ComposeGenerator : IComposeGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var manifest = context.Manifest ?? throw new InvalidOperationException("Manifest is required.");
        var contracts = context.DeploymentContracts
                        ?? throw new InvalidOperationException(
                            "Deployment contracts are required before compose generation. Ensure ContractGeneration runs before ComposeGeneration.");

        var document = ComposeDocumentBuilder.Build(manifest, contracts.Volume, contracts.Health);
        var volumeEntries = ComposeMountBuilder.BuildVolumeEntries(contracts.Volume);
        var validation = ComposeMountValidator.ValidateVolumeEntries(volumeEntries, contracts.Volume, contracts.Filesystem);
        if (!validation.Passed)
        {
            throw new InvalidOperationException(
                $"Generated docker-compose.yml failed infrastructure validation: {string.Join("; ", validation.Errors)}");
        }

        var content = ComposeYamlSerializer.Serialize(document);

        EnsureRequiredTokens(content);

        // Assign before running the neutrality gate: PublishingEnvironmentNeutralityValidator
        // reads context.ComposeContent, so it must see the content just generated rather than
        // whatever (or nothing) was set before this stage ran.
        context.ComposeContent = content;

        var neutralityErrors = PublishingEnvironmentNeutralityValidator.Validate(context);
        if (neutralityErrors.Count > 0)
        {
            context.ComposeContent = null;
            throw new InvalidOperationException(string.Join("; ", neutralityErrors));
        }

        var serializedValidation = ComposeMountValidator.Validate(content, contracts.Volume, contracts.Filesystem);
        if (!serializedValidation.Passed)
        {
            context.ComposeContent = null;
            throw new InvalidOperationException(
                $"Serialized docker-compose.yml failed infrastructure validation: {string.Join("; ", serializedValidation.Errors)}");
        }

        return Task.FromResult(content);
    }

    private static readonly string[] RequiredComposeTokens =
    [
        "services:", "image:", "container_name:", "restart:", "ports:",
        "environment:", "volumes:", "healthcheck:", "networks:",
        "${AOVIAS_CONFIG_ROOT}", "${AOVIAS_LOGS_ROOT}", "${AOVIAS_DEPLOYMENTS_ROOT}",
        "${AOVIAS_DOCKER_NETWORK}"
    ];

    private static void EnsureRequiredTokens(string compose)
    {
        var missing = RequiredComposeTokens
            .Where(token => !compose.Contains(token, StringComparison.Ordinal))
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Generated docker-compose.yml is missing required tokens: {string.Join(", ", missing)}.");
        }

        if (compose.Contains("version:", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Generated docker-compose.yml must not contain an obsolete 'version' field.");
        }
    }
}

/// <summary>
/// Generates or validates Dockerfiles without overwriting custom files.
/// </summary>
public sealed class DockerfileGenerator : IDockerfileGenerator
{
    private readonly IOptionsMonitor<PublishingSettings> settings;

    public DockerfileGenerator(IOptionsMonitor<PublishingSettings> settings)
    {
        this.settings = settings;
    }

    /// <inheritdoc />
    public Task<PublishingStepResult> PrepareDockerfileAsync(PublishingContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var discovery = context.Discovery ?? throw new InvalidOperationException("Discovery is required.");
        var metadata = context.DeploymentMetadata
                       ?? throw new InvalidOperationException("Deployment metadata is required.");
        var projectRoot = discovery.ProjectRoot;
        var existingDockerfile = Path.Combine(projectRoot, "Dockerfile");
        if (!File.Exists(existingDockerfile))
        {
            existingDockerfile = Path.Combine(Path.GetDirectoryName(discovery.ProjectPath) ?? projectRoot, "Dockerfile");
        }

        if (File.Exists(existingDockerfile))
        {
            var content = File.ReadAllText(existingDockerfile);
            if (content.Contains("FROM", StringComparison.OrdinalIgnoreCase))
            {
                context.DockerfilePath = existingDockerfile;

                if (RuntimeAgentDockerContextStager.IsRuntimeAgentDiscovery(discovery.ServiceId, discovery.ProjectRoot)
                    && RuntimeAgentDockerContextStager.UsesMonorepoDockerfile(content))
                {
                    var platformRoot = RuntimeAgentDockerContextStager.TryResolvePlatformRoot(discovery.ProjectRoot)
                        ?? RuntimeAgentDockerContextStager.TryResolvePlatformRoot(existingDockerfile);

                    if (string.IsNullOrWhiteSpace(platformRoot))
                    {
                        return Task.FromResult(PublishingStepResult.Failure(
                            "Unable to locate the AOVIAS platform root for Runtime Agent Docker build. "
                            + "Expected Aovias.Configuration and Aovias.Runtime.Agent alongside each other."));
                    }

                    var stagingRoot = Path.Combine(context.WorkingDirectory ?? settings.CurrentValue.TemporaryFolder, "runtime-agent-docker");
                    context.DockerBuildContext = RuntimeAgentDockerContextStager.Stage(platformRoot, stagingRoot, content);

                    var stagedDockerfile = Path.Combine(stagingRoot, "Dockerfile");
                    File.Copy(existingDockerfile, stagedDockerfile, overwrite: true);
                    context.DockerfilePath = stagedDockerfile;

                    context.Warnings.Add($"Staged Runtime Agent Docker context from {platformRoot}.");
                }
                else
                {
                    context.DockerBuildContext = ResolveDockerBuildContext(discovery, existingDockerfile);
                }

                context.Warnings.Add("Using existing project Dockerfile.");
                return Task.FromResult(PublishingStepResult.Success("Existing Dockerfile validated."));
            }

            return Task.FromResult(PublishingStepResult.Failure("Existing Dockerfile is invalid."));
        }

        var templatePath = ResolveTemplatePath(discovery.ProjectType);
        if (!File.Exists(templatePath))
        {
            return Task.FromResult(PublishingStepResult.Failure($"Dockerfile template not found: {templatePath}"));
        }

        var buildContext = context.DockerBuildContext ?? Path.Combine(context.WorkingDirectory ?? settings.CurrentValue.TemporaryFolder, "docker-context");
        Directory.CreateDirectory(buildContext);

        var dockerfilePath = Path.Combine(buildContext, "Dockerfile");
        var template = File.ReadAllText(templatePath)
            .Replace("{{PORT}}", metadata.Ports.ContainerPort.ToString(), StringComparison.Ordinal)
            .Replace("{{ENTRYPOINT}}", discovery.EntryPoint ?? "app.dll", StringComparison.Ordinal);

        File.WriteAllText(dockerfilePath, template);
        context.DockerfilePath = dockerfilePath;
        context.DockerBuildContext = buildContext;
        context.GeneratedFiles.Add(dockerfilePath);
        return Task.FromResult(PublishingStepResult.Success("Generated Dockerfile from template."));
    }

    private string ResolveTemplatePath(PublishingProjectType projectType) => projectType switch
    {
        PublishingProjectType.WorkerService => settings.CurrentValue.DockerfileWorkerTemplatePath,
        PublishingProjectType.Angular => settings.CurrentValue.DockerfileAngularTemplatePath,
        PublishingProjectType.StaticWeb => settings.CurrentValue.DockerfileStaticTemplatePath,
        _ => settings.CurrentValue.DockerfileAspNetTemplatePath
    };

    private static string ResolveDockerBuildContext(ProjectDiscoveryResult discovery, string dockerfilePath)
    {
        var platformRoot = RuntimeAgentDockerContextStager.TryResolvePlatformRoot(discovery.ProjectRoot)
            ?? RuntimeAgentDockerContextStager.TryResolvePlatformRoot(dockerfilePath);

        if (!string.IsNullOrWhiteSpace(platformRoot)
            && RuntimeAgentDockerContextStager.IsRuntimeAgentDiscovery(discovery.ServiceId, discovery.ProjectRoot))
        {
            return platformRoot;
        }

        return Path.GetDirectoryName(dockerfilePath) ?? discovery.ProjectRoot;
    }
}
