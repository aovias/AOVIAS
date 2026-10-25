using System.Text;
using System.Text.Json;
using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core;

/// <summary>A parsed <c>docker inspect</c> reading of the Runtime Agent container's state.</summary>
public sealed record ContainerState(string Status, string Health, int ExitCode, int RestartCount)
{
    public bool IsRunningAndHealthy =>
        string.Equals(Status, "running", StringComparison.OrdinalIgnoreCase)
        && (string.Equals(Health, "healthy", StringComparison.OrdinalIgnoreCase) || string.Equals(Health, "none", StringComparison.OrdinalIgnoreCase));
}

/// <summary>The outcome of waiting for the Runtime Agent container to become healthy.</summary>
public sealed record ContainerHealthWait(bool Healthy, bool IsCrashLoop, string Reason);

/// <summary>
/// Sprint M7.x — the Docker Manager. Every Docker interaction Bootstrap performs (install, daemon
/// verification/restart, network management, image load, compose up, container state inspection,
/// health waiting, and diagnostics collection) goes through this class; stages never build a
/// <c>docker ...</c> command string themselves. All daemon-touching commands are privileged — a
/// non-docker-group user gets "permission denied" talking to the daemon socket on *any* docker
/// subcommand, not just writes, so leaving reads unprivileged would misread "denied" as "absent".
/// </summary>
public sealed class DockerManager(RemoteExecutionEngine engine)
{
    /// <summary>Matches the container name observed in production (<c>docker ps -a</c> reports <c>NAMES: runtime-agent</c>) and the compose project's own <c>container_name</c>/service convention.</summary>
    public const string RuntimeAgentContainerName = "runtime-agent";

    /// <summary>The compose project name every Runtime Agent start/ps command addresses.</summary>
    public const string RuntimeAgentComposeProject = "aovias-runtime-agent";

    /// <summary>Unprivileged on purpose: `docker --version` only reads the client binary, and its absence is the signal DockerInstallation's discovery needs.</summary>
    public Task<RemoteCommandResult> GetCliVersionAsync(CancellationToken cancellationToken)
        => engine.RunAsync("docker --version", cancellationToken);

