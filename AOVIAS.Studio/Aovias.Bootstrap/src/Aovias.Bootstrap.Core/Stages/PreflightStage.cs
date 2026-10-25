using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Sprint M7.x — the Precondition Engine. Validates every blocker this stage is responsible for
/// catching before Stage 4 makes any change, and reports ALL of them together (not just the
/// first) — an operator fixing preflight issues one SSH round-trip at a time is exactly the
/// friction a one-click flow must not have. Connectivity and privilege-mode detection are now
/// their own preceding stages (<see cref="ConnectStage"/>, <see cref="ValidatePrivilegesStage"/> —
/// Sprint M7.x.2, promoted out of this stage and out of orchestrator pre-loop code for guaranteed
/// failure attribution); this stage validates everything else: OS identity/version, disk, memory,
/// CPU architecture, outbound network, DNS resolution, and registration intent. An inconclusive
/// remote reading (a command whose output could not be parsed) is never treated as a blocker —
/// only a genuine, parsed negative signal is.
/// </summary>
public sealed class PreflightStage : BootstrapStage
{
    /// <summary>Ubuntu releases Docker's own install script (<c>get.docker.com</c>) and this platform actively support. Kept as a small fixed list rather than "any Ubuntu" — an unsupported release is exactly the kind of blocker Preflight exists to catch.</summary>
    private static readonly string[] SupportedUbuntuVersions = ["20.04", "22.04", "24.04"];

    /// <summary>CPU architectures the Runtime Agent image is built for.</summary>
    private static readonly string[] SupportedArchitectures = ["x86_64", "amd64", "aarch64", "arm64"];

    /// <summary>Minimum free space on <c>/</c> Bootstrap requires before installing Docker and the Runtime Agent — deliberately conservative; this is a blocker check, not a tight capacity plan.</summary>
    private const long MinimumFreeDiskBytes = 5L * 1024 * 1024 * 1024;

    /// <summary>Minimum total memory for Docker plus the Runtime Agent to run at all.</summary>
    private const long MinimumTotalMemoryMb = 512;

    public override BootstrapPhaseName Phase => BootstrapPhaseName.Preflight;

    /// <summary>
    /// The full preflight battery is expensive to re-run and its subjects (OS release, CPU
    /// architecture, disk, memory) do not drift session-to-session, so a checkpointed preflight is
    /// honored — reachability and privilege mode, the two things that DO drift between sessions,
    /// are their own separate stages (<see cref="ConnectStage"/>, <see cref="ValidatePrivilegesStage"/>)
    /// that never honor the checkpoint and always run first.
    /// </summary>
    public override Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(context.Checkpointed
            ? StageDiscovery.SatisfiedBecause("Preflight previously passed for this target; host identity/resources do not drift session-to-session.")
            : StageDiscovery.NotSatisfied("Preflight has not passed for this target yet."));

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var target = context.Target;
        var blockers = new List<string>();

        // Registration requirements — pure validation of already-supplied intent, no I/O.
        if (string.IsNullOrWhiteSpace(target.RegistrationToken))
        {
            blockers.Add("Registration Requirements: a registration token is required.");
        }

        if (string.IsNullOrWhiteSpace(target.CommandCenterBaseUrl))
        {
            blockers.Add("Registration Requirements: a Command Center base URL is required for fleet registration.");
        }

        if (string.IsNullOrWhiteSpace(target.PublicUrl))
        {
            blockers.Add("Registration Requirements: a public URL is required to reach the agent once installed.");
        }

        if (blockers.Count > 0)
        {
            return StageOutcome.Fail("Preflight failed before contacting the target host.", string.Join(" | ", blockers));
        }

        context.Observer?.OnPhaseProgress(Phase, "Checking Ubuntu version and Docker compatibility...", null);
        var osCheck = await context.Engine.RunAsync("cat /etc/os-release", cancellationToken);
        if (!osCheck.Succeeded || !osCheck.StandardOutput.Contains("Ubuntu", StringComparison.OrdinalIgnoreCase))
        {
            return StageOutcome.Fail("Target host is not a recognized Ubuntu VPS.", osCheck.StandardError);
        }

        var ubuntuVersion = ParseOsReleaseVersion(osCheck.StandardOutput);
        if (ubuntuVersion is not null && !SupportedUbuntuVersions.Contains(ubuntuVersion))
        {
            blockers.Add($"Ubuntu Supported: Ubuntu {ubuntuVersion} is not a supported release (supported: {string.Join(", ", SupportedUbuntuVersions)}) — Docker Compatibility cannot be assumed.");
        }

