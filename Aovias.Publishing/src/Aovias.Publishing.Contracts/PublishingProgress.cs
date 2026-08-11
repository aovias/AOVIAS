namespace Aovias.Publishing.Contracts;

/// <summary>
/// Pipeline progress notification.
/// </summary>
public sealed class PublishingProgress
{
    public required string StepName { get; init; }

    public required string Status { get; init; }

    public required string Message { get; init; }

    public int PercentComplete { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Result of a single publishing step.
/// </summary>
public sealed class PublishingStepResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = string.Empty;

    public static PublishingStepResult Success(string message = "OK")
        => new() { IsSuccess = true, Message = message };

    public static PublishingStepResult Failure(string message)
        => new() { IsSuccess = false, Message = message };
}
