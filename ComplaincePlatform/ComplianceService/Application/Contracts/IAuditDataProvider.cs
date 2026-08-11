using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Optional interface for retrieving audit data from AuditService.
/// Can be implemented via HTTP client or direct database access.
/// </summary>
public interface IAuditDataProvider
{
    /// <summary>
    /// Gets audit session by compliance program ID.
    /// </summary>
    Task<AuditContextData?> GetAuditSessionByProgramIdAsync(Guid programId, CancellationToken ct = default);

    /// <summary>
    /// Gets audit findings by audit session ID.
    /// </summary>
    Task<List<AuditFindingData>> GetAuditFindingsBySessionIdAsync(Guid auditSessionId, CancellationToken ct = default);
}
