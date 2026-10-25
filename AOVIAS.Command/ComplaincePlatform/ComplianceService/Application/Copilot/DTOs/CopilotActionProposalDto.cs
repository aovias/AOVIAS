namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Read model returned by the Copilot proposals API.</summary>
public sealed class CopilotActionProposalDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }

    // ── Source traceability ───────────────────────────────────────────────────
    public Guid RecommendationId { get; init; }
    public Guid? DraftId { get; init; }

    // ── Classification ────────────────────────────────────────────────────────
    public string ProposalType { get; init; } = string.Empty;
    public int ProposalTypeEnum { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;

    // ── Generation ────────────────────────────────────────────────────────────
    public string ProposedBy { get; init; } = string.Empty;
    public DateTime ProposedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }

    // ── Approval ──────────────────────────────────────────────────────────────
    public string Status { get; init; } = string.Empty;
    public int StatusEnum { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTime? ApprovedAtUtc { get; init; }
    public string? RejectedBy { get; init; }
    public DateTime? RejectedAtUtc { get; init; }
    public string? RejectionReason { get; init; }

    // ── Execution result ──────────────────────────────────────────────────────
    /// <summary>ID of the entity created during execution (e.g., TaskId, FindingId).</summary>
    public Guid? ResultEntityId { get; init; }
    public string? ResultEntityType { get; init; }
    public string? ExecutionNotes { get; init; }
    public DateTime? ExecutedAtUtc { get; init; }
    public string? ExecutedBy { get; init; }

    // ── Computed helpers ──────────────────────────────────────────────────────
    public bool IsExpired { get; init; }
    public bool CanApprove { get; init; }
    public bool CanReject { get; init; }
    public bool CanExecute { get; init; }
}
