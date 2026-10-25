using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// Handles Upload, Configuration, and Deployment phase tasks (Sprint M2.5 D3) whose real
/// execution — extracting a release, staging it, loading a Docker image, backing up and
/// activating a release, starting services — continues to be performed by the existing,
/// already-working, externally-orchestrated <c>ReleaseDeploymentProvider</c> step methods
/// (<c>ExtractReleaseAsync</c>, <c>StageReleaseAsync</c>, <c>LoadDockerImageAsync</c>,
/// <c>BackupCurrentReleaseAsync</c>, <c>ActivateReleaseAsync</c>), called in sequence by
/// <c>Aovias.Orchestration</c> outside this runtime.
///
/// This is a deliberate, documented scope boundary (see the M2.5 report's Technical Debt and
/// Readiness Assessment sections), not an oversight: fully subsuming that external step sequence
/// into this runtime would mean redesigning <c>Aovias.Orchestration</c>'s workflow steps, which is
/// a materially larger and riskier change than this sprint's D1–D11 abstractions, cannot be
/// verified without a real compiler and test run (unavailable in this sandbox — see the
/// Regression Results section), and risks breaking the one deployment path that already works
/// today. This provisioner honestly reports <see cref="TaskExecutionStatus.Skipped"/> — never
/// <c>Completed</c> — for these tasks, so nothing downstream can mistake "not yet wired to this
/// runtime" for "verified done."
/// </summary>
public sealed class PendingExternalExecutionTaskProvisioner : IInfrastructureTaskProvisioner
{
    public bool CanHandle(DeploymentTask task)
        => task.TaskId.StartsWith("upload.", StringComparison.OrdinalIgnoreCase)
           || task.TaskId.StartsWith("configuration.", StringComparison.OrdinalIgnoreCase)
           || task.TaskId.StartsWith("deployment.", StringComparison.OrdinalIgnoreCase);

    public Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(TaskExecutionOutcome.Skipped(
            $"Task '{task.TaskId}' is executed by the existing external deployment orchestrator, not yet by the Execution Runtime (Sprint M2.5 Technical Debt)."));

    public Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord) => true;

    public bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord) => false;

    public Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;
}
