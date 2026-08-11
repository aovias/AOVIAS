using AuditService.Application.Contracts;
using AuditService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AuditDbContext _db;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(AuditDbContext db, ILogger<UnitOfWork> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changeCount = _db.ChangeTracker.Entries().Count(e => e.State == EntityState.Added ||
                                                                   e.State == EntityState.Modified ||
                                                                   e.State == EntityState.Deleted);
        
        _logger.LogDebug("Saving changes to database. Tracked changes: {ChangeCount}", changeCount);
        
        var savedCount = await _db.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully saved {SavedCount} entity/entities to database", savedCount);
        
        return savedCount;
    }
}
