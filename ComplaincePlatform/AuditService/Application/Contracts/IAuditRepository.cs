using AuditService.Domain;

namespace AuditService.Application.Contracts;

public interface IAuditRepository
{
    Task<AuditSession?> GetAsync(Guid id);
    Task AddAsync(AuditSession session);
    void Update(AuditSession session);
    void Remove(AuditSession session);
    IQueryable<AuditSession> Query();

    Task<List<AuditSession>> GetByOrganizationAsync(Guid organizationId);
}

