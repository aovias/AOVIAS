using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ComplianceDbContext _dbContext;

    public AuditLogRepository(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
    }

    public async Task<List<AuditLog>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public IQueryable<AuditLog> Query()
    {
        return _dbContext.AuditLogs.AsNoTracking();
    }
}

