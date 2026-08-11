namespace Aovias.Platform.Core;

/// <summary>
/// Correlation context propagated across platform operations.
/// </summary>
public sealed class CorrelationContext
{
    public required string CorrelationId { get; init; }

    public static CorrelationContext Create(string? correlationId = null)
        => new() { CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId };
}
