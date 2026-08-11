using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.DTOs;

/// <summary>
/// Request body for creating an audit finding against an objective element (NABH).
/// </summary>
public record CreateAuditFindingRequest
{
    public Guid ObjectiveElementId { get; init; }
    public NonConformanceType Type { get; init; }
    public string? Description { get; init; }
    public DateTime? DueDate { get; init; }
}

/// <summary>
/// Audit finding returned to clients; always tied to a <see cref="ObjectiveElementId"/>.
/// </summary>
public record AuditFindingDto
{
    public Guid Id { get; init; }
    public Guid JourneyId { get; init; }
    public Guid ObjectiveElementId { get; init; }
    public NonConformanceType Type { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime DueDate { get; init; }
    public bool IsResolved { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
