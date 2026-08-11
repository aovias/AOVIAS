using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// An in-memory simulated remote host: tracks whether Docker is "installed", which Docker
/// networks "exist", which remote paths "exist", and every command executed — so tests can assert
/// both outcomes (does the phase report success) and side effects (did an idempotent re-run avoid
/// re-running an install command it didn't need to).
/// </summary>
public sealed class FakeBootstrapTargetExecutor : IBootstrapTargetExecutor
{
    public bool ConnectionSucceeds { get; set; } = true;

    public string OsRelease { get; set; } = "NAME=\"Ubuntu\"\nVERSION=\"22.04\"";

    public bool DockerInstalled { get; set; }

    public bool DockerInstallCommandFails { get; set; }

    /// <summary>When true, <c>docker network create</c> throws a genuine .NET exception (a real
    /// SSH/library-level failure — e.g. a channel drop mid-command) instead of returning a failed
    /// <see cref="RemoteCommandResult"/>. Used to prove the guarded-lifecycle mechanism attributes an
    /// exception to the stage it actually happened in, never to some other/earlier stage.</summary>
    public bool ThrowOnDockerNetworkCreate { get; set; }

    /// <summary>When true, the <c>sudo -n true</c> probe (ValidatePrivileges' own first command) throws instead of returning a result — same purpose as <see cref="ThrowOnDockerNetworkCreate"/>, one stage earlier.</summary>
    public bool ThrowOnPrivilegeDetection { get; set; }

    public HashSet<string> ExistingDockerNetworks { get; } = [];

    public HashSet<string> ExistingRemotePaths { get; } = [];

    public Dictionary<string, string> UploadedTextFiles { get; } = [];

    public List<string> UploadedBinaryPaths { get; } = [];

    public List<string> ExecutedCommands { get; } = [];

    public bool AgentAlreadyRunning { get; set; }

    /// <summary>Whether the fake "remote host" reports the runtime-agent container as running+healthy for <c>docker inspect --format ...</c> — the container health-wait's success path. Default true (the normal case: a container that starts cleanly).</summary>
    public bool ContainerHealthy { get; set; } = true;

    /// <summary>When true, each successive <c>docker inspect --format ...</c> call reports an increasing restart count with status "restarting" — simulates the real-world crash-loop this defect fix's health-wait must detect without waiting out the full attempt budget.</summary>
    public bool SimulateCrashLoop { get; set; }

    /// <summary>Invoked whenever a <c>docker compose ... up -d</c> lands on this fake host — lets a test change the simulated container's condition in response to a (re)create, e.g. "unhealthy until recreated".</summary>
    public Action? OnComposeUp { get; set; }

    private int containerRestartCount;

    /// <summary>Deterministic stand-in for a real SHA-256 (this fake never touches the local
    /// filesystem, so the local artifact path tests set — which does not exist on disk — is never
    /// actually read). Settable so a test can simulate an upload that fails checksum verification.</summary>
    public string LocalFileChecksum { get; set; } = "fake-local-checksum";

    /// <summary>What the fake "remote host" reports back for <c>sha256sum</c> on the uploaded
    /// package. Defaults to matching <see cref="LocalFileChecksum"/> (a successful upload); set to a
    /// different value to simulate a checksum-mismatch failure.</summary>
    public string? RemoteFileChecksum { get; set; }

    /// <summary>Available disk space Preflight's <c>df</c> check should see, in KB. Null (default) simulates a host where the check command is not recognized (empty output — treated as inconclusive, never a blocker).</summary>
    public long? AvailableDiskKb { get; set; }

    public bool NetworkConnectivitySucceeds { get; set; } = true;

    /// <summary>Whether the fake "remote host" reports <c>sudo -n true</c> as succeeding (passwordless sudo).</summary>
    public bool HasRequiredPermissions { get; set; } = true;

    /// <summary>Whether <c>id -nG</c> reports the SSH user as a member of the sudo/wheel group — only consulted when <see cref="HasRequiredPermissions"/> is false. Combined with that, drives PasswordRequired (true) vs. Unavailable (false).</summary>
    public bool InSudoGroup { get; set; }

    /// <summary>The only password <see cref="ExecuteElevatedCommandAsync"/> accepts as correct — anything else (including null, for a passwordless-mode caller that should never have been asked) simulates sudo's own rejection.</summary>
    public string CorrectSudoPassword { get; set; } = "correct-sudo-password";

    /// <summary>Every call made through <see cref="ExecuteElevatedCommandAsync"/>, in order — separate from <see cref="ExecutedCommands"/> (which also records these, since the fake delegates to <see cref="ExecuteCommandAsync"/> internally) so a test can assert elevation specifically happened without caring about literal sudo command-line syntax, which only the real SSH.NET-backed executor constructs.</summary>
    public List<(string Command, bool RequiresSudo, string? Password)> ElevatedCommands { get; } = [];

