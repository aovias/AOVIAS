namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// Executes an already-approved, already-validated <see cref="Planning.DeploymentPlan"/> (Sprint
/// M2.5 D1). This is the one and only place a plan's tasks actually run. It never generates a
/// plan itself (Final Engineering Rule 2: the Planner decides what should happen; this runtime
/// performs only that approved work — Final Engineering Rule 3) and it never asks whether the
/// deployment is allowed (Final Engineering Rule 1 — that question was already answered before
/// the plan in <see cref="DeploymentExecutionContext.Plan"/> was ever built).
/// </summary>
public interface IDeploymentExecutionRuntime
{
    /// <summary>Executes <paramref name="context"/>'s plan from the beginning.</summary>
    Task<ExecutionResult> ExecuteAsync(DeploymentExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously started (and, presumably, interrupted) execution using
    /// <paramref name="priorHistory"/> — the durable record of what already completed (Sprint
    /// M2.5 D7). Every task <paramref name="priorHistory"/> already marks
    /// <see cref="TaskExecutionStatus.Completed"/> is skipped rather than re-executed; execution
    /// continues from the first task that is not yet completed.
    /// </summary>
    Task<ExecutionResult> ResumeAsync(DeploymentExecutionContext context, ExecutionHistory priorHistory, CancellationToken cancellationToken = default);
}
