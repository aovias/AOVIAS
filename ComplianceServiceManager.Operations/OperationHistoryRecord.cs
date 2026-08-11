namespace ComplianceServiceManager.Operations;

public sealed record OperationStageTiming(string StageId, string StageName, string Outcome, TimeSpan? Duration);

/// <summary>A durable record of one finished operation run — what <see cref="OperationHistoryStore"/> persists and returns.</summary>
public sealed record OperationHistoryRecord
{
    public required string RunId { get; init; }

    public required string Kind { get; init; }

    public required string TargetIdentity { get; init; }

    public required string DisplayName { get; init; }

    public required int AttemptNumber { get; init; }

    public required bool IsResume { get; init; }

    public required DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset? FinishedUtc { get; init; }

    public TimeSpan? Duration { get; init; }

    public required string Outcome { get; init; }

    public required IReadOnlyList<OperationStageTiming> StageTimings { get; init; }

    /// <summary>Path to the exported structured-log text file for this run, if one was written.</summary>
    public string? LogFilePath { get; init; }

    public static OperationHistoryRecord FromInstance(OperationInstance instance, string? logFilePath) => new()
    {
        RunId = instance.RunId,
        Kind = instance.Descriptor.Kind,
        TargetIdentity = instance.Descriptor.TargetIdentity,
        DisplayName = instance.Descriptor.DisplayName,
        AttemptNumber = instance.AttemptNumber,
        IsResume = instance.IsResume,
        StartedUtc = instance.StartedUtc,
        FinishedUtc = instance.FinishedUtc,
        Duration = instance.FinishedUtc is null ? null : instance.FinishedUtc - instance.StartedUtc,
        Outcome = instance.Status.ToString(),
        StageTimings = instance.Stages
            .Select(stage => new OperationStageTiming(stage.Definition.Id, stage.Definition.Name, stage.Status.ToString(), stage.Duration))
            .ToList(),
        LogFilePath = logFilePath
    };
}
