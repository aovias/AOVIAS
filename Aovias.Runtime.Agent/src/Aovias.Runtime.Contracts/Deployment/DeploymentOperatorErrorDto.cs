namespace Aovias.Runtime.Contracts.Deployment;

/// <summary>
/// Operator-facing deployment failure payload streamed over SignalR.
/// </summary>
public sealed class DeploymentOperatorErrorDto
{
    public required string Category { get; init; }

    public required string Title { get; init; }

    public required string Reason { get; init; }

    public required string RecoveryAction { get; init; }

    public string DocumentationHint { get; init; } = string.Empty;

    public string Phase { get; init; } = string.Empty;

    public string Component { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string ExceptionType { get; init; } = string.Empty;

    public string? InnerExceptionMessage { get; init; }

    public string? ServiceId { get; init; }

    public string? RequiredSecret { get; init; }

    public string? CorrelationId { get; init; }

    public string? DeploymentId { get; init; }

    public int? HttpStatusCode { get; init; }
}
