namespace ComplianceService.Application.DTOs;

/// <summary>Sprint 17A — Enterprise Compliance Graph read-model DTOs.</summary>

public record GraphEntitySummaryDto
{
    public Guid EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string NodeId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Owner { get; init; }
    public string? Department { get; init; }
    public string? Route { get; init; }
    public string? Health { get; init; }
    public string? Risk { get; init; }
    public int RelatedObjectCount { get; init; }
    public DateTime? LastUpdated { get; init; }
}

public record GraphRelationshipDto
{
    public string Id { get; init; } = string.Empty;
    public Guid SourceEntityId { get; init; }
    public string SourceEntityType { get; init; } = string.Empty;
    public string SourceNodeId { get; init; } = string.Empty;
    public string SourceLabel { get; init; } = string.Empty;
    public Guid TargetEntityId { get; init; }
    public string TargetEntityType { get; init; } = string.Empty;
    public string TargetNodeId { get; init; } = string.Empty;
    public string TargetLabel { get; init; } = string.Empty;
    public string RelationshipType { get; init; } = string.Empty;
    public string Direction { get; init; } = "Outgoing";
    public string? TargetRoute { get; init; }
    public bool IsBlocking { get; init; }
}

public record GraphRelationshipsDto
{
    public GraphEntitySummaryDto Entity { get; init; } = new();
    public IReadOnlyList<GraphRelationshipDto> Incoming { get; init; } = Array.Empty<GraphRelationshipDto>();
    public IReadOnlyList<GraphRelationshipDto> Outgoing { get; init; } = Array.Empty<GraphRelationshipDto>();
    public ComplianceGraphDto VisualGraph { get; init; } = new();
    public string ImpactSummary { get; init; } = string.Empty;
}

public record GraphTimelineEntryDto
{
    public DateTime OccurredAt { get; init; }
    public string Layer { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Detail { get; init; }
    public string? ActorName { get; init; }
    public Guid? EntityId { get; init; }
    public string? EntityType { get; init; }
    public string? Route { get; init; }
}

public record GraphTimelineDto
{
    public GraphEntitySummaryDto Entity { get; init; } = new();
    public IReadOnlyList<GraphTimelineEntryDto> Entries { get; init; } = Array.Empty<GraphTimelineEntryDto>();
}

public record GraphImpactItemDto
{
    public string Category { get; init; } = string.Empty;
    public int Count { get; init; }
    public IReadOnlyList<GraphEntitySummaryDto> Examples { get; init; } = Array.Empty<GraphEntitySummaryDto>();
}

public record GraphImpactDto
{
    public GraphEntitySummaryDto Entity { get; init; } = new();
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<GraphImpactItemDto> AffectedAreas { get; init; } = Array.Empty<GraphImpactItemDto>();
}

public record GraphDependencyItemDto
{
    public string Category { get; init; } = string.Empty;
    public string RelationshipType { get; init; } = string.Empty;
    public int Count { get; init; }
    public bool IsBlocking { get; init; }
    public IReadOnlyList<string> Examples { get; init; } = Array.Empty<string>();
}

public record GraphDependenciesDto
{
    public GraphEntitySummaryDto Entity { get; init; } = new();
    public bool CanArchive { get; init; } = true;
    public bool CanSupersede { get; init; } = true;
    public bool CanDelete { get; init; }
    public bool CanClose { get; init; } = true;
    public string WarningMessage { get; init; } = string.Empty;
    public IReadOnlyList<GraphDependencyItemDto> Blocking { get; init; } = Array.Empty<GraphDependencyItemDto>();
    public IReadOnlyList<GraphDependencyItemDto> Downstream { get; init; } = Array.Empty<GraphDependencyItemDto>();
    public IReadOnlyList<GraphDependencyItemDto> Upstream { get; init; } = Array.Empty<GraphDependencyItemDto>();
}

public record GraphSearchResultItemDto
{
    public Guid EntityId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public int Score { get; init; }
}

public record GraphSearchGroupDto
{
    public string EntityType { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public IReadOnlyList<GraphSearchResultItemDto> Items { get; init; } = Array.Empty<GraphSearchResultItemDto>();
}

public record GraphSearchDto
{
    public string Query { get; init; } = string.Empty;
    public int TotalResults { get; init; }
    public IReadOnlyList<GraphSearchGroupDto> Groups { get; init; } = Array.Empty<GraphSearchGroupDto>();
}

public record GraphNavigationStepDto
{
    public int Order { get; init; }
    public string Label { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string Route { get; init; } = string.Empty;
    public string RelationshipType { get; init; } = string.Empty;
}

public record GraphNavigationDto
{
    public GraphEntitySummaryDto Entity { get; init; } = new();
    public IReadOnlyList<GraphNavigationStepDto> ForwardPath { get; init; } = Array.Empty<GraphNavigationStepDto>();
    public IReadOnlyList<GraphNavigationStepDto> BackwardPath { get; init; } = Array.Empty<GraphNavigationStepDto>();
    public IReadOnlyList<GraphEntitySummaryDto> RelatedShortcuts { get; init; } = Array.Empty<GraphEntitySummaryDto>();
}

public record GraphTraceabilityStepDto
{
    public int Order { get; init; }
    public string Layer { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? EntityId { get; init; }
    public string? EntityType { get; init; }
    public string? Route { get; init; }
    public string? Detail { get; init; }
}

public record GraphTraceabilityDto
{
    public GraphEntitySummaryDto Root { get; init; } = new();
    public IReadOnlyList<GraphTraceabilityStepDto> Chain { get; init; } = Array.Empty<GraphTraceabilityStepDto>();
    public string Summary { get; init; } = string.Empty;
    public decimal? ReadinessScore { get; init; }
}

public record GraphExecutiveImpactDto
{
    public IReadOnlyList<GraphEntitySummaryDto> TopConnectedRisks { get; init; } = Array.Empty<GraphEntitySummaryDto>();
    public IReadOnlyList<GraphEntitySummaryDto> MostReferencedDocuments { get; init; } = Array.Empty<GraphEntitySummaryDto>();
    public IReadOnlyList<GraphEntitySummaryDto> MostInfluentialCapas { get; init; } = Array.Empty<GraphEntitySummaryDto>();
    public IReadOnlyList<string> HighDependencyAreas { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DepartmentsWithHighestImpact { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ComplianceBottlenecks { get; init; } = Array.Empty<string>();
    public IReadOnlyList<GraphImpactItemDto> CrossDomainHeatMap { get; init; } = Array.Empty<GraphImpactItemDto>();
}

public record GraphAnalyticsDto
{
    public int TotalNodes { get; init; }
    public int TotalRelationships { get; init; }
    public decimal RelationshipDensity { get; init; }
    public int MostConnectedAssets { get; init; }
    public decimal CrossDomainCoverage { get; init; }
    public decimal DependencyRisk { get; init; }
    public decimal TraceabilityCompleteness { get; init; }
    public IReadOnlyList<GraphImpactItemDto> DomainCoverage { get; init; } = Array.Empty<GraphImpactItemDto>();
}
