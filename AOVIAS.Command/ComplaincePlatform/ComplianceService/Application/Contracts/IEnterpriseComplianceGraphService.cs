using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

/// <summary>Sprint 17A — Enterprise Compliance Graph read-model projection.</summary>
public interface IEnterpriseComplianceGraphService
{
    Task<GraphEntitySummaryDto?> GetEntityAsync(Guid organizationId, Guid entityId, CancellationToken ct = default);
    Task<GraphRelationshipsDto?> GetRelationshipsAsync(Guid organizationId, Guid entityId, CancellationToken ct = default);
    Task<GraphTimelineDto?> GetTimelineAsync(Guid organizationId, Guid entityId, CancellationToken ct = default);
    Task<GraphImpactDto?> GetImpactAsync(Guid organizationId, Guid entityId, CancellationToken ct = default);
    Task<GraphDependenciesDto?> GetDependenciesAsync(Guid organizationId, Guid entityId, CancellationToken ct = default);
    Task<GraphSearchDto> SearchAsync(Guid organizationId, string? query, string? entityType, CancellationToken ct = default);
    Task<GraphNavigationDto?> GetNavigationAsync(Guid organizationId, Guid entityId, CancellationToken ct = default);
    Task<GraphTraceabilityDto?> GetTraceabilityAsync(Guid organizationId, Guid? hospitalId, Guid entityId, CancellationToken ct = default);
    Task<GraphExecutiveImpactDto> GetExecutiveImpactAsync(Guid organizationId, CancellationToken ct = default);
    Task<GraphAnalyticsDto> GetAnalyticsAsync(Guid organizationId, CancellationToken ct = default);
    Task<ComplianceGraphDto> GetVisualGraphAsync(Guid organizationId, Guid? centerEntityId, CancellationToken ct = default);
}
