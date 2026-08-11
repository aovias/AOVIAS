using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Request body for POST /copilot/proposals.</summary>
public sealed class CreateProposalRequestDto
{
    public Guid RecommendationId { get; init; }
    public Guid? DraftId { get; init; }
    public ProposalType ProposalType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = "{}";
    public int? TtlDays { get; init; }
}

/// <summary>Request body for POST /copilot/proposals/from-recommendation/{id}.</summary>
public sealed class GenerateProposalFromRecommendationDto
{
    public ProposalType? ProposalTypeOverride { get; init; }
    public string? TitleOverride { get; init; }
    public string? AdditionalContext { get; init; }
    public Guid? AssignedUserId { get; init; }
    public DateTime? DueDate { get; init; }
}

/// <summary>Request body for POST /copilot/proposals/from-draft/{id}.</summary>
public sealed class GenerateProposalFromDraftDto
{
    public ProposalType? ProposalTypeOverride { get; init; }
    public string? TitleOverride { get; init; }
    public string? AdditionalContext { get; init; }
}

/// <summary>Request body for POST /copilot/proposals/{id}/approve.</summary>
public sealed class ApproveProposalRequestDto
{
    public string? Notes { get; init; }
}

/// <summary>Request body for POST /copilot/proposals/{id}/reject.</summary>
public sealed class RejectProposalRequestDto
{
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Result DTO returned after executing a proposal.</summary>
public sealed class ProposalExecutionResultDto
{
    public Guid ProposalId { get; init; }
    public bool Success { get; init; }
    public Guid? ResultEntityId { get; init; }
    public string? ResultEntityType { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }
}
