using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

/// <summary>
/// Resolves listening ports to owning process identifiers on the local machine.
/// </summary>
internal static class PortProcessResolver
{
    private static readonly Regex NetstatListeningRegex = new(
        @":(?<port>\d+)\s+[^\s]+\s+LISTENING\s+(?<pid>\d+)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsPortListening(int port)
    {
        try
        {
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(endpoint => endpoint.Port == port);
        }
        catch
        {
            return false;
        }
    }

    public static IReadOnlyList<int> GetListeningPorts(ServiceInfoModel service)
    {
        return new[] { service.HttpsPort, service.HttpPort, service.Port }
            .Where(port => port is not null)
            .Select(port => port!.Value)
            .Distinct()
            .ToList();
    }

    public static bool IsAnyPortListening(ServiceInfoModel service)
        => GetListeningPorts(service).Any(IsPortListening);

    public static int? TryGetListeningProcessId(int port)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano -p tcp",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            if (!process.Start())
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                var match = NetstatListeningRegex.Match(line.Trim());
                if (!match.Success || !int.TryParse(match.Groups["port"].Value, out var matchedPort) || matchedPort != port)
                {
                    continue;
                }

                if (int.TryParse(match.Groups["pid"].Value, out var pid) && pid > 0)
                {
                    return pid;
                }
            }
        }
        catch
        {
            // Fall back to "port is in use" without PID.
        }

        return null;
    }

    public static int? TryGetListeningProcessId(ServiceInfoModel service)
    {
        foreach (var port in GetListeningPorts(service))
        {
            if (TryGetListeningProcessId(port) is int pid)
            {
                return pid;
            }
        }

        return null;
    }

    public static void TryStopProcessTree(int processId, string source, ILoggingService logger)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            logger.Process(source, $"Stopping external PID {processId} ({process.ProcessName}).");
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
            logger.Process(source, "External process stopped.");
        }
        catch (ArgumentException)
        {
            logger.Warning(source, $"Process {processId} is no longer running.");
        }
        catch (Exception ex)
        {
            logger.Error(source, $"Unable to stop PID {processId}: {ex.Message}");
            throw;
        }
    }
}
