using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Ensures the platform Docker network exists. Idempotent by discovery: an existing network is
/// never recreated; a missing one (including one deleted since the checkpoint was recorded) is
/// created. Default post-execution verification re-inspects the network.
/// </summary>
public sealed class DockerNetworkStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.DockerNetworkCreation;

    public override async Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var exists = await context.Docker.NetworkExistsAsync(context.Target.DockerNetworkName, Phase, context.Observer, cancellationToken);
        return exists
            ? StageDiscovery.SatisfiedBecause($"Docker network '{context.Target.DockerNetworkName}' already exists.")
            : StageDiscovery.NotSatisfied($"Docker network '{context.Target.DockerNetworkName}' does not exist.");
    }

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var create = await context.Docker.CreateNetworkAsync(context.Target.DockerNetworkName, Phase, context.Observer, cancellationToken);
        return create.Succeeded
            ? StageOutcome.Ok($"Docker network '{context.Target.DockerNetworkName}' was created.")
            : StageOutcome.Fail($"Docker Network Failed — could not create '{context.Target.DockerNetworkName}'.", create.StandardError);
    }

    /// <summary>Self-heal for a network that inspects as missing right after creation reported success (daemon hiccup): create it again.</summary>
    public override async Task<StageRepair> TryRepairAsync(BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
    {
        if (executionFailed)
        {
            return StageRepair.None;
        }

        var recreate = await context.Docker.CreateNetworkAsync(context.Target.DockerNetworkName, Phase, context.Observer, cancellationToken);
        return recreate.Succeeded
            ? StageRepair.Repaired($"Re-created Docker network '{context.Target.DockerNetworkName}'.")
            : StageRepair.Failed($"Network re-creation failed: {recreate.StandardError}");
    }

    /// <summary>
    /// Sprint M7.x.2: automatic Docker network diagnostics on any failure — daemon status, every
    /// existing network, a direct conflict check against this network's name, socket permissions,
    /// rootless-mode detection, and effective identity. Never "Unhandled exception" again.
    /// </summary>
    public override async Task<string?> CollectDiagnosticsAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => await context.Docker.CollectNetworkDiagnosticsAsync(context.Target.DockerNetworkName, Phase, context.Observer, cancellationToken);
}
