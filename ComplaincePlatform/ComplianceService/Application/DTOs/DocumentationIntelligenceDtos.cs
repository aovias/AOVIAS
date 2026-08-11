namespace ComplianceService.Application.DTOs;

public record ComplianceGraphNodeDto
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string NodeType { get; init; } = string.Empty;
    public string? Route { get; init; }
    public int ConnectionCount { get; init; }
}

public record ComplianceGraphEdgeDto
{
    public string SourceId { get; init; } = string.Empty;
    public string TargetId { get; init; } = string.Empty;
    public string RelationshipType { get; init; } = string.Empty;
    public bool Bidirectional { get; init; }
}

public record ComplianceGraphDto
{
    public IReadOnlyList<ComplianceGraphNodeDto> Nodes { get; init; } = Array.Empty<ComplianceGraphNodeDto>();
    public IReadOnlyList<ComplianceGraphEdgeDto> Edges { get; init; } = Array.Empty<ComplianceGraphEdgeDto>();
    public int TotalDocuments { get; init; }
    public int TotalRelationships { get; init; }
}

public record DocumentIntelligenceDto
{
    public Guid DocumentId { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DocumentHealthScoreDto Health { get; init; } = new();
    public int ComplianceMappingCoveragePercent { get; init; }
    public double AcknowledgementPercent { get; init; }
    public int LinkedAssetsCount { get; init; }
    public int DependenciesCount { get; init; }
    public int AiConfidencePercent { get; init; }
    public string ReviewStatus { get; init; } = string.Empty;
    public DocumentImpactSummaryDto Impact { get; init; } = new();
    public IReadOnlyList<LinkedAssetDto> LinkedAssets { get; init; } = Array.Empty<LinkedAssetDto>();
    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();
}

public record DocumentImpactSummaryDto
{
    public int AffectedChapters { get; init; }
    public int AffectedStandards { get; init; }
    public int AffectedObjectiveElements { get; init; }
    public int AffectedEvidence { get; init; }
    public int AffectedFindings { get; init; }
    public int AffectedDeficiencies { get; init; }
    public int AffectedCapas { get; init; }
    public int AffectedTasks { get; init; }
    public int AffectedAuditSessions { get; init; }
    public int AffectedDepartments { get; init; }
    public string PublishingRisk { get; init; } = "Low";
    public string ImpactSummary { get; init; } = string.Empty;
}

public record DocumentImpactDto
{
    public Guid DocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DocumentImpactSummaryDto Summary { get; init; } = new();
    public IReadOnlyList<ImpactItemDto> Items { get; init; } = Array.Empty<ImpactItemDto>();
}

public record ImpactItemDto
{
    public string Category { get; init; } = string.Empty;
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Route { get; init; }
}

public record DocumentDependenciesDto
{
    public Guid DocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool CanArchive { get; init; }
    public bool CanSupersede { get; init; }
    public bool CanDelete { get; init; }
    public string BlockReason { get; init; } = string.Empty;
    public IReadOnlyList<DependencyItemDto> UsedBy { get; init; } = Array.Empty<DependencyItemDto>();
}

public record DependencyItemDto
{
    public string Category { get; init; } = string.Empty;
    public int Count { get; init; }
    public IReadOnlyList<string> Examples { get; init; } = Array.Empty<string>();
}

public record DocumentRelationshipsDto
{
    public Guid DocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public ComplianceGraphDto Graph { get; init; } = new();
}

public record AiComplianceReviewRequest
{
    public Guid DocumentId { get; init; }
}

public record AiComplianceReviewDto
{
    public int OverallComplianceReviewScore { get; init; }
    public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Risks { get; init; } = Array.Empty<string>();
    public IReadOnlyList<AiReviewSuggestionDto> Suggestions { get; init; } = Array.Empty<AiReviewSuggestionDto>();
    public bool IsAdvisoryOnly { get; init; } = true;
}

public record AiReviewSuggestionDto
{
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = "info";
}

public record DocumentExplorerNodeDto
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string NodeType { get; init; } = string.Empty;
    public Guid? DocumentId { get; init; }
    public string? Route { get; init; }
    public IReadOnlyList<DocumentExplorerNodeDto> Children { get; init; } = Array.Empty<DocumentExplorerNodeDto>();
}

public record DocumentExplorerDto
{
    public IReadOnlyList<DocumentExplorerNodeDto> Tree { get; init; } = Array.Empty<DocumentExplorerNodeDto>();
    public IReadOnlyList<ControlledDocumentListItemDto> Recent { get; init; } = Array.Empty<ControlledDocumentListItemDto>();
    public IReadOnlyList<ControlledDocumentListItemDto> Pinned { get; init; } = Array.Empty<ControlledDocumentListItemDto>();
}

public record DocumentSearchResultDto
{
    public Guid DocumentId { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string MatchContext { get; init; } = string.Empty;
    public string MatchLocation { get; init; } = string.Empty;
    public double Score { get; init; }
    public string? Route { get; init; }
}

public record DocumentHealthDashboardDto
{
    public int HealthyDocuments { get; init; }
    public int NeedsReview { get; init; }
    public int WeakMapping { get; init; }
    public int LowAcknowledgement { get; init; }
    public int MissingSections { get; init; }
    public int AiReviewPending { get; init; }
    public int HighImpactDocuments { get; init; }
    public int RecentlyModified { get; init; }
    public int TopReferencedDocuments { get; init; }
    public int AffectingAccreditation { get; init; }
    public IReadOnlyList<DocumentInsightDto> Insights { get; init; } = Array.Empty<DocumentInsightDto>();
    public IReadOnlyList<ControlledDocumentListItemDto> TopReferenced { get; init; } = Array.Empty<ControlledDocumentListItemDto>();
}

public record DocumentInsightDto
{
    public string InsightType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? DocumentId { get; init; }
}

public record LinkedAssetDto
{
    public string AssetType { get; init; } = string.Empty;
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Route { get; init; }
}

public record CrossReferenceSuggestionDto
{
    public string ReferenceType { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string InsertText { get; init; } = string.Empty;
    public string? Route { get; init; }
}
