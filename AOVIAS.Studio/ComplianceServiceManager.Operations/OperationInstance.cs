namespace ComplianceServiceManager.Operations;

/// <summary>
/// The live state of one running (or finished) operation: per-stage status/logs/durations, overall
/// weighted progress, elapsed/estimated-remaining time, and the terminal outcome. Every mutating
/// method is <c>internal</c> and reachable only through <see cref="OperationContext"/> (itself only
/// constructible by <see cref="OperationEngine"/>) — an executor can report progress but can never
/// bypass the state machine, and a presentation-layer consumer can only observe it via
/// <see cref="Changed"/> and the public properties.
///
/// Thread-safe: an executor runs on a background thread (<see cref="OperationEngine"/> invokes it
/// via <c>Task.Run</c>), and every mutating call is guarded by an internal lock. <see cref="Changed"/>
/// fires synchronously on whatever thread made the mutating call — the presentation layer, not this
/// type, is responsible for marshaling to the UI thread (see the "Dispatcher only inside the
/// presentation layer" rule).
/// </summary>
public sealed class OperationInstance
{
    private readonly object mutationGate = new();
    private readonly List<OperationStageState> stages;

    internal OperationInstance(OperationDescriptor descriptor, bool isResume, int attemptNumber)
    {
        Descriptor = descriptor;
        IsResume = isResume;
        AttemptNumber = attemptNumber;
        stages = descriptor.Stages.Select(definition => new OperationStageState(definition)).ToList();
    }

    public OperationDescriptor Descriptor { get; }

    public string RunId { get; } = Guid.NewGuid().ToString("N");

    public bool IsResume { get; }

    public int AttemptNumber { get; }

    public DateTimeOffset StartedUtc { get; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? FinishedUtc { get; private set; }

    public IReadOnlyList<OperationStageState> Stages => stages;

    public OperationStatus Status { get; private set; } = OperationStatus.Running;

    /// <summary>Populated only when <see cref="Status"/> is <see cref="OperationStatus.Failed"/>.</summary>
    public OperationFailure? Failure { get; private set; }

    public string? CurrentStageId { get; private set; }

    public string? CurrentTask { get; private set; }

    /// <summary>0..1. Weighted sum of completed stage weights plus the running stage's sub-fraction of its own weight. See the type remarks on <see cref="OperationEngine"/> for the honesty rule this implements.</summary>
    public double OverallProgress { get; private set; }

    public TimeSpan Elapsed => (FinishedUtc ?? DateTimeOffset.UtcNow) - StartedUtc;

    /// <summary>Null until at least one weighted stage has completed — never a fabricated estimate at 0% progress.</summary>
    public TimeSpan? EstimatedRemaining { get; private set; }

    public bool HasWarnings => stages.Any(stage => stage.Status == OperationStageStatus.Warning);

    /// <summary>Raised after every state change, synchronously, on the calling thread.</summary>
    public event Action<OperationInstance>? Changed;

    internal void BeginStage(string stageId)
    {
        lock (mutationGate)
        {
            var stage = FindStage(stageId);
            stage.Status = OperationStageStatus.Running;
            stage.StartedUtc = DateTimeOffset.UtcNow;
            CurrentStageId = stageId;
            CurrentTask = null;
            RecomputeProgress();
        }

        RaiseChanged();
    }

    internal void ReportTask(string stageId, string task, double? subProgress)
    {
        lock (mutationGate)
        {
            var stage = FindStage(stageId);
            stage.CurrentTask = task;
            if (subProgress.HasValue)
            {
                stage.SubProgress = Math.Clamp(subProgress.Value, 0, 1);
            }

            if (string.Equals(CurrentStageId, stageId, StringComparison.Ordinal))
            {
                CurrentTask = task;
            }

            RecomputeProgress();
        }

        RaiseChanged();
    }

    internal void CompleteStage(string stageId, OperationStageStatus status)
    {
        if (status is not (OperationStageStatus.Completed or OperationStageStatus.Warning or OperationStageStatus.Skipped))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "CompleteStage only accepts Completed, Warning, or Skipped — use FailStage for failures.");
        }

