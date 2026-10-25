using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBase.Infrastructure.Repositories;

public sealed class AssessmentRepository : IAssessmentRepository
{
    private readonly KnowledgeBaseContext _db;

    public AssessmentRepository(KnowledgeBaseContext db) => _db = db;

    public async Task AddAsync(Assessment assessment, CancellationToken cancellationToken = default)
    {
        await _db.Assessments.AddAsync(assessment, cancellationToken);
    }

    public async Task<Assessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Assessments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Assessment?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Assessments
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<AssessmentItem?> GetItemAsync(Guid assessmentId, Guid nodeId, CancellationToken cancellationToken = default)
    {
        return await _db.AssessmentItems
            .FirstOrDefaultAsync(x => x.AssessmentId == assessmentId && x.NodeId == nodeId, cancellationToken);
    }

    public async Task UpsertItemAsync(AssessmentItem item, CancellationToken cancellationToken = default)
    {
        var existing = await _db.AssessmentItems
            .AnyAsync(x => x.Id == item.Id, cancellationToken);
        if (existing)
            _db.AssessmentItems.Update(item);
        else
            await _db.AssessmentItems.AddAsync(item, cancellationToken);
    }

    public async Task<Assessment?> GetLatestForFacilityFrameworkSinceAsync(
        Guid facilityId,
        Guid frameworkId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await _db.Assessments.AsNoTracking()
            .Where(x =>
                x.FacilityId == facilityId &&
                x.FrameworkId == frameworkId &&
                x.Status == "Open" &&
                x.CreatedAt >= sinceUtc)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }
}
