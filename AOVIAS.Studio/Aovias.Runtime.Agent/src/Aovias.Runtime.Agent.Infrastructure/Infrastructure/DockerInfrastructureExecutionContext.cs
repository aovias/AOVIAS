using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Infrastructure;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Infrastructure;

/// <summary>
/// Runtime Agent implementation of host infrastructure actions.
/// </summary>
public sealed class DockerInfrastructureExecutionContext : IInfrastructureExecutionContext
{
    private static readonly Regex SqlServerHostPattern = new(
        @"(?:Server|Data Source)\s*=\s*(?<host>[^,;]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IOptionsMonitor<DockerSettings> dockerSettings;

    public DockerInfrastructureExecutionContext(IOptionsMonitor<DockerSettings> dockerSettings)
    {
        this.dockerSettings = dockerSettings;
    }

    public async Task<DockerEngineStatus> InspectDockerEngineAsync(CancellationToken cancellationToken = default)
    {
        if (!dockerSettings.CurrentValue.Enabled)
        {
            return new DockerEngineStatus
            {
                Available = false,
                ComposeAvailable = false,
                Error = "Docker provider is disabled."
            };
        }

        var settings = dockerSettings.CurrentValue;
        var endpoint = !string.IsNullOrWhiteSpace(settings.Socket) ? settings.Socket : settings.Host;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new DockerEngineStatus
            {
                Available = false,
                ComposeAvailable = false,
                Error = "Docker endpoint is not configured."
            };
        }

        try
        {
            using var client = new DockerClientConfiguration(new Uri(endpoint)).CreateClient();

            var version = await client.System.GetVersionAsync(cancellationToken);
            var composeAvailable = await IsComposeAvailableAsync(cancellationToken);
            return new DockerEngineStatus
            {
                Available = true,
                ComposeAvailable = composeAvailable,
                Version = version.APIVersion
            };
        }
        catch (Exception exception)
        {
            return new DockerEngineStatus
            {
                Available = false,
                ComposeAvailable = false,
                Error = exception.Message
            };
        }
    }

    public async Task EnsureDockerNetworkAsync(string networkName, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient()
            ?? throw new InvalidOperationException("Docker client is unavailable.");

        var networks = await client.Networks.ListNetworksAsync(new NetworksListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["name"] = new Dictionary<string, bool> { [networkName] = true }
            }
        }, cancellationToken);

        if (networks.Any(network => string.Equals(network.Name, networkName, StringComparison.Ordinal)))
        {
            return;
        }

        await client.Networks.CreateNetworkAsync(new NetworksCreateParameters
        {
            Name = networkName,
            Driver = "bridge",
            CheckDuplicate = true
        }, cancellationToken);
    }

    public async Task<bool> ImageExistsLocallyAsync(string imageReference, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        if (client is null)
        {
            return false;
        }

        var images = await client.Images.ListImagesAsync(new ImagesListParameters { All = true }, cancellationToken);
        return images.Any(image =>
            image.RepoTags?.Any(tag => string.Equals(tag, imageReference, StringComparison.OrdinalIgnoreCase)) == true);
    }

    public async Task<DatabaseReachabilityResult> ValidateDatabaseAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var host = ParseSqlHost(connectionString);
        if (string.IsNullOrWhiteSpace(host))
        {
            return DatabaseReachabilityResult.Failure("Unable to parse SQL Server host from connection string.");
        }

        if (string.Equals(host, ".", StringComparison.Ordinal)
            || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseReachabilityResult.Success();
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, 1433);
            var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            if (completed != connectTask)
            {
                return DatabaseReachabilityResult.Failure($"Timed out connecting to SQL Server host '{host}:1433'.");
            }

            await connectTask;
            return DatabaseReachabilityResult.Success();
        }
        catch (Exception exception)
        {
            return DatabaseReachabilityResult.Failure(exception.Message);
        }
    }

    public async Task<MessageBusReachabilityResult> ValidateMessageBusAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var host = ParseMessageBusHost(connectionString);
        var port = ParseMessageBusPort(connectionString) ?? 5672;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return MessageBusReachabilityResult.Success();
        }

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            if (completed != connectTask)
            {
                return MessageBusReachabilityResult.Failure($"Timed out connecting to RabbitMQ host '{host}:{port}'.");
            }

            await connectTask;
            return MessageBusReachabilityResult.Success();
        }
        catch (Exception exception)
        {
            return MessageBusReachabilityResult.Failure(exception.Message);
        }
    }

    private static string ParseMessageBusHost(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        var hostMatch = Regex.Match(connectionString, @"Host\s*=\s*(?<host>[^;]+)", RegexOptions.IgnoreCase);
        return hostMatch.Success ? hostMatch.Groups["host"].Value.Trim() : connectionString;
    }

    private static int? ParseMessageBusPort(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && uri.Port > 0)
        {
            return uri.Port;
        }

        var portMatch = Regex.Match(connectionString, @"Port\s*=\s*(?<port>\d+)", RegexOptions.IgnoreCase);
        return portMatch.Success ? int.Parse(portMatch.Groups["port"].Value) : null;
    }

    private DockerClient? CreateClient()
    {
        var settings = dockerSettings.CurrentValue;
        if (!settings.Enabled)
        {
            return null;
        }

        var endpoint = !string.IsNullOrWhiteSpace(settings.Socket) ? settings.Socket : settings.Host;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return null;
        }

        return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }

    private static async Task<bool> IsComposeAvailableAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "compose version",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        await process.WaitForExitAsync(cancellationToken);
        return process.ExitCode == 0;
    }

    private static string? ParseSqlHost(string connectionString)
    {
        var match = SqlServerHostPattern.Match(connectionString);
        return match.Success ? match.Groups["host"].Value.Trim() : null;
    }
}