        lock (mutationGate)
        {
            var stage = FindStage(stageId);
            stage.Status = status;
            stage.FinishedUtc = DateTimeOffset.UtcNow;
            stage.SubProgress = 1;
            stage.CurrentTask = null;
            if (string.Equals(CurrentStageId, stageId, StringComparison.Ordinal))
            {
                CurrentStageId = null;
                CurrentTask = null;
            }

            RecomputeProgress();
        }

        RaiseChanged();
    }

    internal void SkipStage(string stageId, string? reason)
    {
        lock (mutationGate)
        {
            var stage = FindStage(stageId);
            stage.Status = OperationStageStatus.Skipped;
            stage.FinishedUtc = DateTimeOffset.UtcNow;
            stage.SubProgress = 1;
            stage.CurrentTask = reason;
            RecomputeProgress();
        }

        RaiseChanged();
    }

    internal void FailStage(string stageId, OperationFailure failure)
    {
        lock (mutationGate)
        {
            var stage = FindStage(stageId);
            stage.Status = OperationStageStatus.Failed;
            stage.FinishedUtc = DateTimeOffset.UtcNow;
            stage.Failure = failure;
            if (string.Equals(CurrentStageId, stageId, StringComparison.Ordinal))
            {
                CurrentStageId = null;
                CurrentTask = null;
            }

            RecomputeProgress();
        }

        RaiseChanged();
    }

    internal void AppendLog(string stageId, string message, OperationLogSeverity severity)
    {
        lock (mutationGate)
        {
            var stage = FindStage(stageId);
            stage.AppendLog(new OperationLogEntry(DateTimeOffset.UtcNow, severity, stageId, stage.Definition.Name, message));
        }

        RaiseChanged();
    }

    internal void MarkTerminal(OperationStatus finalStatus, OperationFailure? failure)
    {
        if (finalStatus == OperationStatus.Running)
        {
            throw new ArgumentOutOfRangeException(nameof(finalStatus), finalStatus, "MarkTerminal requires a terminal status.");
        }

        lock (mutationGate)
        {
            Status = finalStatus;
            Failure = failure;
            FinishedUtc = DateTimeOffset.UtcNow;
            CurrentStageId = null;
            CurrentTask = null;

            // Any stage still marked Pending when the run ends without having reached it (a failure
            // or cancellation upstream) stays Pending — that is the "future stages remain Pending"
            // contract, not something to overwrite here.
        }

        RaiseChanged();
    }

    private OperationStageState FindStage(string stageId)
    {
        var stage = stages.FirstOrDefault(candidate => string.Equals(candidate.Definition.Id, stageId, StringComparison.Ordinal));
        if (stage is null)
        {
            throw new ArgumentException($"Unknown stage id '{stageId}' for operation '{Descriptor.Kind}'.", nameof(stageId));
        }

        return stage;
    }

    private void RecomputeProgress()
    {
        var totalWeight = stages.Sum(stage => stage.Definition.Weight);
        if (totalWeight <= 0)
        {
            OverallProgress = 0;
            EstimatedRemaining = null;
            return;
        }

        var completedWeight = 0.0;
        foreach (var stage in stages)
        {
            completedWeight += stage.Status switch
            {
                OperationStageStatus.Completed or OperationStageStatus.Warning or OperationStageStatus.Skipped => stage.Definition.Weight,
                OperationStageStatus.Running => stage.Definition.Weight * Math.Clamp(stage.SubProgress, 0, 1),
                _ => 0
            };
        }

        OverallProgress = Math.Clamp(completedWeight / totalWeight, 0, 1);

        if (completedWeight > 0 && completedWeight < totalWeight)
        {
            var elapsedSeconds = (DateTimeOffset.UtcNow - StartedUtc).TotalSeconds;
            var remainingWeight = totalWeight - completedWeight;
            var estimatedSeconds = elapsedSeconds / completedWeight * remainingWeight;
            EstimatedRemaining = TimeSpan.FromSeconds(Math.Max(0, estimatedSeconds));
        }
        else
        {
            EstimatedRemaining = null;
        }
    }

    private void RaiseChanged() => Changed?.Invoke(this);
}
