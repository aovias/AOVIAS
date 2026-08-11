using Aovias.Deployment.Contracts.Building;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class ComposeGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_service_template_includes_drv_required_tokens()
    {
        var generator = new ComposeGenerator();
        var context = new PublishingContext
        {
            Request = new PublishingRequest
            {
                ProjectPath = @"V:\DevelopmentRepo\V1.0\ComplaincePlatform\AIOrchestratorService\AIOrchestratorService.csproj",
                ServiceId = "AIOrchestratorService",
                Version = "1.0.0"
            },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = @"V:\DevelopmentRepo\V1.0\ComplaincePlatform\AIOrchestratorService\AIOrchestratorService.csproj",
                ProjectRoot = @"V:\DevelopmentRepo\V1.0\ComplaincePlatform\AIOrchestratorService",
                ServiceId = "AIOrchestratorService",
                ServiceName = "AI Orchestrator",
                ProviderName = "DotNet",
                ContainerPort = 5088,
                HealthEndpoint = "/health",
                EntryPoint = "AIOrchestratorService.dll"
            },
            Manifest = new DeployManifest
            {
                Service = "AIOrchestratorService",
                Version = "1.0.0",
                Type = "AspNetCore",
                Container = "aiorchestratorservice",
                Health = "/health",
                EntryPoint = "AIOrchestratorService.dll",
                Ports = [5088],
                Dependencies = [],
                Configuration = DeployPackageLayout.ConfigTemplateFile,
                ImageName = "aiorchestratorservice",
                ImageTag = "1.0.0"
            },
            DeploymentContracts = ServiceContractBuilder.Build(new ServiceContractBuildRequest
            {
                ServiceId = "AIOrchestratorService",
                DisplayName = "AI Orchestrator",
                Version = "1.0.0",
                ImageReference = "aiorchestratorservice:1.0.0",
                ContainerName = "aiorchestratorservice",
                EntryPoint = "AIOrchestratorService.dll",
                HealthEndpoint = "/health",
                Ports = [5088]
            }),
            DeploymentMetadata = new DeploymentMetadata
            {
                ServiceId = "ai",
                Ports = new DeploymentPortMetadata { ContainerPort = 5088 },
                Health = new DeploymentHealthMetadata()
            }
        };

        var compose = await generator.GenerateAsync(context, CancellationToken.None);

        Assert.DoesNotContain("{{NETWORK_NAME}}", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("version:", compose, StringComparison.Ordinal);
        Assert.Contains("AOVIAS_CONFIG_ROOT", compose, StringComparison.Ordinal);
        Assert.Contains("volumes:", compose, StringComparison.Ordinal);
        Assert.Contains("restart:", compose, StringComparison.Ordinal);
        Assert.Contains("healthcheck:", compose, StringComparison.Ordinal);
        Assert.Contains("ports:", compose, StringComparison.Ordinal);
        Assert.Contains("${AOVIAS_DOCKER_NETWORK}", compose, StringComparison.Ordinal);
        Assert.Contains("5088:5088", compose, StringComparison.Ordinal);
    }
}
