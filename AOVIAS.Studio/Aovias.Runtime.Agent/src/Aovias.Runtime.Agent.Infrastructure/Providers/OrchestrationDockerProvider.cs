using Aovias.Configuration.Settings;
using Aovias.Orchestration.Contracts.Providers;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Providers;

/// <summary>
/// Docker provider for orchestration workflows using Docker.DotNet.
/// </summary>
public sealed class OrchestrationDockerProvider : IDockerProvider
{
    private readonly IOptionsMonitor<DockerSettings> dockerSettings;
    private readonly ILogger<OrchestrationDockerProvider> logger;

    public OrchestrationDockerProvider(IOptionsMonitor<DockerSettings> dockerSettings, ILogger<OrchestrationDockerProvider> logger)
    {
        this.dockerSettings = dockerSettings;
        this.logger = logger;
    }

    /// <inheritdoc />
    public string ProviderName => "Docker";

    /// <inheritdoc />
    public bool IsAvailable => dockerSettings.CurrentValue.Enabled;

    /// <inheritdoc />
    public async Task<DockerContainerInfo?> InspectContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        if (client is null)
        {
            return null;
        }

        var inspect = await client.Containers.InspectContainerAsync(containerIdOrName, cancellationToken);
        return Map(inspect);
    }

    /// <inheritdoc />
    public async Task StartContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default)
    {
        using var client = RequireClient();
        await client.Containers.StartContainerAsync(containerIdOrName, new ContainerStartParameters(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default)
    {
        using var client = RequireClient();
        await client.Containers.StopContainerAsync(containerIdOrName, new ContainerStopParameters(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task RestartContainerAsync(string containerIdOrName, CancellationToken cancellationToken = default)
    {
        using var client = RequireClient();
        await client.Containers.RestartContainerAsync(containerIdOrName, new ContainerRestartParameters(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GetLogsAsync(string containerIdOrName, int tailLines, CancellationToken cancellationToken = default)
    {
        using var client = RequireClient();
        var parameters = new ContainerLogsParameters
        {
            ShowStdout = true,
            ShowStderr = true,
            Tail = tailLines.ToString()
        };

        using var multiplexed = await client.Containers.GetContainerLogsAsync(containerIdOrName, false, parameters, cancellationToken);
        var (stdout, stderr) = await multiplexed.ReadOutputToEndAsync(cancellationToken);
        return string.Concat(stdout, stderr);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DockerContainerInfo>> ListContainersAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        if (client is null)
        {
            return [];
        }

        var containers = await client.Containers.ListContainersAsync(new ContainersListParameters { All = true }, cancellationToken);
        return containers.Select(container => new DockerContainerInfo
        {
            Id = container.ID,
            Name = container.Names.FirstOrDefault()?.TrimStart('/') ?? container.ID,
            Image = container.Image,
            State = container.State,
            Status = container.Status,
            Health = container.Status
        }).ToList();
    }

    /// <inheritdoc />
    public Task ComposeUpAsync(string composeFilePath, string projectName, CancellationToken cancellationToken = default)
        => RunComposeAsync(composeFilePath, projectName, "up -d --remove-orphans", cancellationToken);

    /// <inheritdoc />
    public Task ComposeDownAsync(string composeFilePath, string projectName, CancellationToken cancellationToken = default)
        => RunComposeAsync(composeFilePath, projectName, "down", cancellationToken);

    /// <inheritdoc />
    public async Task<DockerContainerInfo?> WaitForContainerHealthAsync(
        string containerIdOrName,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var container = await InspectContainerAsync(containerIdOrName, cancellationToken);
            if (container is not null &&
                (string.Equals(container.Health, "healthy", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(container.State, "running", StringComparison.OrdinalIgnoreCase) && container.Health is null))
            {
                return container;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        return await InspectContainerAsync(containerIdOrName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DockerNetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        if (client is null)
        {
            return [];
        }

        var networks = await client.Networks.ListNetworksAsync(cancellationToken: cancellationToken);
        return networks.Select(network => new DockerNetworkInfo
        {
            Id = network.ID,
            Name = network.Name,
            Driver = network.Driver
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DockerVolumeInfo>> ListVolumesAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        if (client is null)
        {
            return [];
        }

        var response = await client.Volumes.ListAsync(cancellationToken);
        return (response.Volumes ?? []).Select(volume => new DockerVolumeInfo
        {
            Name = volume.Name,
            Driver = volume.Driver,
            Mountpoint = volume.Mountpoint
        }).ToList();
    }

    /// <inheritdoc />
    public async Task EnsureNetworkExistsAsync(string networkName, CancellationToken cancellationToken = default)
    {
        using var client = RequireClient();
        var existing = await client.Networks.ListNetworksAsync(cancellationToken: cancellationToken);
        if (existing.Any(network => string.Equals(network.Name, networkName, StringComparison.OrdinalIgnoreCase)))
        {
            return; // Already exists — idempotent no-op (Sprint M2.6 D6).
        }

        await client.Networks.CreateNetworkAsync(new NetworksCreateParameters { Name = networkName }, cancellationToken);
    }

    private async Task RunComposeAsync(
        string composeFilePath,
        string projectName,
        string arguments,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(composeFilePath))
        {
            throw new FileNotFoundException("Docker compose file was not found.", composeFilePath);
        }

        var composeDirectory = Path.GetDirectoryName(composeFilePath)
            ?? throw new InvalidOperationException("Compose file directory could not be resolved.");

        var startInfo = new global::System.Diagnostics.ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f \"{composeFilePath}\" -p {projectName} {arguments}",
            WorkingDirectory = composeDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = global::System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start docker compose process.");

        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            logger.LogError("Docker compose failed: {StdErr}", stderr);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? stdout : stderr);
        }

        logger.LogInformation("Docker compose completed: {Output}", stdout);
    }

    private DockerClient RequireClient()
        => CreateClient() ?? throw new InvalidOperationException("Docker provider is unavailable.");

    private DockerClient? CreateClient()
    {
        var settings = dockerSettings.CurrentValue;
        if (!settings.Enabled)
        {
            return null;
        }

        try
        {
            var endpoint = !string.IsNullOrWhiteSpace(settings.Socket) ? settings.Socket : settings.Host;
            return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Unable to create Docker client.");
            return null;
        }
    }

    private static DockerContainerInfo Map(ContainerInspectResponse inspect)
        => new()
        {
            Id = inspect.ID,
            Name = inspect.Name.TrimStart('/'),
            Image = inspect.Config.Image,
            State = inspect.State.Status,
            Status = inspect.State.Status,
            Health = inspect.State.Health?.Status
        };
}
