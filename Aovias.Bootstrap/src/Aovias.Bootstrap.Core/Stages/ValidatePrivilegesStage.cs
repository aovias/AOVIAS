using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Sprint M7.x.2 — privilege-mode detection as its own first-class stage (see
/// <see cref="BootstrapPhaseName.ValidatePrivileges"/>'s remarks for the resumed-run latent defect
/// this closes). Discovery never honors the checkpoint — privilege mode is re-detected every
/// session, since it is a property of the current connection, not something that stays true once
/// recorded.
/// </summary>
public sealed class ValidatePrivilegesStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.ValidatePrivileges;

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        context.Observer?.OnPhaseProgress(Phase, "Detecting privilege elevation...", null);
        await context.Engine.DetectPrivilegeModeAsync(context.Target, cancellationToken);

        return context.Engine.PrivilegeMode == BootstrapPrivilegeMode.Unavailable
            ? StageOutcome.Fail($"Required Permissions: {context.Engine.PrivilegeModeDetail}")
            : StageOutcome.Ok(context.Engine.PrivilegeModeDetail);
    }

    /// <summary>Detection is its own verification — there is no separate way to confirm a privilege mode beyond having just determined it.</summary>
    public override Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageOutcome.Ok("Privilege-mode detection is its own verification."));
}
