using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Monitoring;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Docker;

/// <summary>
/// Read-only Docker provider using Docker.DotNet.
/// </summary>
public sealed class DockerProvider : IDockerProvider
{
    private readonly IOptionsMonitor<DockerSettings> dockerSettings;
    private readonly ILogger<DockerProvider> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerProvider"/> class.
    /// </summary>
    /// <param name="dockerSettings">Docker settings.</param>
    /// <param name="logger">Logger instance.</param>
    public DockerProvider(IOptionsMonitor<DockerSettings> dockerSettings, ILogger<DockerProvider> logger)
    {
        this.dockerSettings = dockerSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<DockerOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = dockerSettings.CurrentValue;

        if (!settings.Enabled)
        {
            return new DockerOverviewDto
            {
                Status = "Disabled"
            };
        }

        try
        {
            using var client = CreateClient(settings);
            var version = await client.System.GetVersionAsync(cancellationToken);
            var containers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true }, cancellationToken);
            var images = await client.Images.ListImagesAsync(new ImagesListParameters(), cancellationToken);
            var networks = await client.Networks.ListNetworksAsync(new NetworksListParameters(), cancellationToken);
            var volumes = await client.Volumes.ListAsync(cancellationToken);

            return new DockerOverviewDto
            {
                DockerVersion = version.APIVersion ?? version.Version ?? string.Empty,
                ContainerCount = containers.Count,
                RunningContainerCount = containers.Count(container => string.Equals(container.State, "running", StringComparison.OrdinalIgnoreCase)),
                ImageCount = images.Count,
                NetworkCount = networks.Count,
                VolumeCount = volumes.Volumes?.Count ?? 0,
                Status = "Running"
            };
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to query Docker engine using configured endpoint.");
            return new DockerOverviewDto
            {
                Status = "Unavailable"
            };
        }
    }

    private static DockerClient CreateClient(DockerSettings settings)
    {
        var endpoint = !string.IsNullOrWhiteSpace(settings.Socket) ? settings.Socket : settings.Host;
        return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }
}
