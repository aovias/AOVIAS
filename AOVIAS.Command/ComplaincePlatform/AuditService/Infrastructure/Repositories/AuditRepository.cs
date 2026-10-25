using AuditService.Application.Contracts;
using AuditService.Domain;
using AuditService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly AuditDbContext _db;

    public AuditRepository(AuditDbContext db)
    {
        _db = db;
    }

    public async Task<AuditSession?> GetAsync(Guid id)
    {
        return await _db.AuditSessions.FindAsync(id);
    }

    public async Task AddAsync(AuditSession session)
    {
        await _db.AuditSessions.AddAsync(session);
    }

    public void Update(AuditSession session)
    {
        _db.AuditSessions.Update(session);
    }

    public void Remove(AuditSession session)
    {
        _db.AuditSessions.Remove(session);
    }

    public IQueryable<AuditSession> Query()
    {
        return _db.AuditSessions.AsQueryable();
    }

    public async Task<List<AuditSession>> GetByOrganizationAsync(Guid organizationId)
    {
        return await _db.AuditSessions
            .Where(s => s.OrganizationId == organizationId)
            .ToListAsync();
    }
}

