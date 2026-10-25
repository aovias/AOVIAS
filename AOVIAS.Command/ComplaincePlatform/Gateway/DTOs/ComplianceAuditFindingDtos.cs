using Shared.Infrastructure.Domain.Enums;

namespace Gateway.DTOs;

/// <summary>
/// Gateway contract for creating a compliance journey audit finding (objective element–based).
/// Must stay aligned with ComplianceService <c>CreateAuditFindingRequest</c>.
/// </summary>
public record CreateComplianceAuditFindingRequest
{
    public Guid ObjectiveElementId { get; init; }
    public NonConformanceType Type { get; init; }
    public string? Description { get; init; }
    public DateTime? DueDate { get; init; }
}
