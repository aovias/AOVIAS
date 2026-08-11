using System.Text.RegularExpressions;
using Aovias.Bootstrap.Contracts;
using ComplianceServiceManager.Models.Bootstrap;
using Renci.SshNet;

namespace ComplianceServiceManager.Services.Bootstrap;

/// <summary>
/// Real SSH-based discovery of a target VPS's facts (Ubuntu version, hostname, CPU/memory/disk,
/// public IP, Docker, existing AOVIAS runtime). This is new CSM-side code, not a Bootstrap-engine
/// capability — the engine's own preflight only checks pass/fail "is this Ubuntu," so this exists
/// purely to give the wizard's Target Host step something honest to show instead of fabricating
/// discovery data. It never provisions anything; every command here is read-only.
/// </summary>
public sealed class TargetHostProbeService
{
    private const string ExistingRuntimeConfigRoot = "/opt/aovias/config";
    private const int RuntimeAgentDefaultPort = 7080;

    public async Task<(bool Success, string? Error)> TestConnectionAsync(
        string host,
        int port,
        string username,
        string? privateKeyPath,
        string? password,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SshClient(BuildConnectionInfo(host, port, username, privateKeyPath, password));
            await Task.Run(client.Connect, cancellationToken);
            var connected = client.IsConnected;
            client.Disconnect();
            return (connected, connected ? null : "Connection did not report success.");
        }
        catch (Exception exception)
        {
            return (false, exception.Message);
        }
    }

    public async Task<TargetHostFacts> DiscoverFactsAsync(
        string host,
        int port,
        string username,
        string? privateKeyPath,
        string? password,
        CancellationToken cancellationToken)
    {
        using var client = new SshClient(BuildConnectionInfo(host, port, username, privateKeyPath, password));
        await Task.Run(client.Connect, cancellationToken);
        try
        {
            var osRelease = RunCommand(client, "cat /etc/os-release");
            var hostname = RunCommand(client, "hostname").Trim();
            var cpuModel = RunCommand(client, "grep 'model name' /proc/cpuinfo | head -1 | cut -d: -f2").Trim();
            var cpuCount = RunCommand(client, "nproc").Trim();
            var memory = RunCommand(client, "free -h | awk '/^Mem:/ {print $2 \" total, \" $3 \" used\"}'").Trim();
            var disk = RunCommand(client, "df -h / | awk 'NR==2 {print $2 \" total, \" $3 \" used (\" $5 \" full)\"}'").Trim();
            var publicIp = RunCommand(client, "curl -s -m 5 ifconfig.me || curl -s -m 5 icanhazip.com").Trim();
            var dockerVersionRaw = RunCommand(client, "docker --version 2>/dev/null").Trim();
            var dockerComposeVersionRaw = RunCommand(client, "docker compose version 2>/dev/null").Trim();
            var existingRuntime = RunCommand(client, $"test -d {ExistingRuntimeConfigRoot} && echo present").Trim();

            var ubuntuVersion = ParseOsReleaseField(osRelease, "PRETTY_NAME") ?? "Unknown";
            var dockerInstalled = !string.IsNullOrWhiteSpace(dockerVersionRaw);
            var dockerComposeInstalled = !string.IsNullOrWhiteSpace(dockerComposeVersionRaw);
            var existingRuntimeDetected = existingRuntime.Contains("present", StringComparison.Ordinal);

            string? existingRuntimePlatformVersion = null;
            if (existingRuntimeDetected)
            {
                // Best-effort only: the agent's own heartbeat, curled from the target host itself
                // (loopback), not fabricated. A running-but-unreachable-on-loopback agent still
                // counts as "existing runtime detected" — it just won't have a version to show.
                var heartbeatJson = RunCommand(client, $"curl -s -m 3 http://localhost:{RuntimeAgentDefaultPort}/api/deployment/heartbeat").Trim();
                existingRuntimePlatformVersion = ParseJsonStringField(heartbeatJson, "runtimeVersion") ?? ParseJsonStringField(heartbeatJson, "platformVersion");
            }

            var privilegeMode = DetectPrivilegeMode(client, username);

            return new TargetHostFacts
            {
                UbuntuVersion = ubuntuVersion,
                Hostname = string.IsNullOrWhiteSpace(hostname) ? "Unknown" : hostname,
                CpuSummary = string.IsNullOrWhiteSpace(cpuCount)
                    ? "Unknown"
                    : $"{cpuCount} vCPU{(cpuCount == "1" ? string.Empty : "s")}{(string.IsNullOrWhiteSpace(cpuModel) ? string.Empty : $" — {cpuModel}")}",
                MemorySummary = string.IsNullOrWhiteSpace(memory) ? "Unknown" : memory,
                DiskSummary = string.IsNullOrWhiteSpace(disk) ? "Unknown" : disk,
                PublicIp = string.IsNullOrWhiteSpace(publicIp) ? "Unknown" : publicIp,
                DockerInstalled = dockerInstalled,
                DockerVersion = dockerInstalled ? dockerVersionRaw : "Not installed",
                DockerComposeVersion = dockerComposeInstalled ? dockerComposeVersionRaw : "Not installed",
                ExistingRuntimeDetected = existingRuntimeDetected,
                ExistingRuntimeConfigRoot = existingRuntimeDetected ? ExistingRuntimeConfigRoot : null,
                ExistingRuntimePlatformVersion = existingRuntimePlatformVersion,
                PrivilegeMode = privilegeMode
            };
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }

    /// <summary>
    /// Mirrors <c>BootstrapOrchestrator.DetectPrivilegeModeAsync</c> in spirit (same three read-only
    /// checks: root? passwordless sudo? sudo/wheel group membership?) so the Target Host step can
    /// show the operator an honest "will this need a password" hint before they even continue past
    /// Step 0 — the engine's own Preflight phase remains the single authoritative check that
    /// actually gates provisioning; this is read-only, display-only, and never provisions anything.
    /// </summary>
    private static BootstrapPrivilegeMode DetectPrivilegeMode(SshClient client, string username)
    {
        if (string.Equals(username, "root", StringComparison.Ordinal))
        {
            return BootstrapPrivilegeMode.NotRequired;
        }

        var passwordlessCheck = RunCommand(client, "sudo -n true; echo $?").Trim();
        if (passwordlessCheck == "0")
        {
            return BootstrapPrivilegeMode.Passwordless;
        }

        var groups = RunCommand(client, "id -nG");
        var canElevate = groups.Contains("sudo", StringComparison.OrdinalIgnoreCase) || groups.Contains("wheel", StringComparison.OrdinalIgnoreCase);
        return canElevate ? BootstrapPrivilegeMode.PasswordRequired : BootstrapPrivilegeMode.Unavailable;
    }

    private static string RunCommand(SshClient client, string command)
    {
        try
        {
            using var sshCommand = client.CreateCommand(command);
            return sshCommand.Execute() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? ParseOsReleaseField(string osRelease, string key)
    {
        var match = Regex.Match(osRelease, $"^{key}=\"?([^\"\n]+)\"?", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    /// <summary>Minimal, dependency-free extraction of one string field from a small JSON payload — avoids pulling in a full JSON parse for a single best-effort read of a heartbeat response that may not even be present.</summary>
    private static string? ParseJsonStringField(string json, string fieldName)
    {
        var match = Regex.Match(json, $"\"{fieldName}\"\\s*:\\s*\"([^\"]*)\"");
        return match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value) ? match.Groups[1].Value : null;
    }

    private static ConnectionInfo BuildConnectionInfo(string host, int port, string username, string? privateKeyPath, string? password)
    {
        AuthenticationMethod authentication = !string.IsNullOrWhiteSpace(privateKeyPath)
            ? new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile(privateKeyPath))
            : new PasswordAuthenticationMethod(username, password ?? string.Empty);

        return new ConnectionInfo(host, port, username, authentication);
    }
}
