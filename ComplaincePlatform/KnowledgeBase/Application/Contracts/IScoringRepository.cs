using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IScoringRepository
{
    Task<ScoringConfig?> GetByFrameworkAsync(Guid frameworkId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FrameworkClassification>> GetClassificationsAsync(Guid frameworkId, CancellationToken cancellationToken = default);
    Task<RequirementScore?> GetRequirementScoreAsync(Guid nodeId, CancellationToken cancellationToken = default);
}
