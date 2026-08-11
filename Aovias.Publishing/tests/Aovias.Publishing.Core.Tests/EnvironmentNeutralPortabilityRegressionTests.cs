using Aovias.Configuration.Deployment;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.DeploymentValidation.Rules;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Reporting;
using Aovias.Publishing.Core.Validation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

/// <summary>
/// Sprint M1.4 regression suite: proves a single generated deployment package is
/// environment-neutral and portable across Local/Demo/QA/UAT/Production without
/// republishing. Extends (does not replace) the M1.2/M1.3 determinism and
/// contract-origination regression suites.
/// </summary>
public sealed class EnvironmentNeutralPortabilityRegressionTests
{
    private static readonly string[] CanonicalServiceIds =
    [
        "runtime-agent", "gateway", "identity-service", "organization-service",
        "compliance-service", "audit-service", "ai", "knowledge-base", "portal", "outbox-worker"
    ];

    private static string ConfigRoot => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "aovias-config"));

    // Shared empty directory used as a stand-in project root: no appsettings*.json
    // exists here, so config-template generation stays hermetic across every call.
    private static readonly string EmptyProjectRoot = CreateEmptyProjectRoot();

    private static string CreateEmptyProjectRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "aovias-m1-4-tests", "project-root", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    // ---------------------------------------------------------------
    // D3/D4 — every canonical service's contract-derived volume/network
    // model references only deployment variables, never a concrete host path.
    // ---------------------------------------------------------------

    [Theory]
    [MemberData(nameof(ServiceIds))]
    public void ServiceContractRegistry_volume_mounts_use_only_host_tokens(string serviceId)
    {
        var bundle = BuildBundle(serviceId);

        Assert.NotEmpty(bundle.Volume.Mounts);
        foreach (var mount in bundle.Volume.Mounts)
        {
            Assert.StartsWith("${AOVIAS_HOST_", mount.HostToken, StringComparison.Ordinal);
            Assert.EndsWith("}", mount.HostToken, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(ServiceIds))]
    public void ServiceContractRegistry_network_uses_deployment_variable(string serviceId)
    {
        var bundle = BuildBundle(serviceId);

        Assert.Equal("${AOVIAS_DOCKER_NETWORK}", bundle.Network.NetworkNameToken);
        Assert.True(bundle.Network.ExternalNetwork);
    }

    [Theory]
    [MemberData(nameof(ServiceIds))]
    public void ServiceContractRegistry_filesystem_paths_are_container_internal_not_host_paths(string serviceId)
    {
        var bundle = BuildBundle(serviceId);

        foreach (var directory in bundle.Filesystem.Directories.Concat(bundle.Filesystem.ReadOnlyDirectories))
        {
            Assert.StartsWith("/", directory.Path, StringComparison.Ordinal);
            Assert.Empty(EnvironmentNeutralRules.ScanPackageArtifacts(directory.Path, "filesystem.contract.json"));
        }
    }

    public static IEnumerable<object[]> ServiceIds()
        => CanonicalServiceIds.Select(id => new object[] { id });

    private static ServiceDeploymentBundle BuildBundle(string serviceId)
        => ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = serviceId,
            Version = "1.0.0",
            ImageReference = $"aovias/{serviceId}:1.0.0",
            ContainerName = serviceId,
            EntryPoint = $"{serviceId}.dll",
            HealthEndpoint = "/health",
            Ports = [8080]
        });

    // ---------------------------------------------------------------
    // D2/D3 — the full generated artifact set for a representative service
    // passes the pipeline's own environment-neutrality gate with zero errors.
    // ---------------------------------------------------------------

    [Fact]
    public async Task Generated_artifact_set_passes_environment_neutrality_gate()
    {
        var context = await GenerateArtifactsAsync("gateway", activeProfile: "demo");

        var errors = PublishingEnvironmentNeutralityValidator.Validate(context);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task Generated_artifact_set_never_contains_forbidden_tokens_or_hardcoded_network_names()
    {
        var context = await GenerateArtifactsAsync("compliance-service", activeProfile: "uat");

        var composeViolations = EnvironmentNeutralRules.ScanComposeFile(context.ComposeContent!, DeployPackageLayout.ComposeFile);
        var envViolations = EnvironmentNeutralRules.ScanPackageArtifacts(context.EnvTemplateContent!, DeployPackageLayout.EnvTemplateFile);
        var configViolations = EnvironmentNeutralRules.ScanPackageArtifacts(context.ConfigurationTemplateJson!, DeployPackageLayout.ConfigTemplateFile);

        Assert.Empty(composeViolations);
        Assert.Empty(envViolations);
        Assert.Empty(configViolations);

        foreach (var network in EnvironmentNeutralRules.HardcodedNetworkNames)
        {
            Assert.DoesNotContain(network, context.ComposeContent, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ---------------------------------------------------------------
    // D6 — the same generated package artifacts are byte-identical no matter
    // which deployment profile is active on the publishing machine. Publishing
    // never bakes the publisher's own profile into the package.
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("demo")]
    [InlineData("uat")]
    [InlineData("qa")]
    [InlineData("production")]
    public async Task Generated_compose_and_templates_are_identical_regardless_of_publishing_machine_profile(string activeProfile)
    {
        var baseline = await GenerateArtifactsAsync("gateway", activeProfile: "demo");
        var candidate = await GenerateArtifactsAsync("gateway", activeProfile: activeProfile);

        Assert.Equal(baseline.ComposeContent, candidate.ComposeContent);
        Assert.Equal(baseline.EnvTemplateContent, candidate.EnvTemplateContent);
        Assert.Equal(baseline.ConfigurationTemplateJson, candidate.ConfigurationTemplateJson);
    }

    // ---------------------------------------------------------------
    // D5/D6 — resolved deployment profiles (Deployment's own concern) legitimately
    // differ in host paths and docker network naming across every supported
    // target environment, while every other setting stays identical. This proves
    // Local/Demo/QA/UAT/Production can all consume one package via profile swap.
    // ---------------------------------------------------------------

    [Fact]
    public void DeploymentProfiles_for_all_target_environments_resolve_and_only_differ_in_expected_values()
    {
        var report = EnvironmentComparer.Compare(ConfigRoot, ["demo", "uat", "qa", "production"]);

        Assert.True(report.IsConsistent, string.Join("; ", report.Violations.Select(v => v.Key)));

        var network = report.Variables.Single(variable => variable.Key == "AOVIAS_DOCKER_NETWORK");
        Assert.True(network.Differs, "QA is expected to use a distinct docker network from demo/uat/production.");

        var hostConfig = report.Variables.Single(variable => variable.Key == "AOVIAS_HOST_CONFIG_ROOT");
        Assert.True(hostConfig.ExpectedToDiffer);
        Assert.True(hostConfig.Differs, "QA host paths are expected to differ from the other profiles.");
    }

    [Fact]
    public void QaProfile_resolves_without_developer_or_personal_path_fragments()
    {
        var profile = DeploymentPathResolver.Resolve(ConfigRoot, "qa");

        Assert.Equal("qa", profile.Name);
        Assert.StartsWith("/", profile.ConfigRoot, StringComparison.Ordinal);
        Assert.StartsWith("/", profile.DeploymentsRoot, StringComparison.Ordinal);
        Assert.Empty(EnvironmentNeutralRules.ScanPackageArtifacts(profile.HostConfigRoot, "qa-profile", allowWindowsHostPaths: true));
        Assert.Empty(EnvironmentNeutralRules.ScanPackageArtifacts(profile.HostLogsRoot, "qa-profile", allowWindowsHostPaths: true));
    }

    // ---------------------------------------------------------------
    // D7 — whole-package regression gate: scanning every artifact together
    // (as DRV's EnvironmentNeutralValidationStage does) must find nothing, and
    // must reliably catch an injected violation (proves the gate is not inert).
    // ---------------------------------------------------------------

    [Fact]
    public async Task PackageArtifactScanner_finds_no_violations_across_full_generated_artifact_set()
    {
        var context = await GenerateArtifactsAsync("audit-service", activeProfile: "production");
        var directory = Path.Combine(Path.GetTempPath(), "aovias-m1-4-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, DeployPackageLayout.ComposeFile), context.ComposeContent);
            await File.WriteAllTextAsync(Path.Combine(directory, DeployPackageLayout.EnvTemplateFile), context.EnvTemplateContent);
            await File.WriteAllTextAsync(Path.Combine(directory, DeployPackageLayout.ConfigTemplateFile), context.ConfigurationTemplateJson);

            var violations = PackageArtifactScanner.ScanDirectory(directory);

            Assert.Empty(violations);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void PackageArtifactScanner_detects_an_injected_developer_path_violation()
    {
        var directory = Path.Combine(Path.GetTempPath(), "aovias-m1-4-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            File.WriteAllText(
                Path.Combine(directory, "manifest.json"),
                "{ \"configRoot\": \"V:/DevelopmentRepo/V1.0/aovias-config\" }");

            var violations = PackageArtifactScanner.ScanDirectory(directory);

            Assert.NotEmpty(violations);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static async Task<PublishingContext> GenerateArtifactsAsync(string serviceId, string activeProfile)
    {
        var projectRoot = EmptyProjectRoot;

        var manifest = new DeployManifest
        {
            Service = serviceId,
            Version = "1.0.0",
            Type = "AspNetCore",
            Container = serviceId,
            Health = "/health",
            EntryPoint = $"{serviceId}.dll",
            Ports = [8080],
            Dependencies = [],
            Configuration = DeployPackageLayout.ConfigTemplateFile,
            ImageName = $"aovias/{serviceId}",
            ImageTag = "1.0.0"
        };

        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = $"{serviceId}.csproj",
                ServiceId = serviceId,
                Version = "1.0.0",
                Profile = activeProfile
            },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = Path.Combine(projectRoot, $"{serviceId}.csproj"),
                ProjectRoot = projectRoot,
                ServiceId = serviceId,
                ServiceName = serviceId,
                ProviderName = "DotNet",
                ContainerPort = 8080,
                HealthEndpoint = "/health",
                EntryPoint = $"{serviceId}.dll"
            },
            DeploymentMetadata = new DeploymentMetadata
            {
                ServiceId = serviceId,
                Ports = new DeploymentPortMetadata { ContainerPort = 8080 },
                Health = new DeploymentHealthMetadata()
            },
            Manifest = manifest,
            DeploymentContracts = ServiceContractBuilder.Build(new ServiceContractBuildRequest
            {
                ServiceId = serviceId,
                DisplayName = serviceId,
                Version = "1.0.0",
                ImageReference = $"aovias/{serviceId}:1.0.0",
                ContainerName = serviceId,
                EntryPoint = $"{serviceId}.dll",
                HealthEndpoint = "/health",
                Ports = [8080]
            })
        };

        context.Health = new HealthDefinitionGenerator().Generate(context);
        await new ConfigTemplateGenerator().GenerateAsync(context, CancellationToken.None);
        await new EnvironmentFileGenerator().GenerateAsync(context, CancellationToken.None);
        await new ComposeGenerator().GenerateAsync(context, CancellationToken.None);

        return context;
    }
}