        context.Observer?.OnPhaseProgress(Phase, "Checking CPU architecture...", null);
        var archCheck = await context.Engine.RunAsync("uname -m", cancellationToken);
        var architecture = archCheck.StandardOutput.Trim();
        if (archCheck.Succeeded && !string.IsNullOrWhiteSpace(architecture) && !SupportedArchitectures.Contains(architecture, StringComparer.OrdinalIgnoreCase))
        {
            blockers.Add($"Supported Architecture: '{architecture}' is not a supported CPU architecture (supported: {string.Join(", ", SupportedArchitectures)}).");
        }

        context.Observer?.OnPhaseProgress(Phase, "Checking available disk space...", null);
        var diskCheck = await context.Engine.RunAsync("df -k / | awk 'NR==2 {print $4}'", cancellationToken);
        if (diskCheck.Succeeded && long.TryParse(diskCheck.StandardOutput.Trim(), out var availableKb))
        {
            var availableBytes = availableKb * 1024L;
            if (availableBytes < MinimumFreeDiskBytes)
            {
                blockers.Add($"Disk Space: only {availableBytes / 1024 / 1024} MB free on '/' — at least {MinimumFreeDiskBytes / 1024 / 1024} MB is required.");
            }
        }

        context.Observer?.OnPhaseProgress(Phase, "Checking available memory...", null);
        var memoryCheck = await context.Engine.RunAsync("free -m | awk 'NR==2 {print $2}'", cancellationToken);
        if (memoryCheck.Succeeded && long.TryParse(memoryCheck.StandardOutput.Trim(), out var totalMb) && totalMb < MinimumTotalMemoryMb)
        {
            blockers.Add($"Memory: only {totalMb} MB total memory — at least {MinimumTotalMemoryMb} MB is required for Docker and the Runtime Agent.");
        }

        context.Observer?.OnPhaseProgress(Phase, "Checking outbound network connectivity...", null);
        // A generic, vendor-neutral reachability probe (deliberately not get.docker.com — that
        // domain is also DockerInstallation's own install-script source, and reusing it here would
        // make this check indistinguishable from "is Docker's installer reachable" for a host that
        // blocks only that one domain). Raw IP on purpose, so it isolates connectivity from DNS.
        var networkCheck = await context.Engine.RunAsync("curl -fsS -m 5 -o /dev/null https://1.1.1.1", cancellationToken);
        if (!networkCheck.Succeeded)
        {
            blockers.Add("Network Connectivity: the target host could not reach the public internet (required to install Docker and reach the Command Center).");
        }

        context.Observer?.OnPhaseProgress(Phase, "Checking DNS resolution...", null);
        // Complements the raw-IP probe above: a host with a working default route but a broken
        // resolver passes 1.1.1.1 and then fails the Docker install script's own download — this
        // catches that before any change is made. download.docker.com (the package host the
        // installer actually pulls from), deliberately not get.docker.com — that exact string is
        // DockerInstallation's own install-script marker, and a probe must never be mistakable for
        // the install itself.
        var dnsCheck = await context.Engine.RunAsync("getent hosts download.docker.com", cancellationToken);
        if (!dnsCheck.Succeeded)
        {
            blockers.Add("DNS Resolution: the target host could not resolve download.docker.com — its DNS resolver appears broken (required to install Docker).");
        }

        if (blockers.Count > 0)
        {
            return StageOutcome.Fail("Preflight found one or more blockers.", string.Join(" | ", blockers));
        }

        return StageOutcome.Ok(
            $"Target host is running a supported Ubuntu release ({ubuntuVersion ?? "unknown"}) and passed disk/memory/architecture/network/DNS checks. Privilege mode ({context.Engine.PrivilegeMode}) was already confirmed by the Validate Privileges stage.");
    }

    /// <summary>Preflight is pure checking — a successful execution IS the verification.</summary>
    public override Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageOutcome.Ok("Preflight checks are their own verification."));

    private static string? ParseOsReleaseVersion(string osRelease)
    {
        foreach (var key in new[] { "VERSION_ID", "VERSION" })
        {
            var match = System.Text.RegularExpressions.Regex.Match(osRelease, $"^{key}=\"?([^\"\n]+)\"?", System.Text.RegularExpressions.RegexOptions.Multiline);
            if (!match.Success)
            {
                continue;
            }

            foreach (var supported in SupportedUbuntuVersions)
            {
                if (match.Groups[1].Value.Contains(supported, StringComparison.Ordinal))
                {
                    return supported;
                }
            }

            return match.Groups[1].Value.Trim();
        }

        return null;
    }
}
