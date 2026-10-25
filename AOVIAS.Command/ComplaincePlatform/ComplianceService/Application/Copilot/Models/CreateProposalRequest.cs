using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Models;

/// <summary>Internal request to create a proposal (used by ActionProposalGenerator).</summary>
public sealed class CreateProposalRequest
{
    public required Guid OrganizationId { get; init; }
    public required Guid RecommendationId { get; init; }
    public required ProposalType ProposalType { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ProposalPayload Payload { get; init; }
    public Guid? DraftId { get; init; }
    public string ProposedBy { get; init; } = "Copilot";
    public TimeSpan? Ttl { get; init; }
}

/// <summary>Optional overrides when generating a proposal from a recommendation or draft.</summary>
public sealed class GenerateProposalRequest
{
    public ProposalType? ProposalTypeOverride { get; init; }
    public string? TitleOverride { get; init; }
    public string? AdditionalContext { get; init; }
    public Guid? AssignedUserId { get; init; }
    public DateTime? DueDate { get; init; }
}
