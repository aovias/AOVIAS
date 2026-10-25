using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>The full outcome of one <see cref="IReconciliationOrchestrator.RunAsync"/> call (Sprint M2.6).</summary>
public sealed class ReconciliationResult
{
    public required DriftReport DriftReport { get; init; }

    public required ReconciliationPlan Plan { get; init; }

    public required SelfHealingResult Healing { get; init; }

    public IReadOnlyList<ReconciliationEvent> Events { get; init; } = [];

    public required double DurationMs { get; init; }
}

/// <summary>
/// Runs the complete reconciliation lifecycle exactly once (Sprint M2.6): Desired State -&gt;
/// Observe -&gt; Detect Drift -&gt; Classify -&gt; Plan Repair -&gt; Evaluate Policy -&gt; Execute
/// -&gt; Verify -&gt; Complete. This is the one facade every trigger (scheduler, post-deployment,
/// manual, event-triggered) calls through — there is exactly one reconciliation code path, not a
/// separate one per trigger kind. The sole implementation is <see cref="ReconciliationOrchestrator"/>.
/// </summary>
public interface IReconciliationOrchestrator
{
    Task<ReconciliationResult> RunAsync(
        DeploymentPlan plan,
        InfrastructureContract contract,
        ServiceManifest manifest,
        RepairPolicySet policySet,
        Func<RepairTask, SafetyContext> safetyContextFactory,
        string correlationId,
        string? resolvedNetworkName = null,
        CancellationToken cancellationToken = default);
}

/// <summary>The single <see cref="IReconciliationOrchestrator"/> implementation (Sprint M2.6).</summary>
public sealed class ReconciliationOrchestrator : IReconciliationOrchestrator
{
    private readonly IDesiredStateProvider desiredStateProvider;
    private readonly IObservedStateDiscoveryEngine observedStateDiscoveryEngine;
    private readonly IDriftDetectionEngine driftDetectionEngine;
    private readonly IReconciliationPlanner reconciliationPlanner;
    private readonly ISelfHealingEngine selfHealingEngine;
    private readonly IReconciliationHistoryStore historyStore;

    public ReconciliationOrchestrator(
        IDesiredStateProvider desiredStateProvider,
        IObservedStateDiscoveryEngine observedStateDiscoveryEngine,
        IDriftDetectionEngine driftDetectionEngine,
        IReconciliationPlanner reconciliationPlanner,
        ISelfHealingEngine selfHealingEngine,
        IReconciliationHistoryStore historyStore)
    {
        this.desiredStateProvider = desiredStateProvider;
        this.observedStateDiscoveryEngine = observedStateDiscoveryEngine;
        this.driftDetectionEngine = driftDetectionEngine;
        this.reconciliationPlanner = reconciliationPlanner;
        this.selfHealingEngine = selfHealingEngine;
        this.historyStore = historyStore;
    }

    public async Task<ReconciliationResult> RunAsync(
        DeploymentPlan plan,
        InfrastructureContract contract,
        ServiceManifest manifest,
        RepairPolicySet policySet,
        Func<RepairTask, SafetyContext> safetyContextFactory,
        string correlationId,
        string? resolvedNetworkName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(policySet);
        ArgumentNullException.ThrowIfNull(safetyContextFactory);

        var startedUtc = DateTimeOffset.UtcNow;
        var events = new List<ReconciliationEvent>();

        var desired = desiredStateProvider.BuildDesiredState(plan, contract, manifest, startedUtc, resolvedNetworkName);
        Emit(events, ReconciliationEventType.ReconciliationStarted, desired.EnvironmentId, desired.ServiceId, correlationId, null, "Reconciliation cycle started.");

        var observed = await observedStateDiscoveryEngine.DiscoverAsync(desired, cancellationToken);
        var driftReport = driftDetectionEngine.DetectDrift(desired, observed, correlationId);

        if (driftReport.HasDrift)
        {
            Emit(events, ReconciliationEventType.DriftDetected, desired.EnvironmentId, desired.ServiceId, correlationId, null,
                $"{driftReport.Findings.Count} drift finding(s) detected.");
        }

        var reconciliationPlan = reconciliationPlanner.Plan(driftReport);
        var healingResult = await selfHealingEngine.HealAsync(reconciliationPlan, driftReport, policySet, safetyContextFactory, cancellationToken);
        events.AddRange(healingResult.Events);

        foreach (var resolved in healingResult.Outcomes.Where(o => o.Status == RepairOutcomeStatus.Executed && o.Succeeded == true))
        {
            Emit(events, ReconciliationEventType.DriftResolved, desired.EnvironmentId, desired.ServiceId, correlationId, resolved.Task.TaskId,
                $"Drift for {resolved.Task.ResourceType} '{resolved.Task.ResourceId}' resolved via {resolved.Task.ActionType}.");
        }

        var durationMs = (DateTimeOffset.UtcNow - startedUtc).TotalMilliseconds;
        Emit(events, ReconciliationEventType.ReconciliationCompleted, desired.EnvironmentId, desired.ServiceId, correlationId, null,
            $"Reconciliation cycle completed in {durationMs:0}ms.");

        await historyStore.AppendAsync(BuildHistoryRecord(desired, driftReport, healingResult, durationMs), cancellationToken);

        return new ReconciliationResult
        {
            DriftReport = driftReport,
            Plan = reconciliationPlan,
            Healing = healingResult,
            Events = events,
            DurationMs = durationMs
        };
    }

    private static ReconciliationHistoryRecord BuildHistoryRecord(
        DesiredState desired,
        DriftReport driftReport,
        SelfHealingResult healingResult,
        double durationMs)
        => new()
        {
            CorrelationId = driftReport.CorrelationId,
            EnvironmentId = desired.EnvironmentId,
            ServiceId = desired.ServiceId,
            TimestampUtc = DateTimeOffset.UtcNow,
            DriftFindingCount = driftReport.Findings.Count,
            DriftBySeverity = driftReport.Findings
                .GroupBy(f => f.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            RepairDecisions = healingResult.Outcomes
                .Select(o => new RepairDecisionRecord
                {
                    TaskId = o.Task.TaskId,
                    ActionType = o.Task.ActionType,
                    Decision = o.Status switch
                    {
                        RepairOutcomeStatus.Executed => PolicyDecision.Approve,
                        RepairOutcomeStatus.Rejected => PolicyDecision.Deny,
                        _ => PolicyDecision.RequireOperatorApproval
                    },
                    Executed = o.Status == RepairOutcomeStatus.Executed,
                    Succeeded = o.Succeeded,
                    Message = o.Message
                })
                .ToList(),
            OperatorApprovalsRequested = healingResult.Outcomes.Count(o => o.Status == RepairOutcomeStatus.PendingApproval),
            DurationMs = durationMs
        };

    private static void Emit(
        List<ReconciliationEvent> events,
        ReconciliationEventType type,
        string environmentId,
        string serviceId,
        string correlationId,
        string? taskId,
        string message)
        => events.Add(new ReconciliationEvent
        {
            Type = type,
            TimestampUtc = DateTimeOffset.UtcNow,
            EnvironmentId = environmentId,
            ServiceId = serviceId,
            CorrelationId = correlationId,
            TaskId = taskId,
            Message = message
        });
}
