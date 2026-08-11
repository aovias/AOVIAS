using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Prepares the remote directory scaffold (profile directory + the runtime parent tree's
/// ownership/permissions) through the centralized <see cref="PermissionRepairService"/>. Discovery
/// requires positive proof — checkpoint recorded AND profile directory present AND the tree's mode
/// verified on disk — before skipping; anything less re-runs the (cheap, idempotent) repair pass.
/// Deliberately only the profile scaffold, not Runtime Agent's own nested runtime directories —
/// <c>IDeploymentPathResolver.ValidateStorageLayout(createMissing:true)</c> already creates those
/// on every agent start, and duplicating that here would be a parallel implementation.
/// </summary>
public sealed class RuntimeDirectoryStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.RuntimeDirectoryCreation;

    public override async Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        if (!context.Checkpointed)
        {
            return StageDiscovery.NotSatisfied("Directory preparation has not been recorded complete for this target — the repair pass will run.");
        }

        var profileDirectory = ResolveProfileDirectory(context.Target);
        if (!await context.Executor.RemotePathExistsAsync(profileDirectory, cancellationToken))
        {
            return StageDiscovery.NotSatisfied($"Profile directory '{profileDirectory}' is missing despite a recorded completion — remote state wins, re-preparing.");
        }

        var runtimeParent = PermissionRepairService.ResolveRuntimeParent(context.Target);
        var verification = await context.Permissions.VerifyModeAsync(runtimeParent, "0777", cancellationToken);
        return verification.State == PermissionVerificationState.Verified
            ? StageDiscovery.SatisfiedBecause($"Profile directory exists and {verification.Detail}")
            : StageDiscovery.NotSatisfied($"Permissions need re-applying: {verification.Detail}");
    }

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var profileDirectory = ResolveProfileDirectory(context.Target);
        var outcome = await context.Permissions.EnsureRuntimeTreeAsync(context.Target, profileDirectory, Phase, context.Observer, cancellationToken);
        return outcome.Succeeded
            ? StageOutcome.Ok(outcome.Evidence)
            : StageOutcome.Fail(outcome.Evidence, outcome.Error);
    }

    /// <summary>Positive stat proof passes; an inconclusive reading (host without a parseable <c>stat</c>) is tolerated after a successful repair pass — only a genuine, parsed wrong mode fails.</summary>
    public override async Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var runtimeParent = PermissionRepairService.ResolveRuntimeParent(context.Target);
        var verification = await context.Permissions.VerifyModeAsync(runtimeParent, "0777", cancellationToken);
        return verification.State == PermissionVerificationState.Mismatch
            ? StageOutcome.Fail("Runtime Startup Failed — directory permissions did not take.", verification.Detail)
            : StageOutcome.Ok(verification.Detail);
    }

    /// <summary>A verified-wrong mode after a successful pass points at something actively resetting it — one more chattr+chmod round is the bounded repair before failing with evidence.</summary>
    public override async Task<StageRepair> TryRepairAsync(BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
    {
        if (executionFailed)
        {
            return StageRepair.None;
        }

        var outcome = await context.Permissions.EnsureRuntimeTreeAsync(context.Target, ResolveProfileDirectory(context.Target), Phase, context.Observer, cancellationToken);
        return outcome.Succeeded
            ? StageRepair.Repaired("Re-applied ownership, attributes, and permissions across the runtime tree.")
            : StageRepair.Failed($"Permission re-application failed: {outcome.Error}");
    }

    public override async Task<string?> CollectDiagnosticsAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => await context.Permissions.InspectAsync(
            [PermissionRepairService.ResolveRuntimeParent(context.Target)],
            Phase,
            context.Observer,
            cancellationToken);

    private static string ResolveProfileDirectory(BootstrapTarget target)
        => $"{target.RemoteConfigRoot.TrimEnd('/')}/profiles/{target.ProfileName}";
}
