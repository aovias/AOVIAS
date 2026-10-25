using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Retrieval;

public interface IRetrievalService
{
    Task<AIRetrievalContext> BuildContextAsync(AIRequest request, CancellationToken cancellationToken);

    /// <summary>Phase 5 — hybrid knowledge search without full orchestration.</summary>
    Task<KnowledgeSearchResult> SearchKnowledgeAsync(KnowledgeQuery query, CancellationToken cancellationToken);
}
