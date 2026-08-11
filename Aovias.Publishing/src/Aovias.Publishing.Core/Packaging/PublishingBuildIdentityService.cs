using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Execution;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// Computes deterministic source revisions and publishing input fingerprints (M1.2 D1/D3).
/// </summary>
public sealed class PublishingBuildIdentityService
{
    private readonly IProcessRunner processRunner;
    private readonly IOptionsMonitor<PublishingSettings> settings;

    public PublishingBuildIdentityService(IProcessRunner processRunner, IOptionsMonitor<PublishingSettings> settings)
    {
        this.processRunner = processRunner;
        this.settings = settings;
    }

    public PublishingBuildIdentity EstablishPreDocker(PublishingContext context)
    {
        var discovery = context.Discovery ?? throw new InvalidOperationException("Discovery is required.");
        var gitCommit = PublishingGitRevisionResolver.TryResolveCommit(discovery.ProjectRoot, processRunner);
        var sourceRevision = ComputeSourceRevision(context, gitCommit);
        context.RuntimeAgentBuildRevision = sourceRevision;

        var buildUtc = DateTimeOffset.UtcNow;
        var buildVersion = context.Request.Version ?? PublishingVersionPolicy.DefaultPackageVersion;
        var identity = new PublishingBuildIdentity
        {
            SourceRevision = sourceRevision,
            GitCommit = gitCommit,
            BuildUtc = buildUtc,
            BuildVersion = buildVersion,
            PublishingInputFingerprint = sourceRevision,
            PublishingEngineVersion = PublishingVersionPolicy.EngineVersion,
            FingerprintPolicyVersion = FingerprintPolicy.Version,
            BuildConfiguration = "Release",
            TargetFramework = discovery.Framework,
            RuntimeIdentifier = settings.CurrentValue.TargetRuntimeOs,
            Profile = context.Request.Profile
        };

        context.BuildIdentity = identity;
        return identity;
    }

    public PublishingBuildIdentity FinalizeInputFingerprint(PublishingContext context)
    {
        var identity = context.BuildIdentity ?? EstablishPreDocker(context);
        var fingerprint = ComputePublishingInputFingerprint(context, identity);
        identity = new PublishingBuildIdentity
        {
            SourceRevision = identity.SourceRevision,
            GitCommit = identity.GitCommit,
            BuildUtc = identity.BuildUtc,
            BuildVersion = identity.BuildVersion,
            PublishingInputFingerprint = fingerprint,
            PackageFingerprint = identity.PackageFingerprint,
            DockerImageIdentity = identity.DockerImageIdentity,
            PublishingEngineVersion = identity.PublishingEngineVersion,
            FingerprintPolicyVersion = identity.FingerprintPolicyVersion,
            BuildConfiguration = identity.BuildConfiguration,
            TargetFramework = identity.TargetFramework,
            RuntimeIdentifier = identity.RuntimeIdentifier,
            Profile = identity.Profile
        };

        context.BuildIdentity = identity;
        ApplyManifestTraceability(context, identity);
        return identity;
    }

    public void ApplyDockerImageIdentity(PublishingContext context, string dockerImageIdentity)
    {
        var identity = context.BuildIdentity ?? throw new InvalidOperationException("Build identity is required.");
        identity = new PublishingBuildIdentity
        {
            SourceRevision = identity.SourceRevision,
            GitCommit = identity.GitCommit,
            BuildUtc = identity.BuildUtc,
            BuildVersion = identity.BuildVersion,
            PublishingInputFingerprint = identity.PublishingInputFingerprint,
            PackageFingerprint = identity.PackageFingerprint,
            DockerImageIdentity = dockerImageIdentity,
            PublishingEngineVersion = identity.PublishingEngineVersion,
            FingerprintPolicyVersion = identity.FingerprintPolicyVersion,
            BuildConfiguration = identity.BuildConfiguration,
            TargetFramework = identity.TargetFramework,
            RuntimeIdentifier = identity.RuntimeIdentifier,
            Profile = identity.Profile
        };

        context.BuildIdentity = identity;
        ApplyManifestTraceability(context, identity);
    }

    public IReadOnlyDictionary<string, string> ComputeStagingArtifactFingerprints(PublishingContext context)
    {
        var staging = context.StagingDirectory;
        if (string.IsNullOrWhiteSpace(staging) || !Directory.Exists(staging))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var fingerprints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(staging).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, DeployPackageLayout.ChecksumsFile, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, DeployPackageLayout.PublishingBuildManifestFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(name, DeployPackageLayout.ServiceImageTar, StringComparison.OrdinalIgnoreCase))
            {
                fingerprints[name] = context.BuildIdentity?.DockerImageIdentity ?? PublishingContentHasher.HashFile(file);
                continue;
            }

