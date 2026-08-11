using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.SignalR;

/// <summary>
/// Periodically broadcasts runtime health updates over SignalR.
/// </summary>
public sealed class RuntimeHealthBroadcastService : BackgroundService
{
    private readonly IHubContext<RuntimeHub> hubContext;
    private readonly IHealthProvider healthProvider;
    private readonly IOptionsMonitor<MonitoringSettings> monitoringSettings;
    private readonly IOptionsMonitor<SignalRSettings> signalRSettings;

    public RuntimeHealthBroadcastService(
        IHubContext<RuntimeHub> hubContext,
        IHealthProvider healthProvider,
        IOptionsMonitor<MonitoringSettings> monitoringSettings,
        IOptionsMonitor<SignalRSettings> signalRSettings)
    {
        this.hubContext = hubContext;
        this.healthProvider = healthProvider;
        this.monitoringSettings = monitoringSettings;
        this.signalRSettings = signalRSettings;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (signalRSettings.CurrentValue.Enabled && monitoringSettings.CurrentValue.Enabled)
            {
                var health = await healthProvider.GetHealthAsync(stoppingToken);
                await hubContext.Clients.All.SendAsync(
                    "RuntimeHealth",
                    new
                    {
                        Status = health.Status,
                        Message = health.Message,
                        Timestamp = DateTimeOffset.UtcNow
                    },
                    stoppingToken);
            }

            var interval = Math.Max(5, monitoringSettings.CurrentValue.CollectionIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }
}
