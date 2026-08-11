using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Xunit;

namespace Aovias.DeploymentValidation.Tests.RemoteDeployment;

public sealed class PlatformDeploymentPlannerTests
{
    [Fact]
    public void OrderServices_respects_startup_order_and_dependencies()
    {
        var manifest = new PlatformDeploymentManifest
        {
            Profile = "demo",
            Version = "1.0.0",
            StartupOrder = ["runtime-agent", "gateway", "identity", "compliance"],
            Services =
            [
                Service("runtime-agent", 1, []),
                Service("gateway", 2, ["runtime-agent"]),
                Service("identity", 3, ["gateway"]),
                Service("compliance", 4, ["identity", "gateway"])
            ]
        };

        var ordered = PlatformDeploymentPlanner.OrderServices(manifest);

        Assert.Equal(
            ["runtime-agent", "gateway", "identity", "compliance"],
            ordered.Select(service => service.ServiceId));
    }

    [Fact]
    public void OrderServices_filters_selected_services()
    {
        var manifest = new PlatformDeploymentManifest
        {
            Profile = "demo",
            Version = "1.0.0",
            StartupOrder = ["gateway", "identity"],
            Services =
            [
                Service("gateway", 1, []),
                Service("identity", 2, ["gateway"]),
                Service("compliance", 3, ["identity"])
            ]
        };

        var ordered = PlatformDeploymentPlanner.OrderServices(manifest, ["identity", "gateway"]);

        Assert.Equal(["gateway", "identity"], ordered.Select(service => service.ServiceId));
    }

    [Fact]
    public void Service_mode_gateway_never_includes_runtime_agent()
    {
        var manifest = CreateFullManifest();
        var mode = PlatformDeploymentScope.InferMode(["gateway"]);
        Assert.Equal(PlatformDeploymentMode.Service, mode);

        var effective = PlatformDeploymentScope.ResolveEffectiveServiceIds(manifest, ["gateway"], mode);
        Assert.Equal(["gateway"], effective);

        var ordered = PlatformDeploymentPlanner.OrderServices(manifest, effective);
        Assert.Equal(["gateway"], ordered.Select(service => service.ServiceId));
        Assert.DoesNotContain(ordered, service => service.ServiceId == "runtime-agent");
    }

    [Fact]
    public void Application_mode_expands_non_infrastructure_dependencies()
    {
        var manifest = CreateFullManifest();
        var mode = PlatformDeploymentScope.InferMode(["portal"]);
        Assert.Equal(PlatformDeploymentMode.Service, mode);

        var effective = PlatformDeploymentScope.ResolveEffectiveServiceIds(manifest, ["portal"], mode);
        Assert.Contains("portal", effective);
        Assert.Contains("gateway", effective);
        Assert.Contains("identity", effective);
        Assert.DoesNotContain("runtime-agent", effective);

        var required = PlatformDeploymentScope.GetRequiredApplicationDependencies(manifest, ["portal"]);
        Assert.Contains("gateway", required);
        Assert.Contains("identity", required);
        Assert.DoesNotContain("runtime-agent", required);
    }

    [Fact]
    public void Platform_mode_includes_runtime_agent()
    {
        var manifest = CreateFullManifest();
        var effective = PlatformDeploymentScope.ResolveEffectiveServiceIds(
            manifest,
            selectedServiceIds: null,
            PlatformDeploymentMode.Platform);

        Assert.Contains("runtime-agent", effective);
        Assert.Contains("gateway", effective);
    }

    private static PlatformDeploymentManifest CreateFullManifest()
        => new()
        {
            Profile = "demo",
            Version = "1.0.0",
            StartupOrder = ["runtime-agent", "gateway", "identity", "portal"],
            Services =
            [
                Service("runtime-agent", 1, []),
                Service("gateway", 2, ["runtime-agent"]),
                Service("identity", 3, ["gateway"]),
                Service("portal", 4, ["gateway", "identity"])
            ]
        };

    private static PlatformDeploymentServiceEntry Service(
        string serviceId,
        int startupOrder,
        IReadOnlyList<string> dependsOn)
        => new()
        {
            ServiceId = serviceId,
            Name = serviceId,
            Version = "1.0.0",
            StartupOrder = startupOrder,
            DependsOn = dependsOn,
            HealthEndpoint = "/health",
            DockerImage = $"{serviceId}:1.0.0",
            PackageFileName = $"{serviceId}.aovdeploy",
            DeploymentReady = true
        };
}
