using Aovias.Configuration.Extensions;
using Aovias.Configuration.Features;
using Aovias.Configuration.Secrets;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.Configuration.Tests;

public sealed class ModularConfigurationTests
{
    [Fact]
    public void AddRuntimeAgentConfiguration_DoesNotRegisterDisabledPublishingOptions()
    {
        var configuration = BuildRuntimeOnlyConfiguration(includeExplicitFeatures: true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRuntimeAgentConfiguration(configuration);

        Assert.False(IsOptionsRegistered<PublishingSettings>(services));
        Assert.False(IsOptionsRegistered<MonitoringSettings>(services));
        Assert.False(IsOptionsRegistered<CertificateSettings>(services));
        Assert.False(IsOptionsRegistered<ObservabilitySettings>(services));
        Assert.False(IsOptionsRegistered<SecretsSettings>(services));
    }

    [Fact]
    public void AddRuntimeAgentConfiguration_RegistersCoreRuntimeOptions()
    {
        var configuration = BuildRuntimeOnlyConfiguration(includeExplicitFeatures: true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRuntimeAgentConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IOptions<RuntimeSettings>>().Value;

        Assert.Equal("1.0.0", runtime.RuntimeVersion);
    }

    [Fact]
    public void ResolveRuntimeAgent_DoesNotInferOptionalModulesFromProfileContent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{PublishingSettings.SectionName}:PublishRoot"] = "/opt/aovias/publishing/publish",
                [$"{MonitoringSettings.SectionName}:Enabled"] = "true",
                [$"{ProviderSettings.SectionName}:CertificateEnabled"] = "true",
                [$"{ObservabilitySettings.SectionName}:Enabled"] = "true",
                [$"{ProviderSettings.SectionName}:DockerEnabled"] = "true",
                [$"{SignalRSettings.SectionName}:Enabled"] = "true",
                [$"{DeploymentSettings.SectionName}:Enabled"] = "true"
            })
            .Build();

        var features = PlatformFeatureActivation.ResolveRuntimeAgent(configuration);

