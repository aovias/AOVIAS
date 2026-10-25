namespace Aovias.Platform.Core;

/// <summary>
/// Execution context for platform workflows and operations.
/// </summary>
public sealed class ExecutionContext
{
    public required OperationId OperationId { get; init; }

    public required CorrelationContext Correlation { get; init; }

    public required string User { get; init; }

    public required string Runtime { get; init; }

    public required string Environment { get; init; }

    public required DateTimeOffset StartedAt { get; init; }

    public CancellationToken CancellationToken { get; init; }

    public static ExecutionContext Create(
        string user,
        string runtime,
        string environment,
        CancellationToken cancellationToken,
        string? correlationId = null)
        => new()
        {
            OperationId = OperationId.New(),
            Correlation = CorrelationContext.Create(correlationId),
            User = user,
            Runtime = runtime,
            Environment = environment,
            StartedAt = DateTimeOffset.UtcNow,
            CancellationToken = cancellationToken
        };
}
