using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class AIRepository : IAIRepository
{
    private readonly KnowledgeBaseContext _db;

    public AIRepository(KnowledgeBaseContext db) => _db = db;

    public async Task AddAsync(AIInteraction interaction, CancellationToken cancellationToken = default)
    {
        await _db.AIInteractions.AddAsync(interaction, cancellationToken);
    }

    public async Task<AIInteraction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.AIInteractions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }
}
