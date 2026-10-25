using AIOrchestratorService.Application.Services;
using AIOrchestratorService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AIOrchestratorService.Infrastructure.Persistence;

public class DraftArtifactRepository : IDraftArtifactRepository
{
    private readonly AIOrchestratorDbContext _context;

    public DraftArtifactRepository(AIOrchestratorDbContext context)
    {
        _context = context;
    }

    public async Task<DraftArtifact> AddAsync(DraftArtifact draftArtifact, CancellationToken cancellationToken = default)
    {
        draftArtifact.CreatedAt = DateTime.UtcNow;
        await _context.DraftArtifacts.AddAsync(draftArtifact, cancellationToken);
        return draftArtifact;
    }

    public async Task<DraftArtifact?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DraftArtifacts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<DraftArtifact>> GetBySourceEntityAsync(
        Guid sourceEntityId,
        DraftType? draftType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DraftArtifacts
            .Where(x => x.SourceEntityId == sourceEntityId);

        if (draftType.HasValue)
        {
            query = query.Where(x => x.DraftType == draftType.Value);
        }

        return await query
            .OrderByDescending(x => x.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
