using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Installs Docker Engine if — and only if — the target does not already have it. Discovery reads
/// the actual host (never the checkpoint): an existing installation of any origin is honored, and
/// a host whose checkpoint claims Docker exists but whose CLI is gone gets it reinstalled
/// (self-heal) instead of failing four stages later.
/// </summary>
public sealed class DockerInstallationStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.DockerInstallation;

    public override async Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var check = await context.Docker.GetCliVersionAsync(cancellationToken);
        return check.Succeeded
            ? StageDiscovery.SatisfiedBecause($"Docker is already installed ({check.StandardOutput.Trim()}).")
            : StageDiscovery.NotSatisfied("Docker CLI is not present on the target.");
    }

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        context.Observer?.OnPhaseProgress(Phase, "Installing Docker Engine...", null);
        var install = await context.Docker.InstallAsync(Phase, context.Observer, cancellationToken);
        return install.Succeeded
            ? StageOutcome.Ok("Docker was installed and started.")
            : StageOutcome.Fail("Docker installation failed.", install.StandardError);
    }

    /// <summary>An installed CLI is not a working engine — verification asks the daemon itself.</summary>
    public override async Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var daemon = await context.Docker.VerifyDaemonAsync(Phase, context.Observer, cancellationToken);
        return daemon.Succeeded
            ? StageOutcome.Ok("Docker daemon is answering.")
            : StageOutcome.Fail("Runtime Startup Failed — Docker was installed but its daemon is not answering.", daemon.StandardError);
    }

    /// <summary>Self-heal for a daemon that is not answering: restart it and let the orchestrator re-verify. Not attempted for an install-script failure — rerunning a failed installer without changing anything is churn, not repair.</summary>
    public override async Task<StageRepair> TryRepairAsync(BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
    {
        if (executionFailed)
        {
            return StageRepair.None;
        }

        context.Observer?.OnPhaseProgress(Phase, "Repairing: restarting the Docker daemon...", null);
        var restart = await context.Docker.RestartDaemonAsync(Phase, context.Observer, cancellationToken);
        return restart.Succeeded
            ? StageRepair.Repaired("Restarted the Docker daemon (systemctl restart docker).")
            : StageRepair.Failed($"Docker daemon restart failed: {restart.StandardError}");
    }
}
