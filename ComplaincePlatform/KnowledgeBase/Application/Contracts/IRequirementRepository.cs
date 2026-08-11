using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Application.Contracts;

public interface IRequirementRepository
{
    Task<RequirementDetail?> GetByNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);
}
