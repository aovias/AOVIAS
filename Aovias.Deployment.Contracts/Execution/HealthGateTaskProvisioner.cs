using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// Handles Health phase tasks (Sprint M2.5 D11): each task is paired with the
/// <see cref="HealthGate"/> it verifies by naming convention — a task id
/// <c>health.verify-primary-probe</c> pairs with gate id <c>primary-health-probe</c>, and
/// <c>health.verify-additional-probe-{n}</c> pairs with <c>additional-health-probe-{n}</c>
/// (the exact ids <see cref="Planning.DeploymentPlanBuilder"/> assigns to both). A failed gate is
/// classified <see cref="FailureClassification.Recoverable"/> — the deployment should stop and
/// wait to be resumed once the underlying health issue is fixed, not retried blindly or treated
/// as fatal.
/// </summary>
public sealed class HealthGateTaskProvisioner : IInfrastructureTaskProvisioner
{
    private readonly IExecutionGateCheck gateCheck;

    public HealthGateTaskProvisioner(IExecutionGateCheck gateCheck) => this.gateCheck = gateCheck;

    public bool CanHandle(DeploymentTask task) => task.TaskId.StartsWith("health.", StringComparison.OrdinalIgnoreCase);

    public async Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        var gateId = ResolveGateId(task.TaskId);
        var result = await gateCheck.CheckAsync(gateId, context, cancellationToken);
        return result.Passed
            ? TaskExecutionOutcome.Completed($"Health gate '{gateId}' passed.")
            : TaskExecutionOutcome.Failed(
                $"Health gate '{gateId}' failed: {result.FailureReason ?? "no reason supplied"}.",
                FailureClassification.Recoverable);
    }

    public async Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => (await gateCheck.CheckAsync(ResolveGateId(task.TaskId), context, cancellationToken)).Passed;

    public bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord) => true;

    /// <summary>
    /// A failed health gate is classified <see cref="FailureClassification.Recoverable"/> (D11:
    /// FAIL → Recovery), not <see cref="FailureClassification.Retryable"/> — the Execution
    /// Runtime's automatic in-call retry loop only consults <see cref="CanRetry"/> for
    /// <c>Retryable</c> failures, so this value governs a caller's own decision (e.g. an operator
    /// or a resume-scheduler) about whether calling <see cref="IDeploymentExecutionRuntime.ResumeAsync"/>
    /// again is still worthwhile, capped at three total attempts.
    /// </summary>
    public bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord) => priorRecord.Attempts < 3;

    public Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    private static string ResolveGateId(string taskId)
    {
        var suffix = taskId["health.".Length..];
        return suffix switch
        {
            "verify-primary-probe" => "primary-health-probe",
            _ when suffix.StartsWith("verify-additional-probe-", StringComparison.OrdinalIgnoreCase)
                => "additional-health-probe-" + suffix["verify-additional-probe-".Length..],
            _ => suffix
        };
    }
}
