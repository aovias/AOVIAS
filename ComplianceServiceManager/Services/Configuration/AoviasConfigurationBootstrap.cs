using Aovias.Configuration.Extensions;
using Aovias.Configuration.Loading;
using Aovias.Configuration.Profiles;
using Aovias.Configuration.Settings;
using Aovias.Runtime.Contracts.Health;
using Aovias.Runtime.Contracts.Monitoring;
using Aovias.Runtime.Contracts.Runtime;
using Aovias.Runtime.Contracts.Services;
using Aovias.PackageBuilder;
using Aovias.Publishing.SDK;
using Aovias.Release.SDK;
using Aovias.Runtime.SDK;
using ComplianceServiceManager.Contracts.Deployment;
using ComplianceServiceManager.Contracts.Observability;
using ComplianceServiceManager.Services.Deployment;
using ComplianceServiceManager.Contracts.Runtime;
using ComplianceServiceManager.Models.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace ComplianceServiceManager.Services.Configuration;

/// <summary>
/// Builds the merged AOVIAS configuration for the Operations Console host.
/// </summary>
public static class AoviasConfigurationBootstrap
{
    /// <summary>
    /// Creates the merged configuration pipeline for the WPF host.
    /// </summary>
    /// <returns>The merged configuration.</returns>
    public static IConfiguration Build()
    {
        var bootstrap = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "AOVIAS_")
            .Build();

        return Aovias.Configuration.Extensions.ServiceCollectionExtensions.BuildAoviasConfiguration(
            bootstrap,
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production",
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
    }

    /// <summary>
    /// Registers AOVIAS platform services for the Operations Console.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The merged configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAoviasPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.AddLogging(logging => logging.AddDebug());
        services.AddAoviasConfiguration(configuration);
        services.AddRuntimeAgentClient(configuration);
        services.AddReleaseClient(configuration);
        services.AddAoviasPublishing();
        services.AddPackageBuilder();
        services.AddSingleton<IRuntimeService, RuntimeAgentService>();
        services.AddSingleton<IObservabilityService, ObservabilityAgentService>();
        services.AddSingleton<IDeploymentRequestTraceService, DeploymentRequestTraceService>();
        services.AddSingleton<Aovias.Release.SDK.IReleaseClientTraceHook, ReleaseClientTraceBridge>();
        services.AddSingleton<IDeploymentService, DeploymentService>();
        services.AddSingleton<IEnvironmentManager, EnvironmentManagerService>();
        services.AddHttpClient("SelectedRuntimeAgent");
        services.AddSingleton<IRemoteAgentClientFactory, RemoteAgentClientFactory>();
        services.AddSingleton<IRemoteDeploymentOrchestrator, RemoteDeploymentOrchestrator>();
        services.AddSingleton<ComplianceServiceManager.Contracts.Release.IReleaseCatalogService, ComplianceServiceManager.Services.Release.ReleaseCatalogService>();
        return services;
    }
}

/// <summary>
/// Runtime service adapter backed by the AOVIAS Runtime SDK.
/// </summary>
public sealed class RuntimeAgentService : IRuntimeService
{
    private readonly IRuntimeAgentClient runtimeAgentClient;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeAgentService"/> class.
    /// </summary>
    public RuntimeAgentService(IRuntimeAgentClient runtimeAgentClient, IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.runtimeAgentClient = runtimeAgentClient;
        this.apiSettings = apiSettings;
    }

    /// <inheritdoc />
    public async Task<RuntimeInfo> GetRuntimeAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await runtimeAgentClient.GetRuntimeAsync(cancellationToken);
        var info = await runtimeAgentClient.GetRuntimeInfoAsync(cancellationToken);
        var version = await runtimeAgentClient.GetRuntimeVersionAsync(cancellationToken);

