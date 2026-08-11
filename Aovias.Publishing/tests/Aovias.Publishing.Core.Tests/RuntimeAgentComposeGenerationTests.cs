using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Validation;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class RuntimeAgentComposeGenerationTests
{
    [Fact]
    public async Task GenerateAsync_runtime_agent_includes_deployment_storage_bind_mount()
    {
        var contracts = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "runtime-agent",
            DisplayName = "Runtime Agent",
            Version = "1.0.0",
            ImageReference = "aovias/runtime-agent:1.0.0",
            ContainerName = "runtime-agent",
            EntryPoint = "Aovias.Runtime.Agent.Api.dll",
            HealthEndpoint = "/health",
            Ports = [7080]
        });

        var generator = new ComposeGenerator();
        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = LocateRuntimeAgentProject(),
                ServiceId = "runtime-agent",
                Version = "1.0.0"
            },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = LocateRuntimeAgentProject(),
                ProjectRoot = Path.GetDirectoryName(LocateRuntimeAgentProject())!,
                ServiceId = "runtime-agent",
                ServiceName = "Runtime Agent",
                ProviderName = "DotNet",
                ContainerPort = 7080,
                HealthEndpoint = "/health",
                EntryPoint = "Aovias.Runtime.Agent.Api.dll"
            },
            Manifest = new DeployManifest
            {
                Service = "runtime-agent",
                Version = "1.0.0",
                Type = "AspNetCore",
                Container = "runtime-agent",
                Health = "/health",
                EntryPoint = "Aovias.Runtime.Agent.Api.dll",
                Ports = [7080],
                Dependencies = [],
                Configuration = DeployPackageLayout.ConfigTemplateFile,
                ImageName = "runtime-agent",
                ImageTag = "1.0.0"
            },
            DeploymentContracts = contracts
        };

        var compose = await generator.GenerateAsync(context, CancellationToken.None);
        var validation = ComposeMountValidator.Validate(compose, contracts.Volume, contracts.Filesystem);

        Assert.Contains(
            $"{PlatformDeploymentDefaults.HostDeploymentsRootToken}:{PlatformDeploymentDefaults.DeploymentRoot}",
            compose,
            StringComparison.Ordinal);
        Assert.True(validation.Passed, string.Join("; ", validation.Errors));
        Assert.Contains(validation.Checks, check => check.Label == "Deployment Root" && check.Status == "PASS");
    }

    [Fact]
    public async Task GenerateAsync_without_contracts_fails_before_compose_is_accepted()
    {
        var generator = new ComposeGenerator();
        var context = new PublishingContext
        {
            Request = new PublishingRequest { ProjectPath = "runtime-agent", ServiceId = "runtime-agent" },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = "runtime-agent",
                ProjectRoot = ".",
                ServiceId = "runtime-agent",
                ServiceName = "Runtime Agent",
                ProviderName = "DotNet",
                ContainerPort = 7080,
                HealthEndpoint = "/health"
            },
            Manifest = new DeployManifest
            {
                Service = "runtime-agent",
                Version = "1.0.0",
                Type = "AspNetCore",
                Container = "runtime-agent",
                Health = "/health",
                EntryPoint = "Aovias.Runtime.Agent.Api.dll",
                Ports = [7080],
                Dependencies = [],
                Configuration = DeployPackageLayout.ConfigTemplateFile,
                ImageName = "runtime-agent",
                ImageTag = "1.0.0"
            }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => generator.GenerateAsync(context, CancellationToken.None));

        Assert.Contains("Deployment contracts are required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string LocateRuntimeAgentProject()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "Aovias.Runtime.Agent",
                "src",
                "Aovias.Runtime.Agent.Api",
                "Aovias.Runtime.Agent.Api.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Aovias.Runtime.Agent.Api.csproj.");
    }
}
