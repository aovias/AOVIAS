using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// Handles Infrastructure phase tasks (Sprint M2.5 D3/D5) by adapting the existing, already
/// working, already idempotent <see cref="InfrastructureProvisioningPipeline"/> (Sprint M2.0–M2.3)
/// rather than reimplementing filesystem, certificate, secret, and dependency provisioning a
/// second time inside the new Execution Runtime — a real re-implementation could not be verified
/// without a compiler in this sandbox (see the M2.5 report's Regression Results section), so
/// reusing proven logic was the lower-risk choice.
///
/// The caller (typically Runtime Agent's own composition of <see cref="DeploymentExecutionContext"/>)
/// must seed <see cref="DeploymentExecutionContext.ProvisionerState"/> with an
/// <see cref="InfrastructureProvisioningContext"/> under the <see cref="ProvisioningContextStateKey"/>
/// key before execution reaches the Infrastructure phase — this is the bridge between the M2.5
/// execution context (service id, environment id, tracking state) and the M2.0-era provisioning
/// context (bundle, host paths, runtime capabilities) the legacy pipeline actually requires. If
/// absent, every Infrastructure task fails with a clear, actionable message rather than throwing.
///
/// The underlying pipeline runs at most once per execution (its report is cached in
/// <see cref="DeploymentExecutionContext.ProvisionerState"/>); every task dispatched to this
/// provisioner within the same run reads from that one cached report instead of re-running the
/// whole pipeline per task.
/// </summary>
public sealed class ExistingInfrastructurePipelineTaskProvisioner : IInfrastructureTaskProvisioner
{
    public const string ProvisioningContextStateKey = "infrastructureProvisioningContext";
    private const string ReadinessReportStateKey = "infrastructureReadinessReport";

    public bool CanHandle(DeploymentTask task) => task.TaskId.StartsWith("infrastructure.", StringComparison.OrdinalIgnoreCase);

    public async Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        if (!context.ProvisionerState.TryGetValue(ProvisioningContextStateKey, out var raw) || raw is not InfrastructureProvisioningContext provisioningContext)
        {
            return TaskExecutionOutcome.Failed(
                $"No InfrastructureProvisioningContext was supplied for task '{task.TaskId}'. The caller must seed DeploymentExecutionContext.ProvisionerState['{ProvisioningContextStateKey}'] before execution reaches the Infrastructure phase.",
                FailureClassification.Fatal);
        }

        InfrastructureReadinessReport report;
        if (context.ProvisionerState.TryGetValue(ReadinessReportStateKey, out var cached) && cached is InfrastructureReadinessReport cachedReport)
        {
            report = cachedReport;
        }
        else
        {
            report = await InfrastructureProvisioningPipeline.ExecuteAsync(provisioningContext, cancellationToken);
            context.ProvisionerState[ReadinessReportStateKey] = report;
        }

        var stageName = ResolveStageName(task.TaskId);
        var stage = report.Stages.FirstOrDefault(s => string.Equals(s.Stage, stageName, StringComparison.OrdinalIgnoreCase));

        if (stage is null)
        {
            // The legacy pipeline stopped before reaching this stage (it fails fast, like the
            // Execution Runtime itself) — treat as not-yet-attempted rather than success.
            return TaskExecutionOutcome.Failed(
                $"Infrastructure stage '{stageName}' for task '{task.TaskId}' did not run — an earlier stage in the pipeline blocked first: {string.Join("; ", report.Blockers)}",
                FailureClassification.Recoverable);
        }

        if (!stage.Passed)
        {
            return TaskExecutionOutcome.Failed(
                $"Infrastructure stage '{stageName}' failed for task '{task.TaskId}': {string.Join("; ", stage.Errors)}",
                FailureClassification.Recoverable);
        }

        return TaskExecutionOutcome.Completed($"Infrastructure stage '{stageName}' passed for task '{task.TaskId}'.", stage.Messages);
    }

    public Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        if (context.ProvisionerState.TryGetValue(ReadinessReportStateKey, out var cached) && cached is InfrastructureReadinessReport report)
        {
            var stageName = ResolveStageName(task.TaskId);
            return Task.FromResult(report.Stages.Any(s => string.Equals(s.Stage, stageName, StringComparison.OrdinalIgnoreCase) && s.Passed));
        }

        return Task.FromResult(false);
    }

    /// <summary>The underlying pipeline stages (Filesystem, Certificates, Secrets, Database, Providers, Dependencies, ...) are already idempotent (Sprint M2.0–M2.3) — safe to resume without re-validating.</summary>
    public bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord) => true;

    public bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord) => priorRecord.Attempts < 2;

    public Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    private static string ResolveStageName(string taskId)
    {
        var suffix = taskId["infrastructure.".Length..];
        return suffix switch
        {
            "create-filesystem-directories" => "Filesystem",
            _ when suffix.StartsWith("provision-certificate-", StringComparison.OrdinalIgnoreCase) => "Certificates",
            _ when suffix.StartsWith("provision-secret-", StringComparison.OrdinalIgnoreCase) => "Secrets",
            _ when suffix.StartsWith("satisfy-requirement-", StringComparison.OrdinalIgnoreCase) => "Dependencies",
            _ => suffix
        };
    }
}
