using System.Text.Json;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Converts recommendations and drafts into <see cref="CopilotActionProposal"/> entities.
/// Generates only — never approves or executes.
/// </summary>
public sealed class ActionProposalGenerator : IActionProposalGenerator
{
    private readonly ICopilotRecommendationRepository _recommendations;
    private readonly IAiDraftRepository               _drafts;
    private readonly ICopilotActionProposalRepository _proposals;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = false,
    };

    public ActionProposalGenerator(
        ICopilotRecommendationRepository recommendations,
        IAiDraftRepository               drafts,
        ICopilotActionProposalRepository proposals)
    {
        _recommendations = recommendations;
        _drafts           = drafts;
        _proposals        = proposals;
    }

    // ── From Recommendation ───────────────────────────────────────────────────

    public async Task<CopilotActionProposalDto> GenerateFromRecommendationAsync(
        Guid recommendationId,
        Guid organizationId,
        GenerateProposalRequest? request = null,
        CancellationToken ct = default)
    {
        var recommendation = await _recommendations.GetAsync(recommendationId, ct)
            ?? throw new InvalidOperationException($"Recommendation {recommendationId} not found.");

        var proposalType = request?.ProposalTypeOverride
            ?? MapRecommendationCategoryToProposalType(recommendation.Category);

        var title       = request?.TitleOverride ?? BuildTitle(proposalType, recommendation.Title);
        var description = BuildRecommendationDescription(recommendation, request?.AdditionalContext);

        var payload = BuildRecommendationPayload(recommendation, proposalType, request);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOpts);

        var proposal = CopilotActionProposal.Create(
            organizationId   : organizationId,
            recommendationId : recommendationId,
            proposalType     : proposalType,
            title            : title,
            description      : description,
            payloadJson      : payloadJson,
            proposedBy       : "Copilot");

        await _proposals.CreateAsync(proposal, ct);
        await _proposals.SaveChangesAsync(ct);

        await _proposals.RecordAuditEventAsync(
            ProposalAuditEvent.Record(
                proposal.Id,
                organizationId,
                ProposalAuditEvent.EventTypes.Generated,
                notes: $"Generated from recommendation {recommendationId} (category: {recommendation.Category})"),
            ct);
        await _proposals.SaveChangesAsync(ct);

        return MapToDto(proposal);
    }

    // ── From Draft ────────────────────────────────────────────────────────────

    public async Task<CopilotActionProposalDto> GenerateFromDraftAsync(
        Guid draftId,
        Guid organizationId,
        GenerateProposalRequest? request = null,
        CancellationToken ct = default)
    {
        var draft = await _drafts.GetAsync(draftId, ct)
            ?? throw new InvalidOperationException($"Draft {draftId} not found.");

        var proposalType = request?.ProposalTypeOverride
            ?? MapDraftTypeToProposalType(draft.DraftType);

        var title       = request?.TitleOverride ?? BuildDraftTitle(proposalType, draft.Title);
        var description = BuildDraftDescription(draft, request?.AdditionalContext);

        var recommendationId = draft.RecommendationId ?? Guid.Empty;

        var payload = new ProposalPayload
        {
            SourceDraftId          = draftId,
            SourceRecommendationId = draft.RecommendationId,
            PlanTitle              = title,
            PlanObjective          = description,
            RationaleSnippet       = draft.Summary,
            ExpectedOutcome        = $"Execute action proposed by {draft.DraftType} draft.",
        };
        var payloadJson = JsonSerializer.Serialize(payload, JsonOpts);

        var proposal = CopilotActionProposal.Create(
            organizationId   : organizationId,
            recommendationId : recommendationId,
            proposalType     : proposalType,
            title            : title,
            description      : description,
            payloadJson      : payloadJson,
            proposedBy       : "Copilot",
            draftId          : draftId);

        await _proposals.CreateAsync(proposal, ct);
        await _proposals.SaveChangesAsync(ct);

        await _proposals.RecordAuditEventAsync(
            ProposalAuditEvent.Record(
                proposal.Id,
                organizationId,
                ProposalAuditEvent.EventTypes.Generated,
                notes: $"Generated from draft {draftId} (type: {draft.DraftType})"),
            ct);
        await _proposals.SaveChangesAsync(ct);

        return MapToDto(proposal);
    }

    // ── Custom ────────────────────────────────────────────────────────────────

    public async Task<CopilotActionProposalDto> GenerateCustomAsync(
        CreateProposalRequest request,
        CancellationToken ct = default)
    {
        var payloadJson = JsonSerializer.Serialize(request.Payload, JsonOpts);

        var proposal = CopilotActionProposal.Create(
            organizationId   : request.OrganizationId,
            recommendationId : request.RecommendationId,
            proposalType     : request.ProposalType,
            title            : request.Title,
            description      : request.Description,
            payloadJson      : payloadJson,
            proposedBy       : request.ProposedBy,
            draftId          : request.DraftId,
            ttl              : request.Ttl);

        await _proposals.CreateAsync(proposal, ct);
        await _proposals.SaveChangesAsync(ct);

        await _proposals.RecordAuditEventAsync(
            ProposalAuditEvent.Record(
                proposal.Id,
                request.OrganizationId,
                ProposalAuditEvent.EventTypes.Created,
                notes: $"Custom proposal: {request.ProposalType}"),
            ct);
        await _proposals.SaveChangesAsync(ct);

        return MapToDto(proposal);
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    public static ProposalType MapRecommendationCategoryToProposalType(RecommendationCategory category) =>
        category switch
        {
            RecommendationCategory.Deficiency    => ProposalType.CreateCapa,
            RecommendationCategory.CAPA          => ProposalType.CreateCapa,
            RecommendationCategory.Risk          => ProposalType.CreateTask,
            RecommendationCategory.Accreditation => ProposalType.CreateReadinessPlan,
            RecommendationCategory.Verification  => ProposalType.OpenEvidenceChecklist,
            RecommendationCategory.Evidence      => ProposalType.OpenEvidenceChecklist,
            RecommendationCategory.Executive     => ProposalType.GenerateExecutiveBriefing,
            RecommendationCategory.Analytics     => ProposalType.GenerateExecutiveBriefing,
            RecommendationCategory.Escalation    => ProposalType.CreateFinding,
            RecommendationCategory.Department    => ProposalType.CreateTask,
            _                                    => ProposalType.CreateTask,
        };

    private static ProposalType MapDraftTypeToProposalType(DraftType draftType) =>
        draftType switch
        {
            DraftType.CapaDraft                   => ProposalType.CreateCapa,
            DraftType.ExecutiveComplianceBriefing  => ProposalType.GenerateExecutiveBriefing,
            DraftType.ReadinessSummary             => ProposalType.CreateReadinessPlan,
            DraftType.EvidenceChecklist            => ProposalType.OpenEvidenceChecklist,
            DraftType.AuditPreparationBrief        => ProposalType.CreateFinding,
            DraftType.ManagementReviewSummary      => ProposalType.CreateTask,
            _                                      => ProposalType.CreateTask,
        };

    public static string GetProposalTypeLabel(ProposalType proposalType) =>
        proposalType switch
        {
            ProposalType.CreateTask                  => "Create Compliance Task",
            ProposalType.AssignTask                  => "Assign Task",
            ProposalType.CreateCapa                  => "Create CAPA",
            ProposalType.ScheduleEffectivenessReview => "Schedule Effectiveness Review",
            ProposalType.OpenEvidenceChecklist        => "Open Evidence Checklist",
            ProposalType.GenerateExecutiveBriefing    => "Generate Executive Briefing",
            ProposalType.CreateFinding               => "Create Audit Finding",
            ProposalType.CreateDeficiency            => "Create Deficiency",
            ProposalType.CreateReadinessPlan         => "Create Readiness Plan",
            ProposalType.CreateRemediationPlan       => "Create Remediation Plan",
            _                                        => "Create Action",
        };

    private static string BuildTitle(ProposalType proposalType, string recommendationTitle)
    {
        var label = GetProposalTypeLabel(proposalType);
        return $"{label}: {recommendationTitle}".Truncate(200);
    }

    private static string BuildDraftTitle(ProposalType proposalType, string draftTitle)
    {
        var label = GetProposalTypeLabel(proposalType);
        return $"{label} from Draft: {draftTitle}".Truncate(200);
    }

    private static string BuildRecommendationDescription(
        CopilotRecommendation recommendation,
        string? additionalContext)
    {
        var desc = recommendation.Description ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(additionalContext))
            desc += $"\n\nAdditional context: {additionalContext}";
        return desc.Truncate(2000);
    }

    private static string BuildDraftDescription(AiDraft draft, string? additionalContext)
    {
        var desc = draft.Summary ?? $"Proposal generated from {draft.DraftType} draft.";
        if (!string.IsNullOrWhiteSpace(additionalContext))
            desc += $"\n\nAdditional context: {additionalContext}";
        return desc.Truncate(2000);
    }

    private static ProposalPayload BuildRecommendationPayload(
        CopilotRecommendation recommendation,
        ProposalType proposalType,
        GenerateProposalRequest? request)
    {
        return new ProposalPayload
        {
            SourceRecommendationId = recommendation.Id,
            DeficiencyId           = recommendation.SourceEntityId,
            TaskTitle              = proposalType == ProposalType.CreateTask ? recommendation.Title : null,
            TaskDescription        = proposalType == ProposalType.CreateTask ? recommendation.Description : null,
            AssignedUserId         = request?.AssignedUserId,
            DueDate                = request?.DueDate,
            CapaCorrectiveAction   = proposalType == ProposalType.CreateCapa ? recommendation.Title : null,
            PlanTitle              = proposalType is ProposalType.CreateReadinessPlan or ProposalType.CreateRemediationPlan
                                         ? recommendation.Title : null,
            RationaleSnippet       = recommendation.RationaleJson,
            ExpectedOutcome        = $"Implement: {recommendation.Title}",
        };
    }

    // ── DTO mapping ───────────────────────────────────────────────────────────

    internal static CopilotActionProposalDto MapToDto(CopilotActionProposal p) =>
        new()
        {
            Id                = p.Id,
            OrganizationId    = p.OrganizationId,
            RecommendationId  = p.RecommendationId,
            DraftId           = p.DraftId,
            ProposalType      = GetProposalTypeLabel(p.ProposalType),
            ProposalTypeEnum  = (int)p.ProposalType,
            Title             = p.Title,
            Description       = p.Description,
            PayloadJson       = p.PayloadJson,
            ProposedBy        = p.ProposedBy,
            ProposedAtUtc     = p.ProposedAtUtc,
            ExpiresAtUtc      = p.ExpiresAtUtc,
            Status            = p.Status.ToString(),
            StatusEnum        = (int)p.Status,
            ApprovedBy        = p.ApprovedBy?.ToString(),
            ApprovedAtUtc     = p.ApprovedAtUtc,
            RejectedBy        = p.RejectedBy?.ToString(),
            RejectedAtUtc     = p.RejectedAtUtc,
            RejectionReason   = p.RejectionReason,
            ResultEntityId    = p.ResultEntityId,
            ResultEntityType  = p.ResultEntityType,
            ExecutionNotes    = p.ExecutionNotes,
            ExecutedAtUtc     = p.ExecutedAtUtc,
            ExecutedBy        = p.ExecutedBy?.ToString(),
            IsExpired         = DateTime.UtcNow > p.ExpiresAtUtc,
            CanApprove        = p.Status == ProposalStatus.Proposed && DateTime.UtcNow <= p.ExpiresAtUtc,
            CanReject         = p.Status == ProposalStatus.Proposed && DateTime.UtcNow <= p.ExpiresAtUtc,
            CanExecute        = p.Status == ProposalStatus.Approved,
        };
}

internal static class StringExtensions
{
    internal static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