    public Task<RemoteCommandResult> TestConnectionAsync(CancellationToken cancellationToken)
        => Task.FromResult(ConnectionSucceeds
            ? new RemoteCommandResult { ExitCode = 0 }
            : new RemoteCommandResult { ExitCode = 1, StandardError = "connection refused" });

    /// <summary>When set, <c>cat .../manifest.json</c> on the fake host returns this version.</summary>
    public string? DeployedRuntimeAgentVersion { get; set; }

    public Task<RemoteCommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        ExecutedCommands.Add(command);

        if (command.StartsWith("cat ", StringComparison.Ordinal) && command.Contains("manifest.json", StringComparison.Ordinal))
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(DeployedRuntimeAgentVersion)
                ? new RemoteCommandResult
                {
                    ExitCode = 0,
                    StandardOutput = $"{{\"Version\":\"{DeployedRuntimeAgentVersion}\"}}"
                }
                : new RemoteCommandResult { ExitCode = 1, StandardError = "manifest not found" });
        }

        if (command.StartsWith("cat /etc/os-release", StringComparison.Ordinal))
        {
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = OsRelease });
        }

        if (command.StartsWith("docker --version", StringComparison.Ordinal))
        {
            return Task.FromResult(DockerInstalled
                ? new RemoteCommandResult { ExitCode = 0, StandardOutput = "Docker version 27.0.0" }
                : new RemoteCommandResult { ExitCode = 1, StandardError = "docker: command not found" });
        }

        if (command.Contains("get.docker.com", StringComparison.Ordinal))
        {
            if (DockerInstallCommandFails)
            {
                return Task.FromResult(new RemoteCommandResult { ExitCode = 1, StandardError = "install failed" });
            }

            DockerInstalled = true;
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0 });
        }

        if (command.StartsWith("docker network inspect ", StringComparison.Ordinal))
        {
            var name = command["docker network inspect ".Length..];
            return Task.FromResult(ExistingDockerNetworks.Contains(name)
                ? new RemoteCommandResult { ExitCode = 0 }
                : new RemoteCommandResult { ExitCode = 1, StandardError = "network not found" });
        }

        if (command.StartsWith("docker network create ", StringComparison.Ordinal))
        {
            if (ThrowOnDockerNetworkCreate)
            {
                throw new InvalidOperationException("Simulated SSH channel failure during docker network create.");
            }

            var name = command["docker network create ".Length..];
            ExistingDockerNetworks.Add(name);
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0 });
        }

        if (command.StartsWith("mkdir -p ", StringComparison.Ordinal))
        {
            var path = command["mkdir -p ".Length..];
            ExistingRemotePaths.Add(path);
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0 });
        }

        if (command.StartsWith("docker ps --filter name=aovias-runtime-agent", StringComparison.Ordinal))
        {
            return Task.FromResult(new RemoteCommandResult
            {
                ExitCode = 0,
                StandardOutput = AgentAlreadyRunning ? "abc123" : string.Empty
            });
        }

        if (command.StartsWith("docker run", StringComparison.Ordinal))
        {
            AgentAlreadyRunning = true;
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0 });
        }

        // Sprint G1 pre-flight fix: RunRuntimeAgentInstallationAsync now extracts the uploaded
        // .aovdeploy package and starts it via `docker compose ... up -d` (matching the pre-existing
        // `deploy` verb's own pattern) instead of a bare `docker run`, so this fake must recognize
        // the new command shape as "the agent is now running" the same way it already did for
        // `docker run` — otherwise every idempotency/resume test would see the container as
        // perpetually not-running and re-attempt installation on every subsequent phase check.
        if (command.Contains("docker compose", StringComparison.Ordinal) && command.Contains("up -d", StringComparison.Ordinal))
        {
            AgentAlreadyRunning = true;
            OnComposeUp?.Invoke();
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0 });
        }

        if (command.StartsWith("docker inspect --format ", StringComparison.Ordinal))
        {
            if (SimulateCrashLoop)
            {
                containerRestartCount++;
                return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = $"restarting|unhealthy|139|{containerRestartCount}" });
            }

            var status = ContainerHealthy ? "running" : "starting";
            var health = ContainerHealthy ? "healthy" : "starting";
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = $"{status}|{health}|0|{containerRestartCount}" });
        }

        if (command.StartsWith("docker inspect ", StringComparison.Ordinal))
        {
            // Plain (non---format) inspect, used only for diagnostics collection — a minimal, valid
            // single-element JSON array is all CollectStartupDiagnosticsAsync's summary needs.
            return Task.FromResult(new RemoteCommandResult
            {
                ExitCode = 0,
                StandardOutput = $$"""[{"State":{"Status":"restarting","ExitCode":139,"StartedAt":"2026-01-01T00:00:00Z","FinishedAt":"2026-01-01T00:00:01Z"},"RestartCount":{{containerRestartCount}}}]"""
            });
        }

        if (command.StartsWith("docker logs ", StringComparison.Ordinal))
        {
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = "fake container log output" });
        }

        if (command.StartsWith("docker compose", StringComparison.Ordinal) && command.Contains(" ps", StringComparison.Ordinal))
        {
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = "fake compose ps output" });
        }

        if (command.StartsWith("sha256sum ", StringComparison.Ordinal))
        {
            // Text files this fake "hosts" hash for real (SHA-256 of the stored content), so the
            // checksum-based configuration synchronizer sees genuine match/drift semantics; any
            // other path (e.g. the binary agent package, which this fake never stores content for)
            // falls back to the scripted checksum pair the package-integrity tests drive.
            var firstQuote = command.IndexOf('"');
            var secondQuote = firstQuote >= 0 ? command.IndexOf('"', firstQuote + 1) : -1;
            if (firstQuote >= 0 && secondQuote > firstQuote
                && UploadedTextFiles.TryGetValue(command[(firstQuote + 1)..secondQuote], out var hostedContent))
            {
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hostedContent))).ToLowerInvariant();
                return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = hash });
            }

            return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = RemoteFileChecksum ?? LocalFileChecksum });
        }

        if (command.StartsWith("stat -c", StringComparison.Ordinal))
        {
            // The runtime tree's mode as the permission engine's verification reads it — this fake
            // host always reports the fully-open mode Bootstrap applies.
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = "777" });
        }

        if (command.StartsWith("df -k /", StringComparison.Ordinal))
        {
            return Task.FromResult(AvailableDiskKb is { } kb
                ? new RemoteCommandResult { ExitCode = 0, StandardOutput = kb.ToString() }
                : new RemoteCommandResult { ExitCode = 0 });
        }

        if (command.Contains("curl -fsS -m 5 -o /dev/null https://1.1.1.1", StringComparison.Ordinal))
        {
            return Task.FromResult(NetworkConnectivitySucceeds
                ? new RemoteCommandResult { ExitCode = 0 }
                : new RemoteCommandResult { ExitCode = 1, StandardError = "curl: (6) Could not resolve host" });
        }

        if (command.Contains("sudo -n true", StringComparison.Ordinal))
        {
            if (ThrowOnPrivilegeDetection)
            {
                throw new InvalidOperationException("Simulated SSH failure during privilege detection.");
            }

            return Task.FromResult(HasRequiredPermissions
                ? new RemoteCommandResult { ExitCode = 0 }
                : new RemoteCommandResult { ExitCode = 1, StandardError = "sudo: a password is required" });
        }

        if (command.Contains("id -nG", StringComparison.Ordinal))
        {
            return Task.FromResult(new RemoteCommandResult { ExitCode = 0, StandardOutput = InSudoGroup ? "deploy sudo" : "deploy" });
        }

        return Task.FromResult(new RemoteCommandResult { ExitCode = 0 });
    }

    /// <summary>Delegates to <see cref="ExecuteCommandAsync"/> for outcome simulation — this fake
    /// never adds a literal "sudo " prefix (that shell syntax is the real SSH executor's concern
    /// only); what matters here is that elevation was actually requested and, when a password is
    /// required, that it was correct.</summary>
    public async Task<RemoteCommandResult> ExecuteElevatedCommandAsync(string command, bool requiresSudo, string? sudoPassword, CancellationToken cancellationToken)
    {
        ElevatedCommands.Add((command, requiresSudo, sudoPassword));

        if (requiresSudo && sudoPassword is not null && !string.Equals(sudoPassword, CorrectSudoPassword, StringComparison.Ordinal))
        {
            return new RemoteCommandResult { ExitCode = 1, StandardError = "sudo: 1 incorrect password attempt" };
        }

        return await ExecuteCommandAsync(command, cancellationToken);
    }

    /// <summary>Whether a real .aovdeploy package's extracted contents include <c>service-image.tar</c> — true by default (the normal shape every real package has); set false to simulate a package with no embedded image (nothing to <c>docker load</c>).</summary>
    public bool ServiceImageTarExists { get; set; } = true;

    public Task<bool> RemotePathExistsAsync(string remotePath, CancellationToken cancellationToken)
        => Task.FromResult(
            ExistingRemotePaths.Contains(remotePath)
            || UploadedTextFiles.ContainsKey(remotePath)
            || (ServiceImageTarExists && remotePath.EndsWith("/service-image.tar", StringComparison.Ordinal)));

    public Task UploadTextFileAsync(string remotePath, string content, CancellationToken cancellationToken)
    {
        UploadedTextFiles[remotePath] = content;
        ExistingRemotePaths.Add(remotePath);
        return Task.CompletedTask;
    }

    public Task UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken)
    {
        UploadedBinaryPaths.Add(remotePath);
        ExistingRemotePaths.Add(remotePath);
        return Task.CompletedTask;
    }

    public Task<string> ComputeLocalFileChecksumAsync(string localPath, CancellationToken cancellationToken)
        => Task.FromResult(LocalFileChecksum);
}
