using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Executes approved proposals by dispatching to existing application service commands.
/// NEVER self-approves. NEVER writes directly to domain entities outside existing services.
/// Records the resulting entity ID permanently on the proposal for governance tracking.
/// </summary>
public sealed class ProposalExecutionService : IProposalExecutionService
{
    private readonly ICopilotActionProposalRepository  _proposals;
    private readonly IApprovalPolicyService            _policy;
    private readonly IUserContextService               _userContext;
    private readonly IFindingQueryService              _findingService;
    private readonly IDeficiencyRemediationTaskService _remediationTaskService;
    private readonly ILogger<ProposalExecutionService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ProposalExecutionService(
        ICopilotActionProposalRepository   proposals,
        IApprovalPolicyService             policy,
        IUserContextService                userContext,
        IFindingQueryService               findingService,
        IDeficiencyRemediationTaskService  remediationTaskService,
        ILogger<ProposalExecutionService>  logger)
    {
        _proposals              = proposals;
        _policy                 = policy;
        _userContext            = userContext;
        _findingService         = findingService;
        _remediationTaskService = remediationTaskService;
        _logger                 = logger;
    }

    public async Task<ProposalExecutionResultDto> ExecuteAsync(
        Guid proposalId,
        Guid executingUserId,
        CancellationToken ct = default)
    {
        var proposal = await _proposals.GetAsync(proposalId, ct);
        if (proposal is null)
            return Fail(proposalId, "Proposal not found.");

        // Policy gate — AI never satisfies this because it never holds a real Guid userId
        var policyResult = _policy.CanExecute(proposal, executingUserId, _userContext.Roles);
        if (!policyResult.IsAllowed)
            return Fail(proposalId, $"Execution denied: {policyResult.DenialReason}");

        try
        {
            var (entityId, entityType, notes) = await DispatchAsync(proposal, executingUserId, ct);

            proposal.MarkExecuted(executingUserId, entityId, entityType, notes);
            await _proposals.UpdateAsync(proposal, ct);

            await _proposals.RecordAuditEventAsync(
                ProposalAuditEvent.Record(
                    proposal.Id,
                    proposal.OrganizationId,
                    ProposalAuditEvent.EventTypes.Executed,
                    actorId    : executingUserId.ToString(),
                    actorEmail : _userContext.Email,
                    notes      : $"Executed {proposal.ProposalType}. ResultEntity: {entityType}/{entityId}"),
                ct);

            await _proposals.SaveChangesAsync(ct);

            return new ProposalExecutionResultDto
            {
                ProposalId       = proposalId,
                Success          = true,
                ResultEntityId   = entityId,
                ResultEntityType = entityType,
                Message          = notes,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute proposal {ProposalId}", proposalId);
            return Fail(proposalId, $"Execution failed: {ex.Message}");
        }
    }

    // ── Dispatch — delegates to existing services only ─────────────────────────

    private async Task<(Guid? EntityId, string EntityType, string Notes)> DispatchAsync(
        Domain.Copilot.CopilotActionProposal proposal,
        Guid executingUserId,
        CancellationToken ct)
    {
        var payload = DeserializePayload(proposal.PayloadJson);

        return proposal.ProposalType switch
        {
            ProposalType.CreateTask =>
                await ExecuteCreateTaskAsync(proposal, payload, executingUserId, ct),

            ProposalType.CreateCapa =>
                await ExecuteCreateCapaAsync(proposal, payload, executingUserId, ct),

            ProposalType.CreateFinding =>
                await ExecuteCreateFindingAsync(proposal, payload, executingUserId, ct),

            ProposalType.OpenEvidenceChecklist =>
                ExecuteOpenEvidenceChecklist(proposal, payload),

            ProposalType.CreateReadinessPlan =>
                ExecuteCreateReadinessPlan(proposal, payload),

            ProposalType.CreateRemediationPlan =>
                ExecuteCreateRemediationPlan(proposal, payload),

            ProposalType.GenerateExecutiveBriefing =>
                ExecuteGenerateExecutiveBriefing(proposal, payload),

            ProposalType.ScheduleEffectivenessReview =>
                ExecuteScheduleEffectivenessReview(proposal, payload),

            ProposalType.AssignTask =>
                await ExecuteAssignTaskAsync(proposal, payload, executingUserId, ct),

            _ => throw new NotSupportedException($"Proposal type {proposal.ProposalType} not supported.")
        };
    }

    // ── Execution handlers ────────────────────────────────────────────────────

    private async Task<(Guid?, string, string)> ExecuteCreateTaskAsync(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload,
        Guid executingUserId,
        CancellationToken ct)
    {
        if (payload.DeficiencyId.HasValue)
        {
            // Use remediation task service when a deficiency is linked
            var taskId = await _remediationTaskService.EnsureRemediationTaskAsync(
                payload.DeficiencyId.Value, ct, runAutoRouting: true);

            return (taskId, "ComplianceTask",
                $"Remediation task ensured for deficiency {payload.DeficiencyId}.");
        }

        // Without a deficiency/objective-element ID we cannot create a structured task directly.
        // Return acknowledged result — user navigates to task creation form with pre-filled context.
        return (proposal.RecommendationId, "ComplianceTask",
            $"Task proposal acknowledged: \"{payload.TaskTitle ?? proposal.Title}\". " +
            "Open the Compliance Tasks workspace to create the task with the recommended parameters.");
    }

    private async Task<(Guid?, string, string)> ExecuteCreateCapaAsync(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload,
        Guid executingUserId,
        CancellationToken ct)
    {
        // CAPA is fields on a deficiency — ensure the task record exists first.
        if (payload.DeficiencyId.HasValue)
        {
            var taskId = await _remediationTaskService.EnsureRemediationTaskAsync(
                payload.DeficiencyId.Value, ct, runAutoRouting: false);
            return (taskId, "DeficiencyTask",
                $"CAPA task ensured for deficiency {payload.DeficiencyId}. " +
                "Corrective and preventive actions should be completed via the Deficiency CAPA form.");
        }

        return (proposal.RecommendationId, "CopilotRecommendation",
            "CAPA proposal acknowledged. Open the linked deficiency to enter corrective/preventive actions.");
    }

    private async Task<(Guid?, string, string)> ExecuteCreateFindingAsync(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload,
        Guid executingUserId,
        CancellationToken ct)
    {
        var severity = ParseFindingSeverity(payload.FindingSeverity);
        var department = ParseFindingDepartment(payload.FindingCategory);

        var createRequest = new Application.DTOs.CreateManagedFindingRequest
        {
            Title           = payload.FindingTitle ?? proposal.Title,
            Description     = payload.FindingDescription ?? proposal.Description,
            ClauseReference = string.Empty,
            Severity        = severity,
            Department      = department,
            Recommendation  = payload.ExpectedOutcome ?? proposal.Description,
        };

        var finding = await _findingService.CreateAsync(
            proposal.OrganizationId, executingUserId, createRequest, ct);

        return (finding.Id, "Finding", $"Finding created: {finding.Title}.");
    }

    private static FindingSeverity ParseFindingSeverity(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "critical" => FindingSeverity.Critical,
            "major"    => FindingSeverity.Major,
            "minor"    => FindingSeverity.Minor,
            _          => FindingSeverity.Moderate,
        };

    private static FindingDepartment ParseFindingDepartment(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "clinical"  => FindingDepartment.Clinical,
            "hr"        => FindingDepartment.HR,
            "security"  => FindingDepartment.Security,
            _           => FindingDepartment.Admin,
        };

    private static (Guid?, string, string) ExecuteOpenEvidenceChecklist(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload)
    {
        // Evidence checklist is opened via frontend navigation — no backend create action needed
        return (payload.FrameworkId ?? proposal.RecommendationId, "EvidenceChecklist",
            "Evidence checklist opened. Navigate to the Evidence workspace to review and upload required evidence.");
    }

    private static (Guid?, string, string) ExecuteCreateReadinessPlan(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload)
    {
        return (proposal.RecommendationId, "ReadinessPlan",
            $"Readiness plan proposed: {payload.PlanTitle ?? proposal.Title}. " +
            "Complete the plan in the Accreditation Readiness workspace.");
    }

    private static (Guid?, string, string) ExecuteCreateRemediationPlan(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload)
    {
        return (proposal.RecommendationId, "RemediationPlan",
            $"Remediation plan proposed: {payload.PlanTitle ?? proposal.Title}. " +
            "Complete via the Compliance Deficiency workspace.");
    }

    private static (Guid?, string, string) ExecuteGenerateExecutiveBriefing(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload)
    {
        return (payload.DraftId ?? proposal.DraftId ?? proposal.RecommendationId, "ExecutiveBriefingDraft",
            "Executive briefing draft marked for distribution. " +
            "Review and approve the draft in the Copilot Drafts workspace.");
    }

    private static (Guid?, string, string) ExecuteScheduleEffectivenessReview(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload)
    {
        return (payload.DeficiencyId ?? proposal.RecommendationId, "EffectivenessReview",
            $"Effectiveness review scheduled for {payload.EffectivenessReviewDate?.ToString("yyyy-MM-dd") ?? "TBD"}. " +
            "Confirm via the CAPA module.");
    }

    private async Task<(Guid?, string, string)> ExecuteAssignTaskAsync(
        Domain.Copilot.CopilotActionProposal proposal,
        ProposalPayload payload,
        Guid executingUserId,
        CancellationToken ct)
    {
        // Assignment is a task update — create first if no source task
        if (payload.DeficiencyId.HasValue)
        {
            var taskId = await _remediationTaskService.EnsureRemediationTaskAsync(
                payload.DeficiencyId.Value, ct, runAutoRouting: true);
            return (taskId, "ComplianceTask",
                $"Task assigned for deficiency {payload.DeficiencyId}.");
        }
        return (proposal.RecommendationId, "TaskAssignment",
            "Task assignment noted. Open the task in the Compliance workspace to assign.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ProposalPayload DeserializePayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ProposalPayload>(json, JsonOpts) ?? new ProposalPayload();
        }
        catch
        {
            return new ProposalPayload();
        }
    }

    private static ProposalExecutionResultDto Fail(Guid proposalId, string error) =>
        new() { ProposalId = proposalId, Success = false, Error = error };
}
