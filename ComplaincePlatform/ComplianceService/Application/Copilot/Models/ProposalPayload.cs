namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Structured payload for an action proposal.
/// Serialized to JSON and stored in <see cref="Domain.Copilot.CopilotActionProposal.PayloadJson"/>.
/// Each proposal type uses a subset of these fields.
/// </summary>
public sealed class ProposalPayload
{
    // ── Task creation payload ─────────────────────────────────────────────────
    public string? TaskTitle { get; init; }
    public string? TaskDescription { get; init; }
    public Guid? AssignedUserId { get; init; }
    public DateTime? DueDate { get; init; }
    public Guid? DeficiencyId { get; init; }
    public string? Priority { get; init; }

    // ── Finding creation payload ──────────────────────────────────────────────
    public string? FindingTitle { get; init; }
    public string? FindingDescription { get; init; }
    /// <summary>Maps to FindingSeverity: "critical", "major", "minor", "moderate".</summary>
    public string? FindingSeverity { get; init; }
    /// <summary>Maps to FindingDepartment: "clinical", "hr", "security", "admin".</summary>
    public string? FindingCategory { get; init; }

    // ── CAPA payload ──────────────────────────────────────────────────────────
    public string? CapaCorrectiveAction { get; init; }
    public string? CapaPreventiveAction { get; init; }
    public string? CapaRootCause { get; init; }
    public DateTime? CapaTargetDate { get; init; }
    public string? CapaResponsibleOfficer { get; init; }

    // ── Effectiveness review payload ──────────────────────────────────────────
    public DateTime? EffectivenessReviewDate { get; init; }
    public string? EffectivenessReviewerName { get; init; }

    // ── Evidence checklist / readiness plan payload ───────────────────────────
    public string? PlanTitle { get; init; }
    public string? PlanObjective { get; init; }
    public Guid? FrameworkId { get; init; }
    public Guid? StageId { get; init; }
    public IReadOnlyList<string>? ObligationRefs { get; init; }

    // ── Executive briefing payload ────────────────────────────────────────────
    public string? BriefingTitle { get; init; }
    public Guid? DraftId { get; init; }
    public IReadOnlyList<string>? RecipientEmails { get; init; }

    // ── Source traceability fields (always included) ──────────────────────────
    public Guid? SourceRecommendationId { get; init; }
    public Guid? SourceDraftId { get; init; }
    public string? RationaleSnippet { get; init; }
    public string? ExpectedOutcome { get; init; }
}
