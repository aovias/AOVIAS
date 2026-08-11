using Aovias.Release.Contracts.Enums;

namespace Aovias.Release.Contracts.Audit;

/// <summary>
/// Immutable audit event for release operations.
/// </summary>
public sealed class AuditEvent
{
    public required string EventId { get; init; }

    public required AuditEventType EventType { get; init; }

    public required string ReleaseId { get; init; }

    public required string PackageId { get; init; }

    public required string Actor { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required string Message { get; init; }

    public string? DeploymentId { get; init; }

    public string? CorrelationId { get; init; }

    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
