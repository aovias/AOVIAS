using Aovias.Bootstrap.Contracts;

namespace ComplianceServiceManager.Models.Bootstrap;

/// <summary>
/// Real facts gathered from the target host over SSH before provisioning starts. This is CSM-side
/// discovery (new code, this session) — the Bootstrap engine itself has no fact-gathering surface
/// beyond a pass/fail "is this Ubuntu" preflight check, so nothing here is sourced from it.
/// </summary>
public sealed class TargetHostFacts
{
    public string UbuntuVersion { get; init; } = "Unknown";
    public string Hostname { get; init; } = "Unknown";
    public string CpuSummary { get; init; } = "Unknown";
    public string MemorySummary { get; init; } = "Unknown";
    public string DiskSummary { get; init; } = "Unknown";
    public string PublicIp { get; init; } = "Unknown";
    public bool DockerInstalled { get; init; }
    public string DockerVersion { get; init; } = "Not installed";
    public string DockerComposeVersion { get; init; } = "Not installed";
    public bool ExistingRuntimeDetected { get; init; }
    public string? ExistingRuntimeConfigRoot { get; init; }

    /// <summary>Best-effort Runtime Version reported by an already-running AOVIAS Runtime Agent on this host, sourced from its own heartbeat endpoint on the default port. Null when no existing runtime was detected, or its heartbeat could not be reached (an existing-but-unreachable runtime is still worth flagging as "existing", just without a version).</summary>
    public string? ExistingRuntimePlatformVersion { get; init; }

    /// <summary>Mirrors the same detection <c>BootstrapOrchestrator.DetectPrivilegeModeAsync</c> performs authoritatively during Preflight — shown here only so the Target Host step can tell the operator up front whether a sudo password will be needed, per the "Never ask the operator to edit sudoers" rule.</summary>
    public BootstrapPrivilegeMode PrivilegeMode { get; init; } = BootstrapPrivilegeMode.NotRequired;
}

public enum ExistingRuntimeAction
{
    None,
    Repair,
    Upgrade,
    Reconcile
}
