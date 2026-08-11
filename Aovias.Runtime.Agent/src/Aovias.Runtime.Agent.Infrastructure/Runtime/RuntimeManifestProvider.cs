using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Domain.Entities;
using Aovias.Runtime.Agent.Infrastructure.Persistence;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Aovias.Runtime.Contracts.Runtime;
using Aovias.Runtime.Contracts.Services;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Runtime;

/// <summary>
/// Reads the runtime manifest from the configured manifest path.
/// </summary>
public sealed class RuntimeManifestProvider : IRuntimeManifestProvider
{
    private readonly RuntimeManifestStore manifestStore;
    private readonly RuntimeCapabilitiesProvider capabilitiesProvider;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IOptionsMonitor<NetworkSettings> networkSettings;
    private readonly IOptionsMonitor<DeploymentSettings> deploymentSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeManifestProvider"/> class.
    /// </summary>
    public RuntimeManifestProvider(
        RuntimeManifestStore manifestStore,
        RuntimeCapabilitiesProvider capabilitiesProvider,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IOptionsMonitor<NetworkSettings> networkSettings,
        IOptionsMonitor<DeploymentSettings> deploymentSettings)
    {
        this.manifestStore = manifestStore;
        this.capabilitiesProvider = capabilitiesProvider;
        this.runtimeSettings = runtimeSettings;
        this.networkSettings = networkSettings;
        this.deploymentSettings = deploymentSettings;
    }

    /// <inheritdoc />
    public async Task<RuntimeManifest> GetManifestAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var persisted = await manifestStore.TryLoadAsync(cancellationToken);
        var runtime = runtimeSettings.CurrentValue;
        var network = networkSettings.CurrentValue;
        var deployment = deploymentSettings.CurrentValue;
        var entity = persisted ?? BuildFromConfiguration(runtime, network, deployment);
        entity.LastUpdated = DateTimeOffset.UtcNow;
        var capabilities = capabilitiesProvider.GetCapabilities(entity.RuntimeVersion);
        return Map(entity, capabilities);
    }

    /// <inheritdoc />
    public async Task PersistSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await manifestStore.TryLoadAsync(cancellationToken);
        var runtime = runtimeSettings.CurrentValue;
        var network = networkSettings.CurrentValue;
        var deployment = deploymentSettings.CurrentValue;
        var entity = persisted ?? BuildFromConfiguration(runtime, network, deployment);
        entity.LastUpdated = DateTimeOffset.UtcNow;
        await manifestStore.SaveAsync(entity, cancellationToken);
    }

    private static RuntimeManifestEntity BuildFromConfiguration(
        RuntimeSettings runtime,
        NetworkSettings network,
        DeploymentSettings deployment)
    {
        return new RuntimeManifestEntity
        {
            RuntimeVersion = runtime.RuntimeVersion,
            Environment = runtime.Environment,
            Hostname = runtime.Hostname,
            ServerName = runtime.ServerName,
            ServerId = runtime.ServerId,
            PlatformVersion = runtime.PlatformVersion,
            DeploymentAgentVersion = deployment.AgentVersion,
            DockerNetwork = network.DockerNetwork,
            StartedAt = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    private static RuntimeManifest Map(RuntimeManifestEntity entity, Aovias.Orchestration.Contracts.Capabilities.RuntimeCapabilities capabilities)
    {
        return new RuntimeManifest
        {
            RuntimeVersion = entity.RuntimeVersion,
            Environment = entity.Environment,
            Hostname = entity.Hostname,
            ServerName = entity.ServerName,
            ServerId = entity.ServerId,
            PlatformVersion = entity.PlatformVersion,
            DeploymentAgentVersion = entity.DeploymentAgentVersion,
            DockerNetwork = entity.DockerNetwork,
            StartedAt = entity.StartedAt,
            LastUpdated = entity.LastUpdated,
            Capabilities = capabilities.Capabilities,
            InfrastructureServices = entity.InfrastructureServices
                .Select(service => new InfrastructureServiceDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Category = service.Category,
                    Version = service.Version,
                    Status = service.Status,
                    Health = service.Health
                })
                .ToList(),
            ApplicationServices = entity.ApplicationServices
                .Select(service => new ApplicationServiceDto
                {
                    Id = service.Id,
                    Name = service.Name,
                    Category = service.Category,
                    Version = service.Version,
                    Status = service.Status,
                    Health = service.Health
                })
                .ToList()
        };
    }
}
