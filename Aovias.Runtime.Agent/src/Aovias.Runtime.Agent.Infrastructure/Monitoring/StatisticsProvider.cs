using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Monitoring;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Monitoring;

/// <summary>
/// Aggregates live system and Docker statistics for monitoring endpoints.
/// </summary>
public sealed class StatisticsProvider : IStatisticsProvider
{
    private readonly ISystemInformationProvider systemInformationProvider;
    private readonly IDockerProvider dockerProvider;
    private readonly IOptionsMonitor<RuntimeSettings> runtimeSettings;
    private readonly IOptionsMonitor<ApiSettings> apiSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsProvider"/> class.
    /// </summary>
    public StatisticsProvider(
        ISystemInformationProvider systemInformationProvider,
        IDockerProvider dockerProvider,
        IOptionsMonitor<RuntimeSettings> runtimeSettings,
        IOptionsMonitor<ApiSettings> apiSettings)
    {
        this.systemInformationProvider = systemInformationProvider;
        this.dockerProvider = dockerProvider;
        this.runtimeSettings = runtimeSettings;
        this.apiSettings = apiSettings;
    }

    /// <inheritdoc />
    public async Task<RuntimeStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await systemInformationProvider.GetSystemMetricsAsync(cancellationToken);
        var docker = await dockerProvider.GetOverviewAsync(cancellationToken);
        var runtime = runtimeSettings.CurrentValue;
        var api = apiSettings.CurrentValue;

        return new RuntimeStatisticsDto
        {
            CpuUsagePercent = metrics.CpuUsagePercent,
            MemoryUsagePercent = metrics.MemoryUsagePercent,
            DiskUsagePercent = metrics.DiskUsagePercent,
            CollectedAt = DateTimeOffset.UtcNow,
            OperatingSystem = metrics.OperatingSystem,
            Kernel = metrics.Kernel,
            Hostname = metrics.MachineName,
            Environment = runtime.Environment,
            PublicUrl = api.PublicUrl,
            Docker = docker
        };
    }
}
