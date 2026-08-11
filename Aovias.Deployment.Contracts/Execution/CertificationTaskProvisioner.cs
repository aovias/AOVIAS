using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// Handles Certification phase tasks (Sprint M2.5 D3/D9): the plan's single
/// <c>certification.verify-acceptance-gates</c> task checks every
/// <see cref="CertificationGate"/> the plan declares via the same <see cref="IExecutionGateCheck"/>
/// abstraction <see cref="HealthGateTaskProvisioner"/> uses. Emits the
/// <see cref="ExecutionEventType.CertificationCompleted"/> event on success — see
/// <see cref="DeploymentExecutionRuntime"/>.
/// </summary>
public sealed class CertificationTaskProvisioner : IInfrastructureTaskProvisioner
{
    private readonly IExecutionGateCheck gateCheck;

    public CertificationTaskProvisioner(IExecutionGateCheck gateCheck) => this.gateCheck = gateCheck;

    public bool CanHandle(DeploymentTask task) => task.TaskId.StartsWith("certification.", StringComparison.OrdinalIgnoreCase);

    public async Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        var failures = new List<string>();
        foreach (var gate in context.Plan.CertificationGates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await gateCheck.CheckAsync(gate.Id, context, cancellationToken);
            if (!result.Passed)
            {
                failures.Add($"{gate.Id}: {result.FailureReason ?? "no reason supplied"}");
            }
        }

        return failures.Count == 0
            ? TaskExecutionOutcome.Completed("All certification gates passed.")
            : TaskExecutionOutcome.Failed(
                $"Certification gate(s) failed: {string.Join("; ", failures)}",
                FailureClassification.OperatorActionRequired);
    }

    public async Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        foreach (var gate in context.Plan.CertificationGates)
        {
            if (!(await gateCheck.CheckAsync(gate.Id, context, cancellationToken)).Passed)
            {
                return false;
            }
        }

        return true;
    }

    public bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord) => true;

    public bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord) => priorRecord.Attempts < 2;

    public Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}
