using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class FrameworkRepository : IFrameworkRepository
{
    private readonly KnowledgeBaseContext _db;

    public FrameworkRepository(KnowledgeBaseContext db) => _db = db;

    public async Task<IReadOnlyList<Framework>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Frameworks.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Framework?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Frameworks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
