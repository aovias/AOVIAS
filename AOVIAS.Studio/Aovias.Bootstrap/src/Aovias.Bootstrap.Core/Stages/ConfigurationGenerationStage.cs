using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Configuration synchronization (Sprint M7.x): every run regenerates the full profile
/// configuration deterministically, and the <see cref="ConfigurationSynchronizer"/> uploads only
/// what is missing or drifted — verified by checksum, so a target whose files were hand-edited,
/// corrupted, or written by an older build always converges back to what this build generates,
/// while an already-current target uploads nothing at all. Never depends on previous execution:
/// discovery is a pure checksum comparison against the actual remote files, not the checkpoint.
/// </summary>
public sealed class ConfigurationGenerationStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.RuntimeConfigurationGeneration;

    public override async Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var assessment = await context.Configuration.AssessAsync(context.Target, cancellationToken);
        return assessment.AllCurrent
            ? StageDiscovery.SatisfiedBecause(assessment.Describe())
            : StageDiscovery.NotSatisfied($"Configuration drift detected: {assessment.Describe()}");
    }

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var result = await context.Configuration.SynchronizeAsync(context.Target, Phase, context.Observer, cancellationToken);
        if (!result.Succeeded)
        {
            return StageOutcome.Fail(
                "Configuration Validation Failed — one or more uploaded configuration files failed checksum verification.",
                string.Join(" | ", result.VerificationFailures));
        }

        return StageOutcome.Ok(
            result.Uploaded.Count > 0
                ? $"Synchronized configuration — uploaded: {string.Join(", ", result.Uploaded)}; unchanged: {result.Unchanged.Count}."
                : $"All {result.Unchanged.Count} configuration files were already current.");
    }

    /// <summary>
    /// Deliberately NOT the default re-discovery: discovery treats an unverifiable checksum as
    /// drift (re-upload rather than trust), which on a host without a usable <c>sha256sum</c> would
    /// make re-discovery fail forever after a perfectly good upload. Post-execution verification
    /// instead requires only that every file exists and no parsed hash mismatches — an inconclusive
    /// reading passes with a note, the same rule every other stage applies to remote readings.
    /// </summary>
    public override async Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var (verified, evidence) = await context.Configuration.VerifyCurrentAsync(context.Target, cancellationToken);
        return verified
            ? StageOutcome.Ok(evidence)
            : StageOutcome.Fail("Configuration Validation Failed — synchronized configuration did not verify on the target.", evidence);
    }

    /// <summary>Corrupted-upload self-heal: one full re-synchronization pass, then re-verify.</summary>
    public override async Task<StageRepair> TryRepairAsync(BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
    {
        var retry = await context.Configuration.SynchronizeAsync(context.Target, Phase, context.Observer, cancellationToken);
        return retry.Succeeded
            ? StageRepair.Repaired($"Re-synchronized configuration ({retry.Uploaded.Count} file(s) re-uploaded).")
            : StageRepair.Failed($"Re-synchronization still failed verification: {string.Join(" | ", retry.VerificationFailures)}");
    }
}
