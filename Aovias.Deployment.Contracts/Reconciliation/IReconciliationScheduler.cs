using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>Why a reconciliation cycle was requested (Sprint M2.6 D8).</summary>
public enum ReconciliationTrigger
{
    Periodic,
    PostDeployment,
    Manual,
    EventTriggered
}

/// <summary>
/// Supplies every current reconciliation target — one per service this Runtime Agent has
/// successfully deployed — without the portable, hosting-independent
/// <see cref="ReconciliationScheduler"/> needing to know anything about how Runtime Agent tracks
/// deployments (Sprint M2.6 D8). An empty list means no deployment has completed yet — a
/// scheduler with nothing to reconcile against is a no-op, not an error.
/// </summary>
public interface IReconciliationTargetProvider
{
    IReadOnlyList<ReconciliationTarget> GetCurrentTargets();
}

/// <summary>One resolved reconciliation target (Sprint M2.6 D8) — the latest approved plan/contract/manifest for one service, plus the policy set and safety-context factory to reconcile it against.</summary>
public sealed class ReconciliationTarget
{
    public required DeploymentPlan Plan { get; init; }

    public required InfrastructureContract Contract { get; init; }

    public required ServiceManifest Manifest { get; init; }

    public required RepairPolicySet PolicySet { get; init; }

    public required Func<RepairTask, SafetyContext> SafetyContextFactory { get; init; }

    public string? ResolvedNetworkName { get; init; }
}

/// <summary>
/// Coordinates when reconciliation cycles run (Sprint M2.6 D8): periodic, immediate after a
/// deployment, on manual request, or triggered by an external event — while guaranteeing cycles
/// never overlap. The sole implementation is <see cref="ReconciliationScheduler"/>.
/// </summary>
public interface IReconciliationScheduler
{
    /// <summary>
    /// Requests a reconciliation cycle covering every current target (one per deployed service).
    /// If a cycle is already running, this request is coalesced (skipped, not queued) and an
    /// empty list is returned — "avoid overlapping reconciliation cycles" (D8) is enforced by
    /// never starting a second cycle concurrently, rather than by queueing requests that could
    /// pile up under sustained drift.
    /// </summary>
    Task<IReadOnlyList<ReconciliationResult>> RequestReconciliationAsync(ReconciliationTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary>True while a cycle is currently executing.</summary>
    bool IsRunning { get; }
}

/// <summary>The single <see cref="IReconciliationScheduler"/> implementation (Sprint M2.6 D8). Deliberately has no dependency on <c>Microsoft.Extensions.Hosting</c> — the periodic timer that calls <see cref="RequestReconciliationAsync"/> on an interval is a thin <c>BackgroundService</c> Runtime Agent owns; this class is the portable, independently testable coordination logic underneath it.</summary>
public sealed class ReconciliationScheduler : IReconciliationScheduler
{
    private readonly IReconciliationOrchestrator orchestrator;
    private readonly IReconciliationTargetProvider targetProvider;
    private readonly SemaphoreSlim gate = new(1, 1);

    public ReconciliationScheduler(IReconciliationOrchestrator orchestrator, IReconciliationTargetProvider targetProvider)
    {
        this.orchestrator = orchestrator;
        this.targetProvider = targetProvider;
    }

    public bool IsRunning => gate.CurrentCount == 0;

    public async Task<IReadOnlyList<ReconciliationResult>> RequestReconciliationAsync(ReconciliationTrigger trigger, CancellationToken cancellationToken = default)
    {
        if (!await gate.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            // A cycle is already running — coalesce rather than overlap (D8).
            return [];
        }

        try
        {
            var targets = targetProvider.GetCurrentTargets();
            if (targets.Count == 0)
            {
                return []; // Nothing has been deployed yet — nothing to reconcile.
            }

            var results = new List<ReconciliationResult>(targets.Count);
            foreach (var target in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var correlationId = $"reconcile-{trigger}-{Guid.NewGuid():N}";
                var result = await orchestrator.RunAsync(
                    target.Plan,
                    target.Contract,
                    target.Manifest,
                    target.PolicySet,
                    target.SafetyContextFactory,
                    correlationId,
                    resolvedNetworkName: target.ResolvedNetworkName,
                    cancellationToken: cancellationToken);
                results.Add(result);
            }

            return results;
        }
        finally
        {
            gate.Release();
        }
    }
}
