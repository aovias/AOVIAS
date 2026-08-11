using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Discovery;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class DeployableProjectNamingTests
{
    [Theory]
    [InlineData("Gateway", "gateway")]
    [InlineData("ComplianceService", "compliance-service")]
    [InlineData("IdentityService", "identity-service")]
    public void ToKebabCase_normalizes_assembly_names(string input, string expected)
    {
        Assert.Equal(expected, DeployableProjectNaming.ToKebabCase(input));
    }

    [Fact]
    public void ToCanonicalServiceId_maps_angular_to_portal()
    {
        var discovery = new ProjectDiscoveryResult
        {
            ProjectType = PublishingProjectType.Angular,
            ProjectPath = "C:/repo/WebApp/angular.json",
            ProjectRoot = "C:/repo/WebApp",
            ServiceId = "WebApp",
            ServiceName = "WebApp",
            ProviderName = "Angular",
            Confidence = 100
        };

        Assert.Equal("portal", DeployableProjectNaming.ToCanonicalServiceId(discovery));
    }

    [Fact]
    public void ToCanonicalServiceId_maps_ai_orchestrator_to_ai()
    {
        var discovery = new ProjectDiscoveryResult
        {
            ProjectType = PublishingProjectType.AspNetCore,
            ProjectPath = "C:/repo/AIOrchestratorService/AIOrchestratorService.csproj",
            ProjectRoot = "C:/repo/AIOrchestratorService",
            ServiceId = "AIOrchestratorService",
            ServiceName = "AIOrchestratorService",
            ProviderName = "DotNet",
            Confidence = 100
        };

        Assert.Equal("ai", DeployableProjectNaming.ToCanonicalServiceId(discovery));
    }

    [Fact]
    public void ToCanonicalServiceId_maps_outbox_relay_worker_to_outbox_worker()
    {
        var discovery = new ProjectDiscoveryResult
        {
            ProjectType = PublishingProjectType.WorkerService,
            ProjectPath = "C:/repo/OutboxRelayWorker/OutboxRelayWorker.csproj",
            ProjectRoot = "C:/repo/OutboxRelayWorker",
            ServiceId = "OutboxRelayWorker",
            ServiceName = "OutboxRelayWorker",
            ProviderName = "DotNet",
            Confidence = 100
        };

        Assert.Equal("outbox-worker", DeployableProjectNaming.ToCanonicalServiceId(discovery));
    }
}
