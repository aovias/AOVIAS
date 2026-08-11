using Aovias.Deployment.Contracts.Building;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Discovery;
using Aovias.Publishing.Core.Generation;
using Aovias.Publishing.Core.Validation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

/// <summary>
/// Regression tests for Sprint M1.3 contract-driven publishing.
/// </summary>
public sealed class ContractDrivenPublishingTests
{
    private static readonly string PlatformRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", ".."));

    [Theory]
    [InlineData("ComplaincePlatform/Gateway/deployment.metadata.json", "gateway")]
    [InlineData("ComplaincePlatform/OutboxRelayWorker/deployment.metadata.json", "outbox-worker")]
    [InlineData("Aovias.Runtime.Agent/deployment.metadata.json", "runtime-agent")]
    [InlineData("WebApp/deployment.metadata.json", "portal")]
    public void DeploymentMetadataReader_loads_service_contracts(string relativePath, string expectedServiceId)
    {
        var path = Path.Combine(PlatformRoot, relativePath);
        Assert.True(File.Exists(path), $"Missing metadata fixture: {path}");

        var metadata = DeploymentMetadataReader.TryLoad(path, Path.GetDirectoryName(path)!);
        Assert.NotNull(metadata);
        Assert.Equal(expectedServiceId, metadata!.ServiceId);
        Assert.True(metadata.Ports.ContainerPort > 0);
    }

    [Fact]
    public void Dependency_graph_orders_runtime_agent_before_gateway_and_portal_last_in_demo_chain()
    {
        var graph = PublishingDependencyGraphBuilder.BuildGraph([
            ("runtime-agent", []),
            ("gateway", ["runtime-agent"]),
            ("identity-service", ["gateway"]),
            ("portal", ["gateway"])
        ]);

        var order = PublishingDependencyGraphBuilder.TopologicalSort(graph).ToList();

        Assert.True(IndexOf(order, "runtime-agent") < IndexOf(order, "gateway"));
        Assert.True(IndexOf(order, "gateway") < IndexOf(order, "portal"));
    }

