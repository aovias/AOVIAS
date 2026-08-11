using Aovias.Configuration.Deployment;
using Aovias.Configuration.Extensions;
using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.Configuration.Tests.Deployment;

public sealed class DeploymentStorageOptionsConfiguratorTests
{
    [Fact]
    public void MissingDeploymentStorageFile_DerivesRootFromLegacyReleaseManagement()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:Runtime"] = "true",
                ["Features:ReleaseManagement"] = "true",
                [$"{ReleaseManagementSettings.SectionName}:ReleaseRoot"] = "/opt/aovias/releases",
                [$"{ReleaseManagementSettings.SectionName}:IncomingFolder"] = "/opt/aovias/releases/incoming",
                [$"{ReleaseManagementSettings.SectionName}:PackageRoot"] = "/opt/aovias/runtime/packages",
                [$"{ReleaseManagementSettings.SectionName}:TemporaryFolder"] = "/opt/aovias/releases/temp",
                [$"{ReleaseManagementSettings.SectionName}:ArchiveFolder"] = "/opt/aovias/releases/archive",
                [$"{ReleaseManagementSettings.SectionName}:StagingFolder"] = "/opt/aovias/releases/staging",
                [$"{ReleaseManagementSettings.SectionName}:ExtractionFolder"] = "/opt/aovias/releases/extraction",
                [$"{ReleaseManagementSettings.SectionName}:ActiveFolder"] = "/opt/aovias/releases/active",
                [$"{ReleaseManagementSettings.SectionName}:RollbackFolder"] = "/opt/aovias/releases/rollback",
                [$"{ReleaseManagementSettings.SectionName}:AuditFolder"] = "/opt/aovias/releases/audit",
                [$"{ReleaseManagementSettings.SectionName}:SupportedPackageVersion"] = "1.0",
                [$"{ReleaseManagementSettings.SectionName}:SupportedRuntimeVersion"] = "1.0.0",
                [$"{ReleaseManagementSettings.SectionName}:UploadEndpoint"] = "/api/releases/upload",
                [$"{ReleaseManagementSettings.SectionName}:ValidateEndpoint"] = "/api/releases/validate",
                [$"{ReleaseManagementSettings.SectionName}:PlanEndpoint"] = "/api/releases/plan",
                [$"{ReleaseManagementSettings.SectionName}:DeployEndpoint"] = "/api/releases/deploy",
                [$"{ReleaseManagementSettings.SectionName}:RollbackEndpoint"] = "/api/releases/rollback",
                [$"{ReleaseManagementSettings.SectionName}:ListEndpoint"] = "/api/releases",
                [$"{ReleaseManagementSettings.SectionName}:HistoryEndpoint"] = "/api/releases/history",
                [$"{ReleaseManagementSettings.SectionName}:DetailEndpointTemplate"] = "/api/releases/{id}",
                [$"{ReleaseManagementSettings.SectionName}:TimeoutSeconds"] = "120",
                [$"{ReleaseManagementSettings.SectionName}:RetryCount"] = "3",
                [$"{DeploymentHostSettings.SectionName}:HostConfigRoot"] = "/opt/aovias/runtime/config",
                [$"{DeploymentHostSettings.SectionName}:HostPackagesRoot"] = "/opt/aovias/runtime/packages",
                [$"{DeploymentHostSettings.SectionName}:HostLogsRoot"] = "/opt/aovias/runtime/logs",
                [$"{DeploymentHostSettings.SectionName}:ConfigRoot"] = "/opt/aovias/runtime/config",
                [$"{DeploymentHostSettings.SectionName}:PackagesRoot"] = "/opt/aovias/runtime/packages",
                [$"{DeploymentHostSettings.SectionName}:LogsRoot"] = "/opt/aovias/runtime/logs",
                [$"{DeploymentHostSettings.SectionName}:DotNetEnvironment"] = "Production",
                [$"{DeploymentHostSettings.SectionName}:DockerNetwork"] = "aovias-network",
                [$"{DeploymentSettings.SectionName}:Enabled"] = "true",
                [$"{DeploymentSettings.SectionName}:AgentVersion"] = "1.0.0",
                [$"{DeploymentSettings.SectionName}:PackageRoot"] = "/opt/aovias/runtime/packages",
                [$"{DeploymentSettings.SectionName}:HealthCheckBaseUrl"] = "http://127.0.0.1:7080",
                [$"{DeploymentSettings.SectionName}:HealthCheckTimeoutSeconds"] = "60",
                [$"{DeploymentSettings.SectionName}:HealthCheckRetryCount"] = "1",
                [$"{DeploymentSettings.SectionName}:HealthCheckRetryDelayMilliseconds"] = "1000",
                [$"{DeploymentSettings.SectionName}:ComposeProjectNamePrefix"] = "aovias",
                [$"{DeploymentSettings.SectionName}:ContainerHealthTimeoutSeconds"] = "60",
                [$"{DeploymentSettings.SectionName}:ContainerHealthPollIntervalMilliseconds"] = "1000",
                [$"{OrchestrationSettings.SectionName}:ExecutionTimeoutSeconds"] = "600",
                [$"{OrchestrationSettings.SectionName}:WorkflowTimeoutSeconds"] = "600",
                [$"{OrchestrationSettings.SectionName}:MaximumWorkers"] = "4",
                [$"{OrchestrationSettings.SectionName}:RetryCount"] = "1",
                [$"{OrchestrationSettings.SectionName}:RetryDelayMilliseconds"] = "1000",
                [$"{RemoteDeploymentSettings.SectionName}:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddRuntimeAgentConfiguration(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: false);

        var storage = provider.GetRequiredService<IOptions<DeploymentStorageSettings>>().Value;
        var resolver = provider.GetRequiredService<IDeploymentPathResolver>();

        Assert.Equal("/opt/aovias/deployments", storage.Root);
        Assert.Equal("/opt/aovias/deployments", resolver.DeploymentRoot);
    }
}
