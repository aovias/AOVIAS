using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// The standardized, per-task provisioner contract the Execution Runtime dispatches every
/// <see cref="DeploymentTask"/> through (Sprint M2.5 D5).
///
/// This is a distinct interface from the pre-existing <see cref="Infrastructure.IInfrastructureProvisioner"/>
/// (Sprint M2.0–M2.4, unchanged, still the sole interface <c>InfrastructureProvisioningPipeline</c> and the
/// shared DRV/Runtime Agent pipeline stage use), deliberately not a redesign of it. That older
/// interface models one coarse-grained readiness *stage* over an entire bundle; this one models
/// one <see cref="DeploymentTask"/> at a time, with the fuller lifecycle (execute, validate,
/// resume, retry, cleanup, progress) the M2.5 brief asks for. See the M2.5 report's Technical
/// Debt section for why both now coexist, mirroring the same judgment call M2.4 made for its two
/// "planner" concepts.
///
/// Implementations must never evaluate admission rules (Final Engineering Rule 4) — a
/// provisioner that finds itself checking whether a requirement is satisfied, or whether an
/// environment is registered, has overstepped its role. That decision was already made, upstream,
/// by <see cref="Admission.IDeploymentAdmissionEngine"/> before the plan this task belongs to was
/// ever built.
/// </summary>
public interface IInfrastructureTaskProvisioner
{
    /// <summary>Whether this provisioner is responsible for <paramref name="task"/>. The registry (<see cref="InfrastructureTaskProvisionerRegistry"/>) uses this to dispatch — exactly one provisioner should claim any given task.</summary>
    bool CanHandle(DeploymentTask task);

    /// <summary>
    /// Performs the task's work. Must be idempotent (Sprint M2.5 D6): calling this twice for the
    /// same task against the same already-provisioned infrastructure must not corrupt anything or
    /// fail merely because the work was already done.
    /// </summary>
    Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken);

    /// <summary>Independently confirms the task's declared outcome actually holds (e.g. the directory really exists now) — a second, cheaper check distinct from re-running the task.</summary>
    Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken);

    /// <summary>Whether this task can be safely skipped on resume given <paramref name="priorRecord"/> (Sprint M2.5 D7). Almost always <see langword="true"/> when <paramref name="priorRecord"/> is <see cref="TaskExecutionStatus.Completed"/>, since every task must be idempotent regardless — this hook exists for the rare provisioner that cannot trust a stale completed record (e.g. one whose target could have been altered externally) and wants re-validation instead of a bare skip.</summary>
    bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord);

    /// <summary>Whether the Execution Runtime may retry this task automatically after <paramref name="priorRecord"/>'s failure, before escalating to the caller.</summary>
    bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord);

    /// <summary>Best-effort cleanup after a Fatal failure (e.g. removing a partially-written file). Never throws — the runtime treats cleanup as advisory.</summary>
    Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken);

    /// <summary>Called by the runtime immediately before and after <see cref="ExecuteTaskAsync"/> so a provisioner can contribute to <see cref="ExecutionProgress"/> reporting (e.g. sub-task percentage for a long-running task). The default, do-nothing implementation is sufficient for every provisioner in this sprint.</summary>
    void ReportProgress(DeploymentTask task, ExecutionProgress progress)
    {
        _ = task;
        _ = progress;
    }
}
