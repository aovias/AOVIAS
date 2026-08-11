using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>
/// The single implementation of <see cref="IDeploymentExecutionRuntime"/> (Sprint M2.5 D1). Walks
/// <see cref="DeploymentPlan.ExecutionPhases"/> — in the exact order the plan declares them,
/// never a hardcoded order of its own (D3: "Ordering comes exclusively from the Deployment Plan")
/// — dispatching each <see cref="DeploymentTask"/> to a provisioner resolved by
/// <see cref="InfrastructureTaskProvisionerRegistry"/> (D4/D5), retrying
/// <see cref="FailureClassification.Retryable"/> failures automatically up to a fixed attempt
/// ceiling, classifying every other failure, and stopping — never continuing past a failed task —
/// the moment one occurs. Every phase start/end and task start/end/retry/failure is recorded as a
/// structured <see cref="ExecutionEvent"/> (D9) and folded into <see cref="ExecutionProgress"/> (D8).
/// </summary>
public sealed class DeploymentExecutionRuntime : IDeploymentExecutionRuntime
{
    public const int DefaultMaxRetries = 3;

    private readonly InfrastructureTaskProvisionerRegistry registry;
    private readonly int maxRetries;

    public DeploymentExecutionRuntime(InfrastructureTaskProvisionerRegistry registry, int maxRetries = DefaultMaxRetries)
    {
        ArgumentNullException.ThrowIfNull(registry);
        this.registry = registry;
        this.maxRetries = maxRetries;
    }

    public Task<ExecutionResult> ExecuteAsync(DeploymentExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RunAsync(context, cancellationToken);
    }

    public Task<ExecutionResult> ResumeAsync(DeploymentExecutionContext context, ExecutionHistory priorHistory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(priorHistory);

        foreach (var record in priorHistory.TaskRecords)
        {
            context.History.AddOrUpdateRecord(record);
        }

        foreach (var priorEvent in priorHistory.Events)
        {
            context.History.AddEvent(priorEvent);
        }

        context.History.AddEvent(new ExecutionEvent
        {
            Type = ExecutionEventType.ResumeStarted,
            TimestampUtc = DateTimeOffset.UtcNow,
            Message = $"Resuming execution; last successful task was '{priorHistory.LastSuccessfulTaskId ?? "<none>"}'."
        });

        return RunAsync(context, cancellationToken);
    }

    private async Task<ExecutionResult> RunAsync(DeploymentExecutionContext context, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var totalTasks = context.Plan.ExecutionPhases.Sum(phase => phase.Tasks.Count);

        foreach (var phase in context.Plan.ExecutionPhases.OrderBy(phase => phase.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.CurrentPhase = phase.Name;
            Emit(context, ExecutionEventType.PhaseStarted, phase.Name, null, $"Phase '{phase.Name}' started.");

            foreach (var task in phase.Tasks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var provisioner = registry.Resolve(task);
                var priorRecord = context.History.FindRecord(task.TaskId);

                if (priorRecord?.Status == TaskExecutionStatus.Completed && provisioner.CanResume(task, priorRecord))
                {
                    // Already done, and the provisioner trusts the recorded state (D7) — every
                    // task must be idempotent regardless, so re-running would also be safe, but
                    // skipping is cheaper and is exactly what "no completed task executes twice"
                    // (D7) means.
                    continue;
                }

                context.CurrentTaskId = task.TaskId;
                Emit(context, ExecutionEventType.TaskStarted, phase.Name, task.TaskId, $"Task '{task.TaskId}' started.");

                var record = priorRecord ?? new TaskExecutionRecord
                {
                    TaskId = task.TaskId,
                    Status = TaskExecutionStatus.Running,
                    StartedUtc = DateTimeOffset.UtcNow
                };
                record.Status = TaskExecutionStatus.Running;

                var outcome = await ExecuteWithRetryAsync(provisioner, task, context, record, phase.Name, cancellationToken);
                context.Diagnostics.AddRange(outcome.Warnings);

                if (outcome.Status == TaskExecutionStatus.Failed)
                {
                    record.Status = TaskExecutionStatus.Failed;
                    record.CompletedUtc = DateTimeOffset.UtcNow;
                    record.LastError = outcome.Message;
                    record.LastFailureClassification = outcome.FailureClassification;
                    context.History.AddOrUpdateRecord(record);

                    errors.Add(outcome.Message);
                    Emit(context, ExecutionEventType.FailureDetected, phase.Name, task.TaskId, outcome.Message);
                    await SafeCleanupAsync(provisioner, task, context, cancellationToken);

                    var canResume = outcome.FailureClassification != FailureClassification.Fatal;
                    return BuildResult(context, success: false, errors, canResume, totalTasks);
                }

                record.Status = outcome.Status;
                record.CompletedUtc = DateTimeOffset.UtcNow;
                context.History.AddOrUpdateRecord(record);

                Emit(context, ExecutionEventType.TaskCompleted, phase.Name, task.TaskId, outcome.Message);

                if (phase.Name == DeploymentExecutionPhaseName.Health && outcome.Status == TaskExecutionStatus.Completed)
                {
                    Emit(context, ExecutionEventType.HealthVerified, phase.Name, task.TaskId, $"Health check for task '{task.TaskId}' verified.");
                }
            }

            Emit(context, ExecutionEventType.PhaseCompleted, phase.Name, null, $"Phase '{phase.Name}' completed.");

            if (phase.Name == DeploymentExecutionPhaseName.Certification)
            {
                Emit(context, ExecutionEventType.CertificationCompleted, phase.Name, null, "Certification phase completed.");
            }
        }

        return BuildResult(context, success: true, errors, canResume: true, totalTasks);
    }

    private async Task<TaskExecutionOutcome> ExecuteWithRetryAsync(
        IInfrastructureTaskProvisioner provisioner,
        DeploymentTask task,
        DeploymentExecutionContext context,
        TaskExecutionRecord record,
        DeploymentExecutionPhaseName phase,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            record.Attempts++;
            var outcome = await ExecuteOneAttemptAsync(provisioner, task, context, cancellationToken);

            if (outcome.Status != TaskExecutionStatus.Failed)
            {
                return outcome;
            }

            var canRetryAutomatically =
                outcome.FailureClassification == FailureClassification.Retryable
                && record.Attempts < maxRetries
                && provisioner.CanRetry(task, record);

            if (!canRetryAutomatically)
            {
                return outcome;
            }

            record.LastError = outcome.Message;
            record.LastFailureClassification = outcome.FailureClassification;
            Emit(context, ExecutionEventType.RetryStarted, phase, task.TaskId, $"Retrying task '{task.TaskId}' (attempt {record.Attempts + 1} of {maxRetries}) after: {outcome.Message}");
        }
    }

