using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<List<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
    IQueryable<AuditLog> Query();
}

