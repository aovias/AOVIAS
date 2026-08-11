using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

/// <summary>Read-only queries for the Evidence Binder (framework + hospital scoped).</summary>
public interface IEvidenceBinderQueryService
{
    /// <summary>Resolved framework id for snapshot queries (handles journey id passed as framework id).</summary>
    Task<Guid> ResolveFrameworkIdAsync(Guid hospitalId, Guid frameworkOrJourneyId, CancellationToken cancellationToken = default);

    Task<bool> IsObjectiveInBinderScopeAsync(Guid resolvedFrameworkId, Guid objectiveElementId, CancellationToken cancellationToken = default);

    Task<EvidenceBinderSummaryDto> GetSummaryAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceBinderChapterDto>> GetChaptersAsync(Guid hospitalId, Guid frameworkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceBinderObjectiveElementDto>> GetObjectivesForChapterAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid chapterId,
        CancellationToken cancellationToken = default);

    Task<EvidenceBinderOverviewDto?> GetObjectiveOverviewAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<EvidenceBinderEvidenceItemDto>> GetEvidencesForObjectiveAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceBinderChecklistItemDto>> GetChecklistAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default);

    /// <summary>AI-driven mapping suggestions via AIOrchestrator assist (heuristic fallback). Always HumanApprovalRequired.</summary>
    Task<IReadOnlyList<EvidenceBinderAiMappingItemDto>> GetAiMappingAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OeHistoryEventDto>> GetHistoryAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
