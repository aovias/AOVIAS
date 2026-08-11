using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// Handles Preflight and Completion phase tasks (Sprint M2.5 D3): these restate decisions already
/// made upstream (Preflight: admission already approved this deployment; Completion: registering
/// the completed deployment in the audit trail is real work, but it is deliberately left to the
/// existing <c>ReleaseDeploymentProvider.RegisterDeploymentAsync</c> audit call rather than
/// duplicated here — see the M2.5 report's Technical Debt section). This provisioner's job is to
/// give both phases a real, observable, idempotent task record in the execution history, not to
/// perform fresh infrastructure work.
/// </summary>
public sealed class ConfirmationTaskProvisioner : IInfrastructureTaskProvisioner
{
    public bool CanHandle(DeploymentTask task)
        => task.TaskId.StartsWith("preflight.", StringComparison.OrdinalIgnoreCase)
           || task.TaskId.StartsWith("completion.", StringComparison.OrdinalIgnoreCase);

    public Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(TaskExecutionOutcome.Completed($"Confirmed: {task.Description}"));

    public Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord) => true;

    public bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord) => true;

    public Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}