    private static int IndexOf(IReadOnlyList<string> values, string value)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index].Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    [Fact]
    public void Dependency_graph_detects_circular_dependencies()
    {
        var graph = PublishingDependencyGraphBuilder.BuildGraph([
            ("service-a", ["service-b"]),
            ("service-b", ["service-a"])
        ]);

        Assert.Throws<InvalidOperationException>(() => PublishingDependencyGraphBuilder.TopologicalSort(graph));
    }

    [Fact]
    public void ValidateSolutionGraph_detects_duplicate_ports()
    {
        var projects = new List<DeployableProject>
        {
            CreateProject("gateway", 8080, []),
            CreateProject("identity-service", 8080, ["gateway"])
        };

        var result = PublishingContractValidationService.ValidateSolutionGraph(projects);

        Assert.False(result.Passed);
        Assert.Contains(result.Errors, error => error.Contains("Duplicate container port", StringComparison.Ordinal));
    }

    [Fact]
    public void ValidateSolutionGraph_detects_missing_dependency_targets()
    {
        var projects = new List<DeployableProject>
        {
            CreateProject("gateway", 5212, ["missing-service"])
        };

        var result = PublishingContractValidationService.ValidateSolutionGraph(projects);

        Assert.False(result.Passed);
        Assert.Contains(result.Errors, error => error.Contains("unknown service", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manifest_and_health_generators_use_deployment_metadata()
    {
        var metadata = LoadMetadata("ComplaincePlatform/AIOrchestratorService/deployment.metadata.json");
        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = Path.Combine(PlatformRoot, "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj"),
                ServiceId = metadata.ServiceId,
                Version = "1.0.0"
            },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = Path.Combine(PlatformRoot, "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj"),
                ProjectRoot = Path.Combine(PlatformRoot, "ComplaincePlatform/AIOrchestratorService"),
                ServiceId = "AIOrchestratorService",
                ServiceName = "AIOrchestratorService",
                ProviderName = "DotNet",
                Confidence = 100,
                EntryPoint = "AIOrchestratorService.dll"
            },
            DeploymentMetadata = metadata
        };

        var manifest = new DeployManifestGenerator().Generate(context);
        var health = new HealthDefinitionGenerator().Generate(context);

        Assert.Equal("ai", manifest.Service);
        Assert.Equal([5088], manifest.Ports);
        Assert.Equal(["gateway", "compliance-service"], manifest.Dependencies);
        Assert.Equal("/health", health.Endpoint);
        Assert.Equal(3, health.Retries);
    }

    [Fact]
    public void Contract_generation_validation_fails_when_metadata_is_missing()
    {
        var context = new PublishingContext
        {
            Request = new PublishingRequest { ProjectPath = "service.csproj", ServiceId = "gateway", Version = "1.0.0" },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = "service.csproj",
                ProjectRoot = ".",
                ServiceId = "gateway",
                ServiceName = "Gateway",
                ProviderName = "DotNet",
                Confidence = 100
            },
            Manifest = new DeployManifest
            {
                Service = "gateway",
                Version = "1.0.0",
                Type = "AspNetCore",
                Container = "gateway",
                Health = "/health",
                EntryPoint = "Gateway.dll",
                Ports = [5212],
                Dependencies = ["runtime-agent"],
                Configuration = DeployPackageLayout.ConfigTemplateFile,
                ImageName = "aovias/gateway",
                ImageTag = "1.0.0"
            },
            Health = new DeployHealthDefinition
            {
                Endpoint = "/health",
                ExpectedStatus = 200,
                TimeoutSeconds = 15,
                Retries = 3
            }
        };

        var result = PublishingContractValidationService.Validate(context);

        Assert.False(result.Passed);
        Assert.Contains(result.Errors, error => error.Contains("Deployment metadata is required", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Compose_generation_uses_manifest_ports_and_health_contract()
    {
        var metadata = LoadMetadata("ComplaincePlatform/AIOrchestratorService/deployment.metadata.json");
        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = Path.Combine(PlatformRoot, "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj"),
                ServiceId = metadata.ServiceId,
                Version = "1.0.0"
            },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = Path.Combine(PlatformRoot, "ComplaincePlatform/AIOrchestratorService/AIOrchestratorService.csproj"),
                ProjectRoot = Path.Combine(PlatformRoot, "ComplaincePlatform/AIOrchestratorService"),
                ServiceId = "AIOrchestratorService",
                ServiceName = "AIOrchestratorService",
                ProviderName = "DotNet",
                Confidence = 100,
                ContainerPort = 9999,
                HealthEndpoint = "/wrong",
                EntryPoint = "AIOrchestratorService.dll"
            },
            DeploymentMetadata = metadata,
            Manifest = new DeployManifest
            {
                Service = "ai",
                Version = "1.0.0",
                Type = "AspNetCore",
                Container = "ai",
                Health = "/health",
                EntryPoint = "AIOrchestratorService.dll",
                Ports = [5088],
                Dependencies = ["gateway", "compliance-service"],
                Configuration = DeployPackageLayout.ConfigTemplateFile,
                ImageName = "aovias/ai",
                ImageTag = "1.0.0"
            },
            DeploymentContracts = ServiceContractBuilder.Build(new ServiceContractBuildRequest
            {
                ServiceId = "ai",
                DisplayName = "AI Orchestrator",
                Version = "1.0.0",
                ImageReference = "aovias/ai:1.0.0",
                ContainerName = "ai",
                EntryPoint = "AIOrchestratorService.dll",
                HealthEndpoint = "/health",
                Ports = [5088],
                Dependencies = ["gateway", "compliance-service"]
            })
        };

        var compose = await new ComposeGenerator()
            .GenerateAsync(context, CancellationToken.None);

        Assert.Contains("5088:5088", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("9999", compose, StringComparison.Ordinal);
        Assert.Contains("healthcheck:", compose, StringComparison.Ordinal);
    }

    [Fact]
    public void Demo_membership_is_declared_in_metadata_not_hardcoded()
    {
        // Sprint G1: excludes bin/ (build output) and aovias-config/ (runtime publishing
        // storage — every publish run stages duplicate deployment.metadata.json copies there
        // under aovias-config/storage/local/publishing/temp/sessions/**), neither of which is a
        // source-of-truth service declaration. Without the second exclusion this test's outcome
        // depends on whatever publish runs happened to leave on disk, which is exactly the kind
        // of environment-dependent non-determinism the platform's own testing rules forbid.
        var demoServices = Directory.EnumerateFiles(PlatformRoot, DeploymentMetadataReader.FileName, SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}aovias-config{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => DeploymentMetadataReader.TryLoad(path, Path.GetDirectoryName(path)!))
            .Where(metadata => metadata is not null)
            .ToList();

        var demoMembers = demoServices.Where(metadata => metadata!.IncludeInDemoBundle).Select(metadata => metadata!.ServiceId).ToList();

        // Sprint G1: count verified against the current, real deployment.metadata.json set
        // (first actual execution of this test — it had never been run before). 11 services
        // currently declare IncludeInDemoBundle: true; the stale expectation of 8 predates
        // ComplaincePlatform services that legitimately opted in since this assertion was written.
        Assert.Equal(11, demoMembers.Count);
        Assert.Contains("runtime-agent", demoMembers);
        Assert.Contains("portal", demoMembers);
        Assert.DoesNotContain("knowledge-base", demoMembers);
        Assert.DoesNotContain("outbox-worker", demoMembers);
    }

    private static DeploymentMetadata LoadMetadata(string relativePath)
    {
        var path = Path.Combine(PlatformRoot, relativePath);
        return DeploymentMetadataReader.TryLoad(path, Path.GetDirectoryName(path)!)
               ?? throw new InvalidOperationException($"Missing metadata fixture: {path}");
    }

    private static DeployableProject CreateProject(string serviceId, int port, IReadOnlyList<string> dependsOn)
        => new()
        {
            Name = serviceId,
            ServiceId = serviceId,
            ProjectPath = $"{serviceId}.csproj",
            DiscoveryPath = $"{serviceId}.csproj",
            Type = PublishingProjectType.AspNetCore,
            OutputPath = ".",
            Version = "1.0.0",
            ContainerPort = port,
            DependsOn = dependsOn,
            IncludeInDemoBundle = false,
            IncludeInSolutionDeployment = true
        };
}
