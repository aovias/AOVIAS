using Aovias.Deployment.Contracts.Planning;

namespace Aovias.Deployment.Contracts.Execution;

/// <summary>The lifecycle state of a single <see cref="DeploymentTask"/> within an execution run (Sprint M2.5 D4).</summary>
public enum TaskExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// How a task failure should be handled (Sprint M2.5 D10). Recovery guidance originates from the
/// <see cref="DeploymentPlan"/> where applicable (e.g. a task's phase has a
/// <see cref="RollbackCheckpoint"/> declared before it) — this classification decides whether the
/// Execution Runtime should retry automatically, stop and wait to be resumed, stop and require an
/// operator, or stop unrecoverably.
/// </summary>
public enum FailureClassification
{
    /// <summary>Transient — retrying the same task with no other change is expected to succeed eventually.</summary>
    Retryable,

    /// <summary>Not transient, but resuming (possibly after an operator or automated fix outside this task) is expected to succeed.</summary>
    Recoverable,

    /// <summary>Not retryable and not recoverable by resuming — the deployment cannot proceed.</summary>
    Fatal,

    /// <summary>Recoverable, but only after a human takes a specific corrective action described in the diagnostic.</summary>
    OperatorActionRequired
}

/// <summary>The structured event vocabulary the Execution Runtime emits (Sprint M2.5 D9). These are the foundation for monitoring and auditing — there is no unstructured logging path in the runtime itself.</summary>
public enum ExecutionEventType
{
    PhaseStarted,
    PhaseCompleted,
    TaskStarted,
    TaskCompleted,
    RetryStarted,
    ResumeStarted,
    FailureDetected,
    HealthVerified,
    CertificationCompleted
}

/// <summary>A single structured execution event (Sprint M2.5 D9).</summary>
public sealed class ExecutionEvent
{
    public required ExecutionEventType Type { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    public DeploymentExecutionPhaseName? Phase { get; init; }

    public string? TaskId { get; init; }

    public required string Message { get; init; }
}

/// <summary>
/// The durable record of one task's execution attempts within an <see cref="ExecutionHistory"/>
/// (Sprint M2.5 D7/D8). This is what makes resume possible: a task with a
/// <see cref="TaskExecutionStatus.Completed"/> record is never re-executed on resume.
/// </summary>
public sealed class TaskExecutionRecord
{
    public required string TaskId { get; init; }

    public required TaskExecutionStatus Status { get; set; }

    public int Attempts { get; set; }

    public required DateTimeOffset StartedUtc { get; set; }

    public DateTimeOffset? CompletedUtc { get; set; }

    public string? LastError { get; set; }

    public FailureClassification? LastFailureClassification { get; set; }

    public double DurationMs => (CompletedUtc ?? DateTimeOffset.UtcNow).Subtract(StartedUtc).TotalMilliseconds;
}

/// <summary>
/// The durable execution history for one deployment (Sprint M2.5 D7/D8): every task record and
/// every event, in order. Passed back into <see cref="IDeploymentExecutionRuntime.ResumeAsync"/>
/// after a restart so the runtime can determine the last successful task and continue from there.
/// </summary>
public sealed class ExecutionHistory
{
    private readonly List<TaskExecutionRecord> taskRecords = [];
    private readonly List<ExecutionEvent> events = [];

    public IReadOnlyList<TaskExecutionRecord> TaskRecords => taskRecords;

    public IReadOnlyList<ExecutionEvent> Events => events;

    public TaskExecutionRecord? FindRecord(string taskId)
        => taskRecords.FirstOrDefault(record => string.Equals(record.TaskId, taskId, StringComparison.OrdinalIgnoreCase));

    public bool HasCompleted(string taskId)
        => FindRecord(taskId)?.Status == TaskExecutionStatus.Completed;

    public void AddOrUpdateRecord(TaskExecutionRecord record)
    {
        var existingIndex = taskRecords.FindIndex(existing => string.Equals(existing.TaskId, record.TaskId, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            taskRecords[existingIndex] = record;
        }
        else
        {
            taskRecords.Add(record);
        }
    }

    public void AddEvent(ExecutionEvent executionEvent) => events.Add(executionEvent);

    /// <summary>The id of the most recently completed task, or <see langword="null"/> if nothing has completed yet — the resume anchor point (D7).</summary>
    public string? LastSuccessfulTaskId
        => taskRecords.Where(record => record.Status == TaskExecutionStatus.Completed)
            .OrderBy(record => record.CompletedUtc)
            .LastOrDefault()?.TaskId;
}

/// <summary>Structured, UI-consumable progress (Sprint M2.5 D8) — the Deployment Workspace UI is the intended consumer.</summary>
public sealed class ExecutionProgress
{
    public DeploymentExecutionPhaseName? CurrentPhase { get; init; }

    public string? CurrentTaskId { get; init; }

    public required double PercentComplete { get; init; }

    public required TimeSpan Duration { get; init; }

    public TimeSpan? EstimatedRemaining { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public required int RetryCount { get; init; }
}

/// <summary>The outcome of one call to <see cref="IInfrastructureTaskProvisioner.ExecuteTaskAsync"/> (Sprint M2.5 D5).</summary>
public sealed class TaskExecutionOutcome
{
    public required TaskExecutionStatus Status { get; init; }

    public required string Message { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Required when <see cref="Status"/> is <see cref="TaskExecutionStatus.Failed"/> — every failure must be classified, there is no unclassified failure path (D10).</summary>
    public FailureClassification? FailureClassification { get; init; }

    public static TaskExecutionOutcome Completed(string message, IReadOnlyList<string>? warnings = null) => new()
    {
        Status = TaskExecutionStatus.Completed,
        Message = message,
        Warnings = warnings ?? []
    };

    public static TaskExecutionOutcome Failed(string message, FailureClassification classification) => new()
    {
        Status = TaskExecutionStatus.Failed,
        Message = message,
        FailureClassification = classification
    };

    public static TaskExecutionOutcome Skipped(string message) => new()
    {
        Status = TaskExecutionStatus.Skipped,
        Message = message
    };
}

/// <summary>The final outcome of an <see cref="IDeploymentExecutionRuntime"/> run (Sprint M2.5 D1).</summary>
public sealed class ExecutionResult
{
    public required bool Success { get; init; }

    public required ExecutionHistory History { get; init; }

    public required ExecutionProgress Progress { get; init; }

    /// <summary>Non-empty only when <see cref="Success"/> is <see langword="false"/>: every blocking reason execution stopped for.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Whether this run stopped in a state <see cref="IDeploymentExecutionRuntime.ResumeAsync"/> can continue from (i.e. not a <see cref="Execution.FailureClassification.Fatal"/> stop).</summary>
    public required bool CanResume { get; init; }
}
