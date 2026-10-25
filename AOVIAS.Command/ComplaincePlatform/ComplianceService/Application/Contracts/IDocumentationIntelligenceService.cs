using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IDocumentationIntelligenceService
{
    Task<ComplianceGraphDto> GetGraphAsync(Guid organizationId, CancellationToken ct = default);
    Task<DocumentIntelligenceDto?> GetIntelligenceAsync(Guid organizationId, Guid documentId, CancellationToken ct = default);
    Task<DocumentImpactDto?> GetImpactAsync(Guid organizationId, Guid documentId, CancellationToken ct = default);
    Task<DocumentDependenciesDto?> GetDependenciesAsync(Guid organizationId, Guid documentId, CancellationToken ct = default);
    Task<DocumentRelationshipsDto?> GetRelationshipsAsync(Guid organizationId, Guid documentId, CancellationToken ct = default);
    Task<AiComplianceReviewDto?> ReviewAiAsync(Guid organizationId, AiComplianceReviewRequest request, CancellationToken ct = default);
    Task<DocumentExplorerDto> GetExplorerAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentSearchResultDto>> SearchAsync(Guid organizationId, string query, CancellationToken ct = default);
    Task<DocumentHealthDashboardDto> GetHealthDashboardAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<CrossReferenceSuggestionDto>> GetCrossReferenceSuggestionsAsync(
        Guid organizationId, Guid documentId, string query, CancellationToken ct = default);
}