    public Task<RemoteCommandResult> InstallAsync(BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
        => engine.RunPrivilegedAsync("sh -c \"curl -fsSL https://get.docker.com | sh && systemctl enable --now docker\"", phase, observer, cancellationToken);

    /// <summary>An installed CLI is not a working engine — this asks the daemon itself to answer.</summary>
    public Task<RemoteCommandResult> VerifyDaemonAsync(BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
        => engine.RunPrivilegedAsync("docker info --format \"{{.ServerVersion}}\"", phase, observer, cancellationToken);

    /// <summary>Self-healing repair for a daemon that stopped answering (Sprint M7.x "restart docker before failing").</summary>
    public Task<RemoteCommandResult> RestartDaemonAsync(BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
        => engine.RunPrivilegedAsync("systemctl restart docker", phase, observer, cancellationToken);

    public async Task<bool> NetworkExistsAsync(string networkName, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var inspect = await engine.RunPrivilegedAsync($"docker network inspect {networkName}", phase, observer, cancellationToken);
        return inspect.Succeeded;
    }

    public Task<RemoteCommandResult> CreateNetworkAsync(string networkName, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
        => engine.RunPrivilegedAsync($"docker network create {networkName}", phase, observer, cancellationToken);

    /// <summary>
    /// Sprint M7.x.2 — the Docker network diagnostics engine, collected unconditionally whenever
    /// network creation/verification fails so the operator sees the actual cause instead of an
    /// "Unhandled exception": daemon status (<c>docker info</c>, full output — not just the
    /// version string <see cref="VerifyDaemonAsync"/> checks), every existing network
    /// (<c>docker network ls</c> — surfaces a name/subnet conflict directly), the Docker socket's
    /// own permissions (a non-docker-group user's every daemon command fails identically, and this
    /// makes that visible instead of inferred), whether the daemon is running rootless (changes
    /// where the socket and its permissions actually live), and the identity Bootstrap is actually
    /// running commands as. Best-effort throughout — a diagnostic command failing to run is
    /// reported as such, never allowed to mask the real failure it is attached to.
    /// </summary>
    public async Task<string> CollectNetworkDiagnosticsAsync(string networkName, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var info = await engine.RunPrivilegedAsync("docker info", phase, observer, cancellationToken);
        var networkList = await engine.RunPrivilegedAsync("docker network ls", phase, observer, cancellationToken);
        var conflict = await engine.RunPrivilegedAsync($"docker network inspect {networkName} --format \"{{{{.Name}}}}: {{{{.IPAM.Config}}}}\"", phase, observer, cancellationToken);
        var socketPermissions = await engine.RunAsync("ls -la /var/run/docker.sock 2>/dev/null || ls -la /run/docker.sock 2>/dev/null", cancellationToken);
        var rootless = await engine.RunPrivilegedAsync("docker info --format \"{{.SecurityOptions}}\"", phase, observer, cancellationToken);
        var whoAmI = await engine.RunAsync("id", cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("=== Docker Daemon Status (docker info) ===");
        builder.AppendLine(info.Succeeded ? info.StandardOutput.Trim() : $"(docker info failed: {info.StandardError})");
        builder.AppendLine();
        builder.AppendLine("=== Existing Docker Networks (docker network ls) ===");
        builder.AppendLine(networkList.Succeeded ? networkList.StandardOutput.Trim() : $"(docker network ls failed: {networkList.StandardError})");
        builder.AppendLine();
        builder.AppendLine($"=== Network Conflict Check ('{networkName}') ===");
        builder.AppendLine(conflict.Succeeded ? conflict.StandardOutput.Trim() : $"(no existing network named '{networkName}', or inspect failed: {conflict.StandardError})");
        builder.AppendLine();
        builder.AppendLine("=== Docker Socket Permissions ===");
        builder.AppendLine(socketPermissions.Succeeded && !string.IsNullOrWhiteSpace(socketPermissions.StandardOutput)
            ? socketPermissions.StandardOutput.Trim()
            : "(docker socket not found at its usual paths — inconclusive)");
        builder.AppendLine();
        builder.AppendLine("=== Rootless Docker Detection (SecurityOptions) ===");
        builder.AppendLine(rootless.Succeeded
            ? $"{rootless.StandardOutput.Trim()} — {(rootless.StandardOutput.Contains("rootless", StringComparison.OrdinalIgnoreCase) ? "ROOTLESS mode detected" : "standard (rootful) mode")}"
            : $"(could not read security options: {rootless.StandardError})");
        builder.AppendLine();
        builder.AppendLine("=== Effective Identity (id) ===");
        builder.AppendLine(whoAmI.Succeeded ? whoAmI.StandardOutput.Trim() : $"(id failed: {whoAmI.StandardError})");

        return builder.ToString();
    }

    public async Task<bool> IsRuntimeAgentRunningAsync(BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var running = await engine.RunPrivilegedAsync(
            $"docker ps --filter name={RuntimeAgentComposeProject} --filter status=running -q",
            phase,
            observer,
            cancellationToken);
        return running.Succeeded && !string.IsNullOrWhiteSpace(running.StandardOutput);
    }

    /// <summary>Null when the container does not exist or the reading is unparseable — an inconclusive reading, never fabricated state.</summary>
    public async Task<ContainerState?> GetContainerStateAsync(BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var inspect = await engine.RunPrivilegedAsync(
            $"docker inspect --format \"{{{{.State.Status}}}}|{{{{if .State.Health}}}}{{{{.State.Health.Status}}}}{{{{else}}}}none{{{{end}}}}|{{{{.State.ExitCode}}}}|{{{{.RestartCount}}}}\" {RuntimeAgentContainerName}",
            phase,
            observer,
            cancellationToken);

        if (!inspect.Succeeded)
        {
            return null;
        }

        var parts = inspect.StandardOutput.Trim().Split('|');
        if (parts.Length != 4 || !int.TryParse(parts[2], out var exitCode) || !int.TryParse(parts[3], out var restartCount))
        {
            return null;
        }

        return new ContainerState(parts[0], parts[1], exitCode, restartCount);
    }

    public Task<RemoteCommandResult> LoadImageAsync(string imageTarPath, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
        => engine.RunPrivilegedAsync($"docker load -i \"{imageTarPath}\"", phase, observer, cancellationToken);

    /// <summary>
    /// Always <c>--force-recreate</c>: <c>docker compose up -d</c> is idempotent against an
    /// unchanged service definition — it will not recreate an existing container just because it is
    /// crash-looping, it leaves Docker's own restart policy retrying the *same* already-created
    /// container indefinitely. A container created before a host-side fix (e.g. a permission repair
    /// applied to a bind mount after the container's first start) needs recreation to pick up a
    /// fresh, correct state — a real-world observed defect, not a hypothetical.
    /// </summary>
    public Task<RemoteCommandResult> ComposeUpAsync(string composeFilePath, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
        => engine.RunPrivilegedAsync(
            $"docker compose -f \"{composeFilePath}\" -p {RuntimeAgentComposeProject} up -d --force-recreate",
            phase,
            observer,
            cancellationToken);

    /// <summary>
    /// Polls container state until genuinely running and healthy, or a growing restart count proves
    /// it is crash-looping (in which case there is no point waiting out the remaining attempts). A
    /// container with no healthcheck configured at all is accepted once merely "running" — there is
    /// nothing else to wait for in that case. Container start is NOT success; this is.
    /// </summary>
    public async Task<ContainerHealthWait> WaitForHealthyAsync(int maxAttempts, int pollDelayMs, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        int? restartCountAtStart = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            observer?.OnPhaseProgress(
                phase,
                $"Waiting for Runtime Agent container to be running and healthy — attempt {attempt} of {maxAttempts}.",
                (double)attempt / maxAttempts);

            var state = await GetContainerStateAsync(phase, observer, cancellationToken);
            if (state is not null)
            {
                restartCountAtStart ??= state.RestartCount;

                if (state.IsRunningAndHealthy)
                {
                    return new ContainerHealthWait(true, false, $"status={state.Status}, health={state.Health}");
                }

                if (state.RestartCount > restartCountAtStart)
                {
                    return new ContainerHealthWait(
                        false,
                        true,
                        $"Restart count increased from {restartCountAtStart} to {state.RestartCount} while waiting (last exit code {state.ExitCode}, status={state.Status}) — the container is crash-looping, not merely slow to start.");
                }
            }

            if (attempt < maxAttempts && pollDelayMs > 0)
            {
                await Task.Delay(pollDelayMs, cancellationToken);
            }
        }

        return new ContainerHealthWait(false, false, $"Runtime Agent did not report running and healthy after {maxAttempts} attempts.");
    }

    /// <summary>
    /// Container-level diagnostics: <c>docker logs</c>, a concise summary of <c>docker inspect</c>
    /// (state/restart count/exit code — never the full multi-hundred-line JSON, which would swamp
    /// the failure card), and <c>docker compose ps</c>. Best-effort throughout: a diagnostic
    /// command failing to run is reported as such, never allowed to mask the failure it is
    /// attached to.
    /// </summary>
    public async Task<string> CollectContainerDiagnosticsAsync(BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var logs = await engine.RunPrivilegedAsync($"docker logs {RuntimeAgentContainerName} --tail 200", phase, observer, cancellationToken);
        var inspect = await engine.RunPrivilegedAsync($"docker inspect {RuntimeAgentContainerName}", phase, observer, cancellationToken);
        var composePs = await engine.RunPrivilegedAsync($"docker compose -p {RuntimeAgentComposeProject} ps", phase, observer, cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("=== Container State (docker inspect) ===");
        builder.AppendLine(inspect.Succeeded ? SummarizeContainerState(inspect.StandardOutput) : $"(docker inspect failed: {inspect.StandardError})");
        builder.AppendLine();
        builder.AppendLine("=== docker compose ps ===");
        builder.AppendLine(composePs.Succeeded ? composePs.StandardOutput.Trim() : $"(docker compose ps failed: {composePs.StandardError})");
        builder.AppendLine();
        builder.AppendLine("=== docker logs --tail 200 ===");
        builder.AppendLine(logs.Succeeded ? logs.StandardOutput.Trim() : $"(docker logs failed: {logs.StandardError})");

        return builder.ToString();
    }

    private static string SummarizeContainerState(string inspectJson)
    {
        try
        {
            using var document = JsonDocument.Parse(inspectJson);
            var root = document.RootElement[0];
            var state = root.GetProperty("State");
            var status = state.GetProperty("Status").GetString();
            var exitCode = state.GetProperty("ExitCode").GetInt32();
            var restartCount = root.GetProperty("RestartCount").GetInt32();
            var startedAt = state.TryGetProperty("StartedAt", out var startedAtElement) ? startedAtElement.GetString() : "unknown";
            var finishedAt = state.TryGetProperty("FinishedAt", out var finishedAtElement) ? finishedAtElement.GetString() : "unknown";
            var healthStatus = state.TryGetProperty("Health", out var health) && health.TryGetProperty("Status", out var healthStatusElement)
                ? healthStatusElement.GetString()
                : "none";

            return $"Status: {status} | Health: {healthStatus} | ExitCode: {exitCode} | RestartCount: {restartCount} | StartedAt: {startedAt} | FinishedAt: {finishedAt}";
        }
        catch (Exception exception)
        {
            return $"(could not parse docker inspect output: {exception.Message})";
        }
    }
}
