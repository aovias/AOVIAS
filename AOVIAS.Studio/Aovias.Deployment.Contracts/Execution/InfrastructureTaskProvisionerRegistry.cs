using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// Dispatches each <see cref="DeploymentTask"/> to exactly one <see cref="IInfrastructureTaskProvisioner"/>
/// (Sprint M2.5 D5). Provisioners are tried in registration order; the first whose
/// <see cref="IInfrastructureTaskProvisioner.CanHandle"/> returns <see langword="true"/> wins. A
/// task no registered provisioner claims falls back to <see cref="NoOpTaskProvisioner"/> rather
/// than throwing — every task must resolve to *some* provisioner so the runtime can always record
/// a result, never silently drop a task.
/// </summary>
public sealed class InfrastructureTaskProvisionerRegistry
{
    private readonly IReadOnlyList<IInfrastructureTaskProvisioner> provisioners;
    private readonly IInfrastructureTaskProvisioner fallback;

    public InfrastructureTaskProvisionerRegistry(IReadOnlyList<IInfrastructureTaskProvisioner> provisioners, IInfrastructureTaskProvisioner? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(provisioners);
        this.provisioners = provisioners;
        this.fallback = fallback ?? new NoOpTaskProvisioner();
    }

    public IInfrastructureTaskProvisioner Resolve(DeploymentTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return provisioners.FirstOrDefault(provisioner => provisioner.CanHandle(task)) ?? fallback;
    }
}

/// <summary>
/// The always-available fallback provisioner (Sprint M2.5 D5). Marks any task no other
/// provisioner claims as <see cref="TaskExecutionStatus.Skipped"/> — never <c>Completed</c>,
/// since falsely claiming success for work nothing actually performed would be dishonest
/// observability, and never <c>Failed</c>, since an un-owned task is a configuration gap in the
/// registry, not a deployment failure.
/// </summary>
public sealed class NoOpTaskProvisioner : IInfrastructureTaskProvisioner
{
    public bool CanHandle(DeploymentTask task) => true;

    public Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(TaskExecutionOutcome.Skipped($"No provisioner is registered for task '{task.TaskId}'. Skipped without action."));

    public Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord) => true;

    public bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord) => false;

    public Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}
