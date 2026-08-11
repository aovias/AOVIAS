using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>Sprint 13F — read-side analytics over Copilot domain tables.</summary>
public interface ICopilotAnalyticsRepository
{
    Task<RecommendationCounts> GetRecommendationCountsAsync(
        Guid organizationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default);

    Task<DraftCounts> GetDraftCountsAsync(
        Guid organizationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default);

    Task<ProposalCounts> GetProposalCountsAsync(
        Guid organizationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<LabelCount>> GetRecommendationCountsByCategoryAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<IReadOnlyList<LabelCount>> GetRecommendationCountsByPersonaAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<IReadOnlyList<LabelCount>> GetDraftCountsByTypeAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<IReadOnlyList<LabelCount>> GetProposalCountsByTypeAsync(
        Guid organizationId,
        CancellationToken ct = default);

    Task<int> CountRecommendationsWithProposalsAsync(Guid organizationId, CancellationToken ct = default);

    Task<int> CountRecommendationsWithExecutedProposalsAsync(Guid organizationId, CancellationToken ct = default);

    Task<int> CountDraftsWithProposalsAsync(Guid organizationId, CancellationToken ct = default);

    Task<double?> GetAverageRecommendationTimeToActionHoursAsync(Guid organizationId, CancellationToken ct = default);

    Task<double?> GetAverageProposalApprovalTimeHoursAsync(Guid organizationId, CancellationToken ct = default);

    Task<double?> GetAverageProposalExecutionTimeHoursAsync(Guid organizationId, CancellationToken ct = default);

    Task<int> CountProposalAuditViewedAsync(Guid organizationId, CancellationToken ct = default);

    Task<IReadOnlyList<RecommendationImpactRow>> GetRecommendationImpactRowsAsync(
        Guid organizationId,
        int limit = 20,
        CancellationToken ct = default);

    Task SaveEffectivenessSnapshotAsync(CopilotEffectivenessSnapshot snapshot, CancellationToken ct = default);

    Task<IReadOnlyList<CopilotEffectivenessSnapshot>> GetEffectivenessSnapshotsAsync(
        Guid organizationId,
        int days = 30,
        CancellationToken ct = default);

    Task<CopilotEffectivenessSnapshot?> GetEffectivenessSnapshotForDateAsync(
        Guid organizationId,
        DateOnly date,
        CancellationToken ct = default);
}

public sealed class RecommendationCounts
{
    public int Total { get; init; }
    public int Proposed { get; init; }
    public int Accepted { get; init; }
    public int Dismissed { get; init; }
    public int Expired { get; init; }
}

public sealed class DraftCounts
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Archived { get; init; }
    public int RegeneratedGroups { get; init; }
}

public sealed class ProposalCounts
{
    public int Total { get; init; }
    public int Proposed { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Executed { get; init; }
    public int Expired { get; init; }
}

public sealed class LabelCount
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class RecommendationImpactRow
{
    public Guid RecommendationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Persona { get; init; } = string.Empty;
    public string? ImpactJson { get; init; }
    public bool HasProposal { get; init; }
    public bool ProposalExecuted { get; init; }
    public string? ResultEntityType { get; init; }
    public string? ExecutionNotes { get; init; }
}
