using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Runtime;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Runtime;

/// <summary>
/// Composes runtime discovery data from configuration and live system providers.
/// </summary>
public sealed class RuntimeProvider : IRuntimeProvider
{
    private readonly IRuntimeManifestProvider manifestProvider;
    private readonly ISystemInformationProvider systemInformationProvider;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IOptionsMonitor<InfrastructureSettings> infrastructureSettings;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeProvider"/> class.
    /// </summary>
    public RuntimeProvider(
        IRuntimeManifestProvider manifestProvider,
        ISystemInformationProvider systemInformationProvider,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IOptionsMonitor<InfrastructureSettings> infrastructureSettings,
        IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.manifestProvider = manifestProvider;
        this.systemInformationProvider = systemInformationProvider;
        this.runtimeSettings = runtimeSettings;
        this.infrastructureSettings = infrastructureSettings;
        this.apiSettings = apiSettings;
    }

    /// <inheritdoc />
    public async Task<RuntimeInfoDto> GetRuntimeInfoAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await manifestProvider.GetManifestAsync(cancellationToken);
        var metrics = await systemInformationProvider.GetSystemMetricsAsync(cancellationToken);
        var runtime = runtimeSettings.CurrentValue;
        var infrastructure = infrastructureSettings.CurrentValue;
        var api = apiSettings.CurrentValue;

        return new RuntimeInfoDto
        {
            Name = runtime.AgentDisplayName,
            Server = manifest.ServerName,
            Hostname = metrics.MachineName,
            OperatingSystem = metrics.OperatingSystem,
            Kernel = metrics.Kernel,
            PublicIp = infrastructure.PublicIp,
            SslEnabled = infrastructure.SslEnabled,
            HttpsEndpoint = api.PublicUrl,
            DockerNetwork = manifest.DockerNetwork
        };
    }

    /// <inheritdoc />
    public async Task<RuntimeEnvironmentDto> GetRuntimeEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await manifestProvider.GetManifestAsync(cancellationToken);
        var metrics = await systemInformationProvider.GetSystemMetricsAsync(cancellationToken);

        return new RuntimeEnvironmentDto
        {
            Name = manifest.Environment,
            ServerName = manifest.ServerName,
            ServerId = manifest.ServerId,
            Hostname = metrics.MachineName,
            DockerNetwork = manifest.DockerNetwork
        };
    }

    /// <inheritdoc />
    public async Task<RuntimeVersionDto> GetRuntimeVersionAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await manifestProvider.GetManifestAsync(cancellationToken);

        return new RuntimeVersionDto
        {
            RuntimeVersion = manifest.RuntimeVersion,
            PlatformVersion = manifest.PlatformVersion,
            DeploymentAgentVersion = manifest.DeploymentAgentVersion
        };
    }
}
