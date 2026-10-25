namespace ComplianceServiceManager.Operations;

/// <summary>The live state of one stage within a running or finished <see cref="OperationInstance"/>.
/// Every mutating member is <c>internal</c> — only <see cref="OperationInstance"/> (via
/// <see cref="OperationContext"/>) is allowed to change a stage's state, so a presentation-layer
/// consumer can never accidentally desynchronize the UI from what actually happened.</summary>
public sealed class OperationStageState
{
    private readonly List<OperationLogEntry> logs = [];

    internal OperationStageState(OperationStageDefinition definition)
    {
        Definition = definition;
    }

    public OperationStageDefinition Definition { get; }

    public OperationStageStatus Status { get; internal set; } = OperationStageStatus.Pending;

    /// <summary>0..1 progress within this stage — meaningful only while <see cref="Status"/> is <see cref="OperationStageStatus.Running"/> (e.g. bytes uploaded / total bytes). Real signal or nothing; never a fabricated estimate.</summary>
    public double SubProgress { get; internal set; }

    /// <summary>Human-readable description of what is happening right now within this stage, e.g. "Uploading runtime agent — 34.2 / 87.5 MB".</summary>
    public string? CurrentTask { get; internal set; }

    public DateTimeOffset? StartedUtc { get; internal set; }

    public DateTimeOffset? FinishedUtc { get; internal set; }

    public TimeSpan? Duration => StartedUtc is null ? null : (FinishedUtc ?? DateTimeOffset.UtcNow) - StartedUtc.Value;

    /// <summary>Populated only when <see cref="Status"/> is <see cref="OperationStageStatus.Failed"/>.</summary>
    public OperationFailure? Failure { get; internal set; }

    public IReadOnlyList<OperationLogEntry> Logs => logs;

    internal void AppendLog(OperationLogEntry entry) => logs.Add(entry);
}
