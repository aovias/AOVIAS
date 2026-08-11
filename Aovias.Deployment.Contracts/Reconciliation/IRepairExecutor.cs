namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>The outcome of one <see cref="IRepairExecutor.ExecuteAsync"/> call (Sprint M2.6 D6).</summary>
public sealed class RepairExecutionOutcome
{
    public required bool Succeeded { get; init; }

    public required string Message { get; init; }

    public static RepairExecutionOutcome Success(string message) => new() { Succeeded = true, Message = message };

    public static RepairExecutionOutcome Failure(string message) => new() { Succeeded = false, Message = message };
}

/// <summary>
/// Performs one concrete <see cref="RepairActionType"/> (Sprint M2.6 D6). Mirrors the shape of
/// Sprint M2.5's <c>IInfrastructureTaskProvisioner</c> deliberately — same
/// <c>CanHandle</c>/dispatch-by-registry pattern, applied here to repair actions instead of
/// deployment tasks. Kept free of any dependency on Runtime Agent's concrete providers; real
/// implementations live in <c>Aovias.Runtime.Agent.Infrastructure.Reconciliation</c>.
/// Implementations must be idempotent (repairing an already-healthy resource must be a safe
/// no-op) and must never execute anything the caller (<see cref="ISelfHealingEngine"/>) did not
/// already approve — an executor does not itself decide whether to run, only how.
/// </summary>
public interface IRepairExecutor
{
    bool CanHandle(RepairActionType actionType);

    Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default);
}

/// <summary>First-match-wins dispatch for <see cref="IRepairExecutor"/>, with a safe, always-available fallback (Sprint M2.6 D6) — mirrors <c>Execution.InfrastructureTaskProvisionerRegistry</c> from Sprint M2.5.</summary>
public sealed class RepairExecutorRegistry
{
    private readonly IReadOnlyList<IRepairExecutor> executors;
    private readonly IRepairExecutor fallback;

    public RepairExecutorRegistry(IReadOnlyList<IRepairExecutor> executors, IRepairExecutor? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(executors);
        this.executors = executors;
        this.fallback = fallback ?? new NoOpRepairExecutor();
    }

    public IRepairExecutor Resolve(RepairActionType actionType)
        => executors.FirstOrDefault(executor => executor.CanHandle(actionType)) ?? fallback;
}

/// <summary>The always-available fallback (Sprint M2.6 D6) — honestly reports failure rather than fabricating a repair no executor actually performed.</summary>
public sealed class NoOpRepairExecutor : IRepairExecutor
{
    public bool CanHandle(RepairActionType actionType) => true;

    public Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
        => Task.FromResult(RepairExecutionOutcome.Failure($"No repair executor is registered for action type '{task.ActionType}'."));
}
