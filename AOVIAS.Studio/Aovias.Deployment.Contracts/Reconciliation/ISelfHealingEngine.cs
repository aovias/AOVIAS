namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>Tracks whether an operator has approved a specific repair task (Sprint M2.6 D6). The default in-memory implementation is a safe test double; Runtime Agent's real implementation persists approvals durably and exposes them via an operator-facing API (out of this sprint's scope to build a UI for — see Technical Debt).</summary>
public interface IOperatorApprovalStore
{
    bool IsApproved(string taskId);

    void RequestApproval(string taskId, string description);
}

/// <summary>An in-memory, always-deny-until-explicitly-approved <see cref="IOperatorApprovalStore"/> — the safe default: nothing is ever approved unless something explicitly calls <c>Approve</c>.</summary>
public sealed class InMemoryOperatorApprovalStore : IOperatorApprovalStore
{
    private readonly HashSet<string> approved = new(StringComparer.Ordinal);
    private readonly object gate = new();

    public bool IsApproved(string taskId)
    {
        lock (gate)
        {
            return approved.Contains(taskId);
        }
    }

    public void RequestApproval(string taskId, string description)
    {
        // Requesting approval never grants it — it only records that a human should look. This
        // store's sole job is to remember what has been explicitly approved via Approve below.
    }

    public void Approve(string taskId)
    {
        lock (gate)
        {
            approved.Add(taskId);
        }
    }
}

/// <summary>What happened to one <see cref="RepairTask"/> during one <see cref="ISelfHealingEngine.HealAsync"/> call (Sprint M2.6 D6).</summary>
public enum RepairOutcomeStatus
{
    Executed,
    Rejected,
    PendingApproval
}

public sealed class RepairOutcome
{
    public required RepairTask Task { get; init; }

    public required RepairOutcomeStatus Status { get; init; }

    public bool? Succeeded { get; init; }

    public required string Message { get; init; }
}

/// <summary>The full outcome of one <see cref="ISelfHealingEngine.HealAsync"/> call (Sprint M2.6 D6), plus every <see cref="ReconciliationEvent"/> emitted along the way (D10).</summary>
public sealed class SelfHealingResult
{
    public IReadOnlyList<RepairOutcome> Outcomes { get; init; } = [];

    public IReadOnlyList<ReconciliationEvent> Events { get; init; } = [];

    public bool AllResolved => Outcomes.All(outcome => outcome.Status == RepairOutcomeStatus.Executed && outcome.Succeeded == true);
}

/// <summary>
/// Executes only policy-approved, safety-gated repairs from a <see cref="ReconciliationPlan"/>
/// (Sprint M2.6 D6). For every <see cref="RepairTask"/>, in the plan's order: evaluate
/// <see cref="IRepairPolicyEvaluator"/>, then <see cref="ReconciliationSafetyGate"/>; execute only
/// when both agree, via the matching <see cref="IRepairExecutor"/>; otherwise record a rejection
/// or a pending-approval outcome and move on to the next task — one task's rejection never blocks
/// another, independent task from being attempted (unlike Sprint M2.5's Execution Runtime, which
/// deliberately does stop on first failure — repairs are independent corrective actions, not a
/// dependent task chain). The sole implementation is <see cref="SelfHealingEngine"/>.
/// </summary>
public interface ISelfHealingEngine
{
    Task<SelfHealingResult> HealAsync(
        ReconciliationPlan plan,
        DriftReport driftReport,
        RepairPolicySet policySet,
        Func<RepairTask, SafetyContext> safetyContextFactory,
        CancellationToken cancellationToken = default);
}

/// <summary>The single <see cref="ISelfHealingEngine"/> implementation (Sprint M2.6 D6).</summary>
public sealed class SelfHealingEngine : ISelfHealingEngine
{
    private readonly IRepairPolicyEvaluator policyEvaluator;
    private readonly RepairExecutorRegistry executorRegistry;
    private readonly IOperatorApprovalStore approvalStore;

    public SelfHealingEngine(IRepairPolicyEvaluator policyEvaluator, RepairExecutorRegistry executorRegistry, IOperatorApprovalStore approvalStore)
    {
        this.policyEvaluator = policyEvaluator;
        this.executorRegistry = executorRegistry;
        this.approvalStore = approvalStore;
    }

    public async Task<SelfHealingResult> HealAsync(
        ReconciliationPlan plan,
        DriftReport driftReport,
        RepairPolicySet policySet,
        Func<RepairTask, SafetyContext> safetyContextFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(driftReport);
        ArgumentNullException.ThrowIfNull(policySet);
        ArgumentNullException.ThrowIfNull(safetyContextFactory);

        var outcomes = new List<RepairOutcome>();
        var events = new List<ReconciliationEvent>();
        var now = DateTimeOffset.UtcNow;

        foreach (var task in plan.RepairTasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var finding = driftReport.Findings.FirstOrDefault(f => f.FindingId == task.FindingId);
            if (finding is null)
            {
                continue; // Cannot evaluate a task whose finding is missing — skip rather than guess.
            }

            var decision = policyEvaluator.Evaluate(task, finding, policySet, now);
            var safety = ReconciliationSafetyGate.Evaluate(task, safetyContextFactory(task));

            if (decision == PolicyDecision.Deny)
            {
                outcomes.Add(new RepairOutcome { Task = task, Status = RepairOutcomeStatus.Rejected, Message = "Repair policy denies this action for this environment." });
                Emit(events, ReconciliationEventType.RepairRejected, plan, task.TaskId, "Repair policy denied this action.");
                continue;
            }

            var needsApproval = decision == PolicyDecision.RequireOperatorApproval || !safety.Safe;
            if (needsApproval && !approvalStore.IsApproved(task.TaskId))
            {
                approvalStore.RequestApproval(task.TaskId, task.Description);
                outcomes.Add(new RepairOutcome
                {
                    Task = task,
                    Status = RepairOutcomeStatus.PendingApproval,
                    Message = safety.Safe ? "Awaiting operator approval per repair policy." : $"Awaiting operator approval: {safety.Reason}"
                });
                Emit(events, ReconciliationEventType.OperatorApprovalRequired, plan, task.TaskId, safety.Safe ? "Policy requires operator approval." : safety.Reason!);
                continue;
            }

            Emit(events, ReconciliationEventType.RepairStarted, plan, task.TaskId, $"Executing {task.ActionType} for {task.ResourceType} '{task.ResourceId}'.");
            var executor = executorRegistry.Resolve(task.ActionType);
            RepairExecutionOutcome executionOutcome;
            try
            {
                executionOutcome = await executor.ExecuteAsync(task, finding, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                executionOutcome = RepairExecutionOutcome.Failure($"Repair executor threw: {exception.Message}");
            }

            outcomes.Add(new RepairOutcome { Task = task, Status = RepairOutcomeStatus.Executed, Succeeded = executionOutcome.Succeeded, Message = executionOutcome.Message });
            Emit(events, ReconciliationEventType.RepairCompleted, plan, task.TaskId, executionOutcome.Message);
        }

        return new SelfHealingResult { Outcomes = outcomes, Events = events };
    }

    private static void Emit(List<ReconciliationEvent> events, ReconciliationEventType type, ReconciliationPlan plan, string? taskId, string message)
        => events.Add(new ReconciliationEvent
        {
            Type = type,
            TimestampUtc = DateTimeOffset.UtcNow,
            EnvironmentId = plan.EnvironmentId,
            ServiceId = plan.ServiceId,
            CorrelationId = plan.CorrelationId,
            TaskId = taskId,
            Message = message
        });
}