        Assert.True(features.Runtime);
        Assert.True(features.DockerProvider);
        Assert.True(features.SignalR);
        Assert.True(features.ReleaseManagement);
        Assert.False(features.Publishing);
        Assert.False(features.Monitoring);
        Assert.False(features.Certificates);
        Assert.False(features.Observability);
        Assert.False(features.RequiresSecretsValidation);
    }

    [Fact]
    public void HostStartup_SucceedsForRuntimeOnlyConfigurationWithoutOptionalModules()
    {
        var configuration = BuildRuntimeOnlyConfiguration(includeExplicitFeatures: true);
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        builder.Services.AddSingleton<IConfiguration>(configuration);
        builder.Services.AddRuntimeAgentConfiguration(configuration);

        using var host = builder.Build();
    }

    private static bool IsOptionsRegistered<TOptions>(IServiceCollection services)
        where TOptions : class
        => services.Any(descriptor =>
            descriptor.ServiceType == typeof(IConfigureOptions<TOptions>)
            || descriptor.ServiceType == typeof(IValidateOptions<TOptions>));

    private static IConfiguration BuildRuntimeOnlyConfiguration(bool includeExplicitFeatures)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Runtime:Environment"] = "Demo",
            ["Runtime:ServerName"] = "demo",
            ["Runtime:Hostname"] = "demo.example.com",
            ["Runtime:PlatformVersion"] = "2026.1",
            ["Runtime:RuntimeVersion"] = "1.0.0",
            ["Runtime:ServerId"] = "srv-demo",
            ["Runtime:AgentDisplayName"] = "Runtime Agent",
            ["Storage:DataRoot"] = "/data",
            ["Storage:BackupRoot"] = "/backup",
            ["Storage:TempRoot"] = "/temp",
            ["RuntimeManifest:ManifestPath"] = "/manifest",
            ["Logging:LogRoot"] = "/logs",
            ["Api:BaseUrl"] = "https://demo.example.com/runtime",
            ["Api:PublicUrl"] = "https://demo.example.com/runtime",
            ["Api:InternalUrl"] = "http://127.0.0.1:7080",
            ["Api:HealthEndpoint"] = "/health",
            ["Api:TimeoutSeconds"] = "30",
            ["Api:RetryCount"] = "1",
            ["Infrastructure:PublicIp"] = "127.0.0.1",
            ["Infrastructure:Domain"] = "demo.example.com",
            ["Infrastructure:HttpsPort"] = "443",
            ["Infrastructure:HttpPort"] = "80",
            ["Security:JwtIssuer"] = "aovias",
            ["Security:JwtAudience"] = "runtime",
            ["ReleaseManagement:ReleaseRoot"] = "/releases",
            ["ReleaseManagement:PackageRoot"] = "/packages",
            ["ReleaseManagement:TemporaryFolder"] = "/temp",
            ["ReleaseManagement:ArchiveFolder"] = "/archive",
            ["ReleaseManagement:IncomingFolder"] = "/incoming",
            ["ReleaseManagement:StagingFolder"] = "/staging",
            ["ReleaseManagement:ExtractionFolder"] = "/extraction",
            ["ReleaseManagement:ActiveFolder"] = "/active",
            ["ReleaseManagement:RollbackFolder"] = "/rollback",
            ["ReleaseManagement:AuditFolder"] = "/audit",
            ["ReleaseManagement:SupportedPackageVersion"] = "1.0",
            ["ReleaseManagement:SupportedRuntimeVersion"] = "1.0.0",
            ["ReleaseManagement:UploadEndpoint"] = "/upload",
            ["ReleaseManagement:ValidateEndpoint"] = "/validate",
            ["ReleaseManagement:PlanEndpoint"] = "/plan",
            ["ReleaseManagement:DeployEndpoint"] = "/deploy",
            ["ReleaseManagement:RollbackEndpoint"] = "/rollback",
            ["ReleaseManagement:ListEndpoint"] = "/list",
            ["ReleaseManagement:HistoryEndpoint"] = "/history",
            ["ReleaseManagement:DetailEndpointTemplate"] = "/detail/{id}",
            ["ReleaseManagement:TimeoutSeconds"] = "120",
            ["ReleaseManagement:RetryCount"] = "1",
            ["Deployment:Enabled"] = "true",
            ["Deployment:AgentVersion"] = "1.0.0",
            ["Deployment:PackageRoot"] = "/packages",
            ["Deployment:HealthCheckBaseUrl"] = "https://demo.example.com/runtime",
            ["Deployment:HealthCheckTimeoutSeconds"] = "60",
            ["Deployment:HealthCheckRetryCount"] = "1",
            ["Deployment:HealthCheckRetryDelayMilliseconds"] = "1000",
            ["Deployment:ComposeProjectNamePrefix"] = "aovias",
            ["Deployment:ContainerHealthTimeoutSeconds"] = "60",
            ["Deployment:ContainerHealthPollIntervalMilliseconds"] = "1000",
            ["Orchestration:ExecutionTimeoutSeconds"] = "600",
            ["Orchestration:WorkflowTimeoutSeconds"] = "600",
            ["Orchestration:MaximumWorkers"] = "4",
            ["Orchestration:RetryCount"] = "1",
            ["Orchestration:RetryDelayMilliseconds"] = "1000",
            ["Docker:Enabled"] = "true",
            ["Docker:Host"] = "unix:///var/run/docker.sock",
            ["Docker:Socket"] = "unix:///var/run/docker.sock",
            ["Docker:ApiVersion"] = "1.41",
            ["Docker:Network"] = "aovias-network",
            ["Docker:ComposeRoot"] = "/compose",
            ["Docker:VolumesRoot"] = "/volumes",
            ["Network:DockerNetwork"] = "aovias-network",
            ["Providers:DockerEnabled"] = "true",
            ["Providers:RuntimeEndpoint"] = "/api/runtime",
            ["SignalR:Enabled"] = "true",
            ["SignalR:HubPath"] = "/hubs/deployment",
            ["SignalR:KeepAliveSeconds"] = "15",
            ["SignalR:ClientTimeoutSeconds"] = "30",
            [$"{PublishingSettings.SectionName}:PublishRoot"] = "/invalid/incomplete/publishing",
            [$"{MonitoringSettings.SectionName}:Enabled"] = "true",
            [$"{CertificateSettings.SectionName}:StoreLocation"] = string.Empty,
            [$"{ObservabilitySettings.SectionName}:Enabled"] = "true"
        };

        if (includeExplicitFeatures)
        {
            values["Features:Runtime"] = "true";
            values["Features:ReleaseManagement"] = "true";
            values["Features:DockerProvider"] = "true";
            values["Features:SignalR"] = "true";
            values["Features:Publishing"] = "false";
            values["Features:Certificates"] = "false";
            values["Features:Monitoring"] = "false";
            values["Features:Observability"] = "false";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