            fingerprints[name] = PublishingContentHasher.HashFile(file);
        }

        return fingerprints;
    }

    private static string ComputeSourceRevision(PublishingContext context, string? gitCommit)
    {
        var discovery = context.Discovery!;
        var components = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["project"] = File.Exists(discovery.ProjectPath)
                ? PublishingContentHasher.HashFile(discovery.ProjectPath)
                : PublishingContentHasher.HashText(discovery.ProjectPath)
        };

        if (!string.IsNullOrWhiteSpace(context.DockerfilePath) && File.Exists(context.DockerfilePath))
        {
            components["dockerfile"] = PublishingContentHasher.HashFile(context.DockerfilePath);
        }

        if (!string.IsNullOrWhiteSpace(context.PublishOutputDirectory) && Directory.Exists(context.PublishOutputDirectory))
        {
            components["publishOutput"] = PublishingContentHasher.HashDirectory(context.PublishOutputDirectory);
        }

        if (!string.IsNullOrWhiteSpace(gitCommit))
        {
            components["gitCommit"] = gitCommit;
        }

        return PublishingContentHasher.HashCanonicalJson(components);
    }

    private static string ComputePublishingInputFingerprint(PublishingContext context, PublishingBuildIdentity identity)
    {
        var discovery = context.Discovery!;
        var components = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceRevision"] = identity.SourceRevision,
            ["serviceId"] = context.Request.ServiceId ?? discovery.ServiceId,
            ["version"] = identity.BuildVersion,
            ["buildConfiguration"] = identity.BuildConfiguration,
            ["framework"] = discovery.Framework ?? string.Empty,
            ["runtimeIdentifier"] = identity.RuntimeIdentifier ?? string.Empty,
            ["fingerprintPolicyVersion"] = identity.FingerprintPolicyVersion.ToString()
        };

        if (!string.IsNullOrWhiteSpace(identity.GitCommit))
        {
            components["gitCommit"] = identity.GitCommit;
        }

        if (!string.IsNullOrWhiteSpace(identity.DockerImageIdentity))
        {
            components["dockerImageIdentity"] = identity.DockerImageIdentity;
        }

        if (!string.IsNullOrWhiteSpace(context.ConfigurationTemplateJson))
        {
            components["configTemplate"] = PublishingContentHasher.HashText(context.ConfigurationTemplateJson);
        }

        if (!string.IsNullOrWhiteSpace(context.EnvTemplateContent))
        {
            components["envTemplate"] = PublishingContentHasher.HashText(context.EnvTemplateContent);
        }

        if (!string.IsNullOrWhiteSpace(context.ComposeContent))
        {
            components["compose"] = PublishingContentHasher.HashText(context.ComposeContent);
        }

        if (context.DeploymentContracts is not null)
        {
            components["contracts"] = HashDeploymentContracts(context.DeploymentContracts);
        }

        return PublishingContentHasher.HashCanonicalJson(components);
    }

    private static string HashDeploymentContracts(Aovias.Deployment.Contracts.ServiceDeploymentBundle contracts)
    {
        var builder = new System.Text.StringBuilder();
        AppendContractHash(builder, "manifest", contracts.Manifest);
        AppendContractHash(builder, "deployment", contracts.Deployment);
        AppendContractHash(builder, "filesystem", contracts.Filesystem);
        AppendContractHash(builder, "volume", contracts.Volume);
        AppendContractHash(builder, "health", contracts.Health);
        AppendContractHash(builder, "network", contracts.Network);
        AppendContractHash(builder, "dependency", contracts.Dependency);
        AppendContractHash(builder, "configuration", contracts.Configuration);
        AppendContractHash(builder, "secret", contracts.Secret);
        AppendContractHash(builder, "database", contracts.Database);
        AppendContractHash(builder, "certificate", contracts.Certificate);
        AppendContractHash(builder, "feature", contracts.Feature);
        AppendContractHash(builder, "runtimeCapabilities", contracts.RuntimeCapabilities);
        return PublishingContentHasher.HashText(builder.ToString());
    }

    private static void AppendContractHash(System.Text.StringBuilder builder, string name, object? contract)
    {
        if (contract is null)
        {
            return;
        }

        builder.Append(name);
        builder.Append('|');
        builder.Append(PublishingContentHasher.HashText(System.Text.Json.JsonSerializer.Serialize(contract)));
        builder.Append('|');
    }

    private static void ApplyManifestTraceability(PublishingContext context, PublishingBuildIdentity identity)
    {
        var manifest = context.Manifest;
        if (manifest is null)
        {
            return;
        }

        context.Manifest = new DeployManifest
        {
            Service = manifest.Service,
            Version = manifest.Version,
            Type = manifest.Type,
            Container = manifest.Container,
            Health = manifest.Health,
            EntryPoint = manifest.EntryPoint,
            Ports = manifest.Ports,
            Dependencies = manifest.Dependencies,
            Configuration = manifest.Configuration,
            ImageName = manifest.ImageName,
            ImageTag = manifest.ImageTag,
            SourceRevision = identity.SourceRevision,
            PublishingInputFingerprint = identity.PublishingInputFingerprint,
            DockerImageIdentity = identity.DockerImageIdentity
        };
    }
}
