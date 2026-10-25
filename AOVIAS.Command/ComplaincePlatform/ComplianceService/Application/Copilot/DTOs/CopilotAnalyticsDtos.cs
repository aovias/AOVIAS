namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Sprint 13F — governance dashboard overview.</summary>
public sealed class CopilotGovernanceDashboardDto
{
    public CopilotUsageSummaryDto Usage { get; init; } = new();
    public RecommendationEffectivenessMetricsDto Recommendations { get; init; } = new();
    public DraftEffectivenessMetricsDto Drafts { get; init; } = new();
    public ProposalConversionMetricsDto Proposals { get; init; } = new();
    public PersonaAdoptionMetricsDto Personas { get; init; } = new();
    public CopilotActivityAnalyticsDto Activity { get; init; } = new();
    public CopilotTrustMetricsDto Trust { get; init; } = new();
    public DateTime MetricsAsOfUtc { get; init; }
}

public sealed class CopilotUsageSummaryDto
{
    public int WorkspaceViews { get; init; }
    public int ActiveUsers { get; init; }
    public int PersonaChanges { get; init; }
    public int TotalActivityEvents { get; init; }
}

public sealed class RecommendationEffectivenessMetricsDto
{
    public int TotalGenerated { get; init; }
    public int Proposed { get; init; }
    public int Accepted { get; init; }
    public int Dismissed { get; init; }
    public int Expired { get; init; }
    public int Viewed { get; init; }
    public int WithProposalCreated { get; init; }
    public int WithProposalExecuted { get; init; }
    public double AcceptanceRate { get; init; }
    public double ExecutionRate { get; init; }
    public double DismissalRate { get; init; }
    public double? AverageTimeToActionHours { get; init; }
    public IReadOnlyList<CategoryMetricDto> ByCategory { get; init; } = [];
    public IReadOnlyList<CategoryMetricDto> ByPersona { get; init; } = [];
}

public sealed class DraftEffectivenessMetricsDto
{
    public int TotalDrafts { get; init; }
    public int ActiveDrafts { get; init; }
    public int ArchivedDrafts { get; init; }
    public int RegeneratedGroups { get; init; }
    public int WithProposalCreated { get; init; }
    public double UtilizationRate { get; init; }
    public double RegenerationRate { get; init; }
    public double ProposalConversionRate { get; init; }
    public IReadOnlyList<CategoryMetricDto> ByDraftType { get; init; } = [];
}

public sealed class ProposalConversionMetricsDto
{
    public int Proposed { get; init; }
    public int Viewed { get; init; }
    public int Approved { get; init; }
    public int Rejected { get; init; }
    public int Executed { get; init; }
    public int Expired { get; init; }
    public double ApprovalRate { get; init; }
    public double ExecutionRate { get; init; }
    public double RejectionRate { get; init; }
    public double? AverageApprovalTimeHours { get; init; }
    public double? AverageExecutionTimeHours { get; init; }
    public IReadOnlyList<CategoryMetricDto> ByProposalType { get; init; } = [];
}

public sealed class PersonaAdoptionMetricsDto
{
    public IReadOnlyList<PersonaAdoptionRowDto> Personas { get; init; } = [];
    public string? MostUsedPersona { get; init; }
    public string? LeastUsedPersona { get; init; }
    public string? HighestConversionPersona { get; init; }
    public int ManualOverrides { get; init; }
}

public sealed class PersonaAdoptionRowDto
{
    public string Persona { get; init; } = string.Empty;
    public int SessionEvents { get; init; }
    public int RecommendationsViewed { get; init; }
    public int DraftsGenerated { get; init; }
    public int ProposalsApproved { get; init; }
    public double ConversionRate { get; init; }
}

public sealed class CopilotActivityAnalyticsDto
{
    public int DailyEvents { get; init; }
    public int WeeklyEvents { get; init; }
    public int MonthlyEvents { get; init; }
    public IReadOnlyList<CategoryMetricDto> TopActivities { get; init; } = [];
    public IReadOnlyList<DailyActivityPointDto> DailyTrend { get; init; } = [];
}

public sealed class CopilotTrustMetricsDto
{
    public double ProposalApprovalRate { get; init; }
    public double RecommendationAcceptanceRate { get; init; }
    public double DraftToProposalRate { get; init; }
    public int IgnoredRecommendations { get; init; }
}

public sealed class CategoryMetricDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Rate { get; init; }
}

public sealed class DailyActivityPointDto
{
    public DateOnly Date { get; init; }
    public int Count { get; init; }
}

public sealed class CopilotTrendsDto
{
    public IReadOnlyList<CopilotEffectivenessSnapshotDto> Snapshots { get; init; } = [];
    public IReadOnlyList<DailyActivityPointDto> ActivityTrend { get; init; } = [];
}

public sealed class CopilotEffectivenessSnapshotDto
{
    public DateOnly SnapshotDate { get; init; }
    public int RecommendationsGenerated { get; init; }
    public int RecommendationsAccepted { get; init; }
    public int DraftsGenerated { get; init; }
    public int ProposalsExecuted { get; init; }
    public int PersonaSwitches { get; init; }
    public int ActiveUsers { get; init; }
}

public sealed class RecommendationImpactDto
{
    public Guid RecommendationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Persona { get; init; } = string.Empty;
    public bool HasProposal { get; init; }
    public bool ProposalExecuted { get; init; }
    public string? ResultEntityType { get; init; }
    public string? EstimatedImpact { get; init; }
    public string? ActualOutcome { get; init; }
}

public sealed class RecommendationImpactSummaryDto
{
    public int RecommendationsWithExecution { get; init; }
    public int RecommendationsWithProposal { get; init; }
    public double ExecutionConversionRate { get; init; }
    public IReadOnlyList<RecommendationImpactDto> TopImpacts { get; init; } = [];
}
