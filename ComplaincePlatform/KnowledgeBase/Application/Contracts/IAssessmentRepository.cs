using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IAssessmentRepository
{
    Task AddAsync(Assessment assessment, CancellationToken cancellationToken = default);
    Task<Assessment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Assessment?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssessmentItem?> GetItemAsync(Guid assessmentId, Guid nodeId, CancellationToken cancellationToken = default);
    Task UpsertItemAsync(AssessmentItem item, CancellationToken cancellationToken = default);
    Task<Assessment?> GetLatestForFacilityFrameworkSinceAsync(
        Guid facilityId,
        Guid frameworkId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