    private static async Task<TaskExecutionOutcome> ExecuteOneAttemptAsync(
        IInfrastructureTaskProvisioner provisioner,
        DeploymentTask task,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return await provisioner.ExecuteTaskAsync(task, context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return TaskExecutionOutcome.Failed($"Task '{task.TaskId}' threw an unhandled exception: {exception.Message}", FailureClassification.Recoverable);
        }
    }

    private static async Task SafeCleanupAsync(
        IInfrastructureTaskProvisioner provisioner,
        DeploymentTask task,
        DeploymentExecutionContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await provisioner.CleanupAsync(task, context, cancellationToken);
        }
        catch
        {
            // Cleanup is advisory only — a cleanup failure must never mask the real failure that
            // triggered it (see IInfrastructureTaskProvisioner.CleanupAsync).
        }
    }

    private static void Emit(
        DeploymentExecutionContext context,
        ExecutionEventType type,
        DeploymentExecutionPhaseName? phase,
        string? taskId,
        string message)
        => context.History.AddEvent(new ExecutionEvent
        {
            Type = type,
            TimestampUtc = DateTimeOffset.UtcNow,
            Phase = phase,
            TaskId = taskId,
            Message = message
        });

    private static ExecutionResult BuildResult(
        DeploymentExecutionContext context,
        bool success,
        List<string> errors,
        bool canResume,
        int totalTasks)
    {
        var completedRecords = context.History.TaskRecords.Where(record => record.Status == TaskExecutionStatus.Completed).ToList();
        var retryCount = context.History.TaskRecords.Sum(record => Math.Max(0, record.Attempts - 1));
        var duration = DateTimeOffset.UtcNow - context.StartedUtc;
        var percentComplete = totalTasks == 0 ? 100d : Math.Round(100d * completedRecords.Count / totalTasks, 2);

        TimeSpan? estimatedRemaining = null;
        if (completedRecords.Count > 0 && completedRecords.Count < totalTasks)
        {
            var averageMs = completedRecords.Average(record => record.DurationMs);
            estimatedRemaining = TimeSpan.FromMilliseconds(averageMs * (totalTasks - completedRecords.Count));
        }

        var progress = new ExecutionProgress
        {
            CurrentPhase = context.CurrentPhase,
            CurrentTaskId = context.CurrentTaskId,
            PercentComplete = percentComplete,
            Duration = duration,
            EstimatedRemaining = estimatedRemaining,
            Warnings = context.Diagnostics.ToList(),
            Errors = errors,
            RetryCount = retryCount
        };

        return new ExecutionResult
        {
            Success = success,
            History = context.History,
            Progress = progress,
            Errors = errors,
            CanResume = canResume
        };
    }
}