        return new RuntimeInfo
        {
            Name = info.Name,
            Environment = manifest.Environment,
            Server = info.Server,
            Version = version.RuntimeVersion,
            Status = "Healthy",
            Health = RuntimeHealth.Healthy,
            LastUpdated = manifest.LastUpdated,
            Hostname = info.Hostname,
            DockerNetwork = info.DockerNetwork,
            OperatingSystem = info.OperatingSystem,
            Kernel = info.Kernel,
            PublicIp = info.PublicIp,
            SslEnabled = info.SslEnabled,
            HttpsEndpoint = info.HttpsEndpoint
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeService>> GetRuntimeServicesAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await runtimeAgentClient.GetRuntimeAsync(cancellationToken);
        var services = manifest.InfrastructureServices
            .Concat(manifest.ApplicationServices.Select(MapApplicationService))
            .Select(MapService)
            .ToList();

        return services;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RuntimeEvent>> GetRuntimeEventsAsync(CancellationToken cancellationToken = default)
    {
        var health = await runtimeAgentClient.GetRuntimeHealthAsync(cancellationToken);
        var statistics = await runtimeAgentClient.GetRuntimeStatisticsAsync(cancellationToken);
        var now = DateTimeOffset.Now;

        return
        [
            new RuntimeEvent { Timestamp = statistics.CollectedAt, Level = "Info", Message = $"Docker status: {statistics.Docker.Status}" },
            new RuntimeEvent { Timestamp = now.AddMinutes(-1), Level = "Info", Message = $"Environment: {statistics.Environment}" },
            new RuntimeEvent { Timestamp = health.CheckedAt, Level = "Info", Message = health.Message },
            new RuntimeEvent { Timestamp = now.AddMinutes(-5), Level = "Info", Message = $"Connected to {apiSettings.CurrentValue.PublicUrl}" }
        ];
    }

    /// <inheritdoc />
    public async Task<RuntimeStatistics> GetRuntimeStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = await runtimeAgentClient.GetRuntimeStatisticsAsync(cancellationToken);
        var health = await runtimeAgentClient.GetRuntimeHealthAsync(cancellationToken);
        var docker = statistics.Docker;

        return new RuntimeStatistics
        {
            CpuUsagePercent = statistics.CpuUsagePercent,
            MemoryUsagePercent = statistics.MemoryUsagePercent,
            DiskUsagePercent = statistics.DiskUsagePercent,
            DockerEngineStatus = docker.Status,
            DockerEngineHealth = MapHealth(docker.Status),
            NginxStatus = FindComponent(health, "Nginx")?.Status ?? "Unknown",
            NginxHealth = MapHealth(FindComponent(health, "Nginx")?.Health ?? "Unknown"),
            SqlServerStatus = FindComponent(health, "SQL Server")?.Status ?? "Unknown",
            SqlServerHealth = MapHealth(FindComponent(health, "SQL Server")?.Health ?? "Unknown"),
            CertificatesStatus = FindComponent(health, "Certificates")?.Status ?? "Unknown",
            CertificatesHealth = MapHealth(FindComponent(health, "Certificates")?.Health ?? "Unknown"),
            DeploymentAgentStatus = FindComponent(health, "Deployment Agent")?.Status ?? "Unknown",
            DeploymentAgentHealth = MapHealth(FindComponent(health, "Deployment Agent")?.Health ?? "Unknown"),
            DockerVersion = docker.DockerVersion,
            ContainerCount = docker.ContainerCount,
            RunningContainerCount = docker.RunningContainerCount,
            NetworkCount = docker.NetworkCount,
            VolumeCount = docker.VolumeCount,
            ImageCount = docker.ImageCount,
            OperatingSystem = statistics.OperatingSystem,
            Kernel = statistics.Kernel,
            Hostname = statistics.Hostname,
            Environment = statistics.Environment,
            PublicUrl = statistics.PublicUrl
        };
    }

    private static RuntimeComponentHealthDto? FindComponent(RuntimeHealthDto health, string name)
        => health.Components.FirstOrDefault(component => component.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static RuntimeService MapService(InfrastructureServiceDto service)
    {
        return new RuntimeService
        {
            Id = service.Id,
            Name = service.Name,
            Category = service.Category,
            Version = service.Version,
            Status = service.Status,
            Health = MapHealth(service.Health),
            Started = DateTimeOffset.UtcNow,
            Uptime = service.Status
        };
    }

    private static InfrastructureServiceDto MapApplicationService(ApplicationServiceDto service)
    {
        return new InfrastructureServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Category = service.Category,
            Version = service.Version,
            Status = service.Status,
            Health = service.Health
        };
    }

    private static RuntimeHealth MapHealth(string value)
    {
        return value.ToUpperInvariant() switch
        {
            "HEALTHY" or "RUNNING" or "VALID" => RuntimeHealth.Healthy,
            "DEGRADED" or "WARNING" => RuntimeHealth.Degraded,
            "NOT INSTALLED" or "UNKNOWN" or "UNAVAILABLE" or "DISABLED" => RuntimeHealth.NotInstalled,
            "STOPPED" => RuntimeHealth.Stopped,
            _ => RuntimeHealth.Unknown
        };
    }
}
