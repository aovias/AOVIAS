namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Read model for a proposal audit trail event.</summary>
public sealed class ProposalAuditEventDto
{
    public Guid Id { get; init; }
    public Guid ProposalId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? ActorId { get; init; }
    public string? ActorEmail { get; init; }
    public string? Notes { get; init; }
    public string? ContextJson { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
