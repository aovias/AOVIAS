using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class DecisionRepository : IDecisionRepository
{
    private readonly KnowledgeBaseContext _db;

    public DecisionRepository(KnowledgeBaseContext db) => _db = db;

    public async Task AddAsync(DecisionLog log, CancellationToken cancellationToken = default)
    {
        await _db.DecisionLogs.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyList<DecisionLog>> ListByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _db.DecisionLogs.AsNoTracking()
            .Where(x => x.NodeId == nodeId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }
}
