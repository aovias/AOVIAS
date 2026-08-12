using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ComplianceServiceManager.Models.PortRegistry;

namespace ComplianceServiceManager.Services.PortRegistry;

/// <summary>
/// Windows/.NET local TCP availability using bind + listener inspection.
/// Never kills processes. Never claims permanent availability.
/// </summary>
public sealed class WindowsPortAvailabilityService : IPortAvailabilityService
{
    public PortOccupancyObservation Observe(int port)
    {
        if (port is <= 0 or > 65535)
        {
            return new PortOccupancyObservation
            {
                Port = port,
                IsListening = false,
                Error = "Port must be between 1 and 65535."
            };
        }

        try
        {
            var listening = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(endpoint => endpoint.Port == port);

            if (!listening)
            {
                // Double-check with ephemeral bind (point-in-time).
                try
                {
                    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
                    socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                    return new PortOccupancyObservation
                    {
                        Port = port,
                        IsListening = false
                    };
                }
                catch (SocketException)
                {
                    listening = true;
                }
            }

            if (!listening)
            {
                return new PortOccupancyObservation { Port = port, IsListening = false };
            }

            var pid = TryGetListeningProcessId(port);
            string? name = null;
            if (pid is int processId)
            {
                try
                {
                    using var process = Process.GetProcessById(processId);
                    name = process.ProcessName;
                }
                catch
                {
                    name = null;
                }
            }

            return new PortOccupancyObservation
            {
                Port = port,
                IsListening = true,
                ProcessId = pid,
                ProcessName = name
            };
        }
        catch (Exception ex)
        {
            return new PortOccupancyObservation
            {
                Port = port,
                IsListening = false,
                Error = ex.Message
            };
        }
    }

    public int? FindFirstAvailablePort(int startInclusive, int endInclusive, IEnumerable<int>? excludedPorts = null)
    {
        if (startInclusive < 1 || endInclusive > 65535 || startInclusive > endInclusive)
        {
            return null;
        }

        var excluded = new HashSet<int>(excludedPorts ?? []);
        for (var port = startInclusive; port <= endInclusive; port++)
        {
            if (excluded.Contains(port))
            {
                continue;
            }

            var observation = Observe(port);
            if (!observation.IsListening && string.IsNullOrWhiteSpace(observation.Error))
            {
                return port;
            }
        }

        return null;
    }

    private static int? TryGetListeningProcessId(int port)
    {
        // Reuse existing Studio netstat-based resolver without process kill APIs.
        return PortProcessResolver.TryGetListeningProcessId(port);
    }
}
