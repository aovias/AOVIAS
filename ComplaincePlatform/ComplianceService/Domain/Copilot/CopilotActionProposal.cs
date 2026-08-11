using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Domain.Copilot;

/// <summary>
/// Represents an AI-proposed operational action awaiting human approval.
/// The AI proposes — humans decide — existing workflows execute.
///
/// Trust model:
///   AI may create proposals (this entity).
///   AI may NOT approve, reject, or execute proposals.
///   Only authenticated human users advance the proposal lifecycle.
///
/// Lifecycle:
///   Proposed → Approved → Executed
///   Proposed → Rejected   (terminal)
///   Proposed → Expired    (terminal — TTL elapsed)
/// </summary>
public class CopilotActionProposal : AggregateRoot
{
    // ── Identity ─────────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }

    // ── Source traceability ───────────────────────────────────────────────────
    public Guid RecommendationId { get; private set; }
    public Guid? DraftId { get; private set; }

    // ── Classification ────────────────────────────────────────────────────────
    public ProposalType ProposalType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// JSON-encoded payload for the target service call.
    /// Contains all parameters needed to execute the proposed action.
    /// </summary>
    public string PayloadJson { get; private set; } = string.Empty;

    // ── Generation ────────────────────────────────────────────────────────────
    public string ProposedBy { get; private set; } = string.Empty;
    public DateTime ProposedAtUtc { get; private set; }

    /// <summary>UTC timestamp after which the proposal auto-expires if not actioned.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    // ── Approval ──────────────────────────────────────────────────────────────
    public ProposalStatus Status { get; private set; } = ProposalStatus.Proposed;
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public Guid? RejectedBy { get; private set; }
    public DateTime? RejectedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }

    // ── Execution result (Sprint 13D implementation recommendation) ───────────
    /// <summary>ID of the resulting entity created during execution (TaskId, FindingId, etc.).</summary>
    public Guid? ResultEntityId { get; private set; }

    /// <summary>Type of the resulting entity (e.g., "ComplianceTask", "Finding").</summary>
    public string? ResultEntityType { get; private set; }

    /// <summary>Human-readable notes recorded by the execution layer.</summary>
    public string? ExecutionNotes { get; private set; }

    public DateTime? ExecutedAtUtc { get; private set; }
    public Guid? ExecutedBy { get; private set; }

    // ── EF Core constructor ───────────────────────────────────────────────────
    private CopilotActionProposal() { }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>Creates a new proposal. The AI calls this; it does NOT approve or execute.</summary>
    public static CopilotActionProposal Create(
        Guid organizationId,
        Guid recommendationId,
        ProposalType proposalType,
        string title,
        string description,
        string payloadJson,
        string proposedBy = "Copilot",
        Guid? draftId = null,
        TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        var now = DateTime.UtcNow;
        return new CopilotActionProposal
        {
            Id               = Guid.NewGuid(),
            OrganizationId   = organizationId,
            RecommendationId = recommendationId,
            DraftId          = draftId,
            ProposalType     = proposalType,
            Title            = title,
            Description      = description,
            PayloadJson      = payloadJson,
            ProposedBy       = proposedBy,
            ProposedAtUtc    = now,
            ExpiresAtUtc     = now.Add(ttl ?? TimeSpan.FromDays(7)),
            Status           = ProposalStatus.Proposed,
        };
    }

    // ── Human lifecycle transitions (only humans may call these) ─────────────

    /// <summary>An authorized human approves this proposal.</summary>
    public void Approve(Guid approvingUserId)
    {
        GuardProposed("approve");

        Status        = ProposalStatus.Approved;
        ApprovedBy    = approvingUserId;
        ApprovedAtUtc = DateTime.UtcNow;
    }

    /// <summary>An authorized human rejects this proposal.</summary>
    public void Reject(Guid rejectingUserId, string reason)
    {
        GuardProposed("reject");

        Status           = ProposalStatus.Rejected;
        RejectedBy       = rejectingUserId;
        RejectedAtUtc    = DateTime.UtcNow;
        RejectionReason  = reason ?? string.Empty;
    }

    /// <summary>Records the result of executing an approved proposal.</summary>
    public void MarkExecuted(
        Guid executingUserId,
        Guid? resultEntityId,
        string? resultEntityType,
        string? executionNotes = null)
    {
        if (Status != ProposalStatus.Approved)
            throw new InvalidOperationException(
                $"Only Approved proposals can be executed. Current status: {Status}.");

        Status           = ProposalStatus.Executed;
        ExecutedBy       = executingUserId;
        ExecutedAtUtc    = DateTime.UtcNow;
        ResultEntityId   = resultEntityId;
        ResultEntityType = resultEntityType;
        ExecutionNotes   = executionNotes;
    }

    /// <summary>Expires the proposal (TTL elapsed — called by a scheduled job).</summary>
    public void Expire()
    {
        if (Status != ProposalStatus.Proposed)
            return;

        Status = ProposalStatus.Expired;
    }

    // ── Guards ────────────────────────────────────────────────────────────────

    private void GuardProposed(string action)
    {
        if (Status != ProposalStatus.Proposed)
            throw new InvalidOperationException(
                $"Only Proposed proposals can be {action}d. Current status: {Status}.");

        if (DateTime.UtcNow > ExpiresAtUtc)
            throw new InvalidOperationException(
                $"Proposal has expired and cannot be {action}d.");
    }
}
