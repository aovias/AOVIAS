using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Sprint M7.x.2 — SSH connectivity as its own first-class stage (see <see cref="BootstrapPhaseName.Connect"/>'s
/// remarks for why this was promoted out of unguarded pre-loop code). Reachability is a session
/// property, not a one-time fact — discovery never honors the checkpoint; every run re-probes the
/// connection fresh.
/// </summary>
public sealed class ConnectStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.Connect;

    public override Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => ProbeAsync(context, cancellationToken);

    /// <summary>A successful probe IS the verification — there is nothing further to confirm.</summary>
    public override Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageOutcome.Ok("Connectivity probe is its own verification."));

    /// <summary>One bounded retry after a short delay — a transient network blip (the exact class of condition this stage exists to tolerate) often clears in a couple of seconds.</summary>
    public override async Task<StageRepair> TryRepairAsync(BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
    {
        if (context.Tunables.ConnectRetryDelayMs > 0)
        {
            await Task.Delay(context.Tunables.ConnectRetryDelayMs, cancellationToken);
        }

        var retry = await ProbeAsync(context, cancellationToken);
        return retry.Succeeded
            ? StageRepair.Repaired("Connection succeeded on a retried probe after a short delay.")
            : StageRepair.Failed($"Retried probe still failed: {retry.Message}");
    }

    private static async Task<StageOutcome> ProbeAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        context.Observer?.OnPhaseProgress(BootstrapPhaseName.Connect, "Connecting to target host...", null);
        var connection = await context.Executor.TestConnectionAsync(cancellationToken);
        return connection.Succeeded
            ? StageOutcome.Ok("Target host is reachable over SSH.")
            : StageOutcome.Fail("Could not establish a connection to the target host.", connection.StandardError);
    }
}
