using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Agent.Infrastructure.Observability.Collectors;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Observability.Collectors;

/// <summary>
/// Subscribes to Docker engine events and publishes observability platform events.
/// </summary>
public sealed class DockerEventListenerService : BackgroundService
{
    private readonly IRuntimeEventBus eventBus;
    private readonly DockerStateCollector dockerStateCollector;
    private readonly IOptionsMonitor<ObservabilitySettings> observabilitySettings;
    private readonly IOptionsMonitor<DockerSettings> dockerSettings;
    private readonly ILogger<DockerEventListenerService> logger;

    public DockerEventListenerService(
        IRuntimeEventBus eventBus,
        DockerStateCollector dockerStateCollector,
        IOptionsMonitor<ObservabilitySettings> observabilitySettings,
        IOptionsMonitor<DockerSettings> dockerSettings,
        ILogger<DockerEventListenerService> logger)
    {
        this.eventBus = eventBus;
        this.dockerStateCollector = dockerStateCollector;
        this.observabilitySettings = observabilitySettings;
        this.dockerSettings = dockerSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!observabilitySettings.CurrentValue.Enabled ||
            !observabilitySettings.CurrentValue.DockerEventListenerEnabled ||
            !dockerSettings.CurrentValue.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var client = CreateClient(dockerSettings.CurrentValue);
                await client.System.MonitorEventsAsync(
                    new ContainerEventsParameters(),
                    new Progress<Message>(message => _ = HandleDockerEventAsync(message, stoppingToken)),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Docker event listener disconnected. Reconnecting...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleDockerEventAsync(Message dockerEvent, CancellationToken cancellationToken)
    {
        var eventType = MapEventType(dockerEvent);
        if (eventType is null)
        {
            return;
        }

        string? containerName = null;
        if (dockerEvent.Actor?.Attributes is { } attributes)
        {
            attributes.TryGetValue("name", out containerName);
        }

        await eventBus.PublishAsync(new RuntimePlatformEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            Timestamp = DateTimeOffset.UtcNow,
            Category = RuntimeEventCategory.Docker,
            EventType = eventType,
            Message = $"Docker {dockerEvent.Type} {dockerEvent.Action} {containerName}".Trim(),
            ContainerName = containerName,
            Metadata = new Dictionary<string, string>
            {
                ["dockerType"] = dockerEvent.Type ?? string.Empty,
                ["dockerAction"] = dockerEvent.Action ?? string.Empty,
                ["dockerId"] = dockerEvent.Actor?.ID ?? string.Empty
            }
        }, cancellationToken);

        _ = dockerStateCollector.CollectAsync(cancellationToken);
    }

    private static string? MapEventType(Message dockerEvent)
    {
        if (string.Equals(dockerEvent.Type, "container", StringComparison.OrdinalIgnoreCase))
        {
            return (dockerEvent.Action ?? string.Empty).ToLowerInvariant() switch
            {
                "start" => "ContainerStarted",
                "stop" => "ContainerStopped",
                "die" => "ContainerStopped",
                "restart" => "ContainerRestarted",
                "health_status" => "HealthChanged",
                _ => "ContainerEvent"
            };
        }

        if (string.Equals(dockerEvent.Type, "network", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(dockerEvent.Action, "create", StringComparison.OrdinalIgnoreCase))
        {
            return "NetworkCreated";
        }

        if (string.Equals(dockerEvent.Type, "volume", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(dockerEvent.Action, "create", StringComparison.OrdinalIgnoreCase))
        {
            return "VolumeCreated";
        }

        if (string.Equals(dockerEvent.Type, "image", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(dockerEvent.Action, "pull", StringComparison.OrdinalIgnoreCase))
        {
            return "ImagePulled";
        }

        return null;
    }

    private static DockerClient CreateClient(DockerSettings settings)
    {
        var endpoint = !string.IsNullOrWhiteSpace(settings.Socket) ? settings.Socket : settings.Host;
        return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }
}
