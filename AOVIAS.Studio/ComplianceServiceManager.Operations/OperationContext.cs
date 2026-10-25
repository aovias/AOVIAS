namespace ComplianceServiceManager.Operations;

/// <summary>
/// The only surface an <see cref="IOperationExecutor"/> talks to. Constructible only by
/// <see cref="OperationEngine"/>, so an executor can never obtain one except by being run — it
/// cannot mutate an <see cref="OperationInstance"/> directly.
/// </summary>
public sealed class OperationContext
{
    private readonly OperationInstance instance;

    internal OperationContext(OperationInstance instance, CancellationToken cancellationToken)
    {
        this.instance = instance;
        CancellationToken = cancellationToken;
    }

    public bool IsResume => instance.IsResume;

    public int AttemptNumber => instance.AttemptNumber;

    /// <summary>Cooperative cancellation only — see <see cref="OperationEngine"/> remarks on cancellation granularity. An executor must check this between discrete units of work (e.g. between remote commands) and throw <see cref="OperationCanceledException"/> promptly; it is never force-aborted mid-call.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Marks a stage Running and records its start time.</summary>
    public void BeginStage(string stageId) => instance.BeginStage(stageId);

    /// <summary>Reports what the stage is doing right now, and optionally a real sub-progress fraction (0..1) within the stage — omit <paramref name="subProgress"/> when there is no real fractional signal (e.g. a stage with only one discrete unit of work) rather than guessing one.</summary>
    public void ReportTask(string stageId, string currentTask, double? subProgress = null) => instance.ReportTask(stageId, currentTask, subProgress);

    /// <summary>Marks a stage finished successfully — <paramref name="status"/> must be <see cref="OperationStageStatus.Completed"/>, <see cref="OperationStageStatus.Warning"/>, or <see cref="OperationStageStatus.Skipped"/>.</summary>
    public void CompleteStage(string stageId, OperationStageStatus status = OperationStageStatus.Completed) => instance.CompleteStage(stageId, status);

    public void SkipStage(string stageId, string? reason = null) => instance.SkipStage(stageId, reason);

    /// <summary>Marks a stage Failed. The engine stops advancing to subsequent stages once an executor's <see cref="IOperationExecutor.ExecuteAsync"/> returns after calling this — stages after the failed one are expected to remain <see cref="OperationStageStatus.Pending"/>.</summary>
    public void FailStage(string stageId, OperationFailure failure) => instance.FailStage(stageId, failure);

    public void Log(string stageId, string message, OperationLogSeverity severity = OperationLogSeverity.Info) => instance.AppendLog(stageId, message, severity);
}
