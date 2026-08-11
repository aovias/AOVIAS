using Aovias.Release.Contracts.Audit;

namespace Aovias.ReleaseManager.Abstractions;

/// <summary>
/// Stores immutable audit events for release operations.
/// </summary>
public interface IReleaseAuditStore
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditEvent>> GetByReleaseIdAsync(string releaseId, CancellationToken cancellationToken = default);
}
