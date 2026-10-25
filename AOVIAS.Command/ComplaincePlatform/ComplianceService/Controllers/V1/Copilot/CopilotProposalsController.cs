using ComplianceService.Application.Contracts;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Application.Copilot.Services;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Authorization;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1.Copilot;

/// <summary>
/// Copilot proposals API — proposal lifecycle, approval, rejection, and execution.
/// Sprint 13D: Full approval card framework.
/// Consumed via the Gateway — portal never calls this directly.
///
/// CRITICAL TRUST RULE:
///   AI generates proposals only.
///   Only authenticated human users may approve, reject, or execute.
///   AI is blocked from calling approve/reject/execute endpoints.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/copilot/proposals")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class CopilotProposalsController : ControllerBase
{
    private readonly IActionProposalGenerator          _generator;
    private readonly IApprovalPolicyService            _policy;
    private readonly IProposalExecutionService         _execution;
    private readonly ICopilotActionProposalRepository  _repository;
    private readonly IUserContextService               _userContext;
    private readonly ILogger<CopilotProposalsController> _logger;

    public CopilotProposalsController(
        IActionProposalGenerator            generator,
        IApprovalPolicyService              policy,
        IProposalExecutionService           execution,
        ICopilotActionProposalRepository    repository,
        IUserContextService                 userContext,
        ILogger<CopilotProposalsController> logger)
    {
        _generator   = generator;
        _policy      = policy;
        _execution   = execution;
        _repository  = repository;
        _userContext = userContext;
        _logger      = logger;
    }

    // ── GET /copilot/proposals ────────────────────────────────────────────────

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CopilotActionProposalDto>>>> GetProposals(
        [FromQuery] ProposalStatus? status,
        [FromQuery] ProposalType? proposalType,
        [FromQuery] Guid? recommendationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        var results = await _repository.SearchAsync(
            organizationId, status, proposalType, recommendationId, page, pageSize, cancellationToken);

        var dtos = results.Select(ActionProposalGenerator.MapToDto).ToList();

        return Ok(ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, (IReadOnlyList<CopilotActionProposalDto>)dtos, requestId));
    }

    // ── GET /copilot/proposals/pending ────────────────────────────────────────

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CopilotActionProposalDto>>>> GetPendingProposals(
        CancellationToken cancellationToken = default)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        var results = await _repository.GetPendingAsync(organizationId, cancellationToken);
        var dtos    = results.Select(ActionProposalGenerator.MapToDto).ToList();

        return Ok(ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED, (IReadOnlyList<CopilotActionProposalDto>)dtos, requestId));
    }

    // ── GET /copilot/proposals/{id} ───────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CopilotActionProposalDto>>> GetProposal(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var proposal  = await _repository.GetAsync(id, cancellationToken);

        if (proposal is null)
            return NotFound(ApiResponseFactory.Failure<CopilotActionProposalDto>(
                ApiResponseCode.RESOURCE_NOT_FOUND, requestId));

        // Record view event
        await _repository.RecordAuditEventAsync(
            Domain.Copilot.ProposalAuditEvent.Record(
                proposal.Id, proposal.OrganizationId,
                Domain.Copilot.ProposalAuditEvent.EventTypes.Viewed,
                actorId    : _userContext.UserId.ToString(),
                actorEmail : _userContext.Email),
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED,
            ActionProposalGenerator.MapToDto(proposal), requestId));
    }

    // ── POST /copilot/proposals ───────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CopilotActionProposalDto>>> CreateProposal(
        [FromBody] CreateProposalRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        var request = new CreateProposalRequest
        {
            OrganizationId   = organizationId,
            RecommendationId = dto.RecommendationId,
            DraftId          = dto.DraftId,
            ProposalType     = dto.ProposalType,
            Title            = dto.Title,
            Description      = dto.Description,
            Payload          = new Application.Copilot.Models.ProposalPayload
            {
                SourceRecommendationId = dto.RecommendationId,
                SourceDraftId          = dto.DraftId,
            },
            Ttl = dto.TtlDays.HasValue ? TimeSpan.FromDays(dto.TtlDays.Value) : null,
        };

        var result = await _generator.GenerateCustomAsync(request, cancellationToken);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, requestId));
    }

    // ── POST /copilot/proposals/from-recommendation/{id} ─────────────────────

    [HttpPost("from-recommendation/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CopilotActionProposalDto>>> GenerateFromRecommendation(
        Guid id,
        [FromBody] GenerateProposalFromRecommendationDto? dto,
        CancellationToken cancellationToken = default)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        var overrides = dto is null ? null : new GenerateProposalRequest
        {
            ProposalTypeOverride = dto.ProposalTypeOverride,
            TitleOverride        = dto.TitleOverride,
            AdditionalContext    = dto.AdditionalContext,
            AssignedUserId       = dto.AssignedUserId,
            DueDate              = dto.DueDate,
        };

        var result = await _generator.GenerateFromRecommendationAsync(
            id, organizationId, overrides, cancellationToken);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, requestId));
    }

    // ── POST /copilot/proposals/from-draft/{id} ───────────────────────────────

    [HttpPost("from-draft/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CopilotActionProposalDto>>> GenerateFromDraft(
        Guid id,
        [FromBody] GenerateProposalFromDraftDto? dto,
        CancellationToken cancellationToken = default)
    {
        var requestId      = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;

        var overrides = dto is null ? null : new GenerateProposalRequest
        {
            ProposalTypeOverride = dto.ProposalTypeOverride,
            TitleOverride        = dto.TitleOverride,
            AdditionalContext    = dto.AdditionalContext,
        };

        var result = await _generator.GenerateFromDraftAsync(
            id, organizationId, overrides, cancellationToken);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, requestId));
    }

    // ── POST /copilot/proposals/{id}/approve ──────────────────────────────────

    [HttpPost("{id:guid}/approve")]
    [RequirePermission("Copilot.Proposal.Approve")]
    public async Task<ActionResult<ApiResponse<CopilotActionProposalDto>>> ApproveProposal(
        Guid id,
        [FromBody] ApproveProposalRequestDto? dto,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var userId    = _userContext.UserId;
        var roles     = _userContext.Roles;

        var proposal = await _repository.GetAsync(id, cancellationToken);
        if (proposal is null)
            return NotFound(ApiResponseFactory.Failure<CopilotActionProposalDto>(
                ApiResponseCode.RESOURCE_NOT_FOUND, requestId));

        var check = _policy.CanApprove(proposal, userId, roles);
        if (!check.IsAllowed)
            return StatusCode(403, ApiResponseFactory.Failure<CopilotActionProposalDto>(
                ApiResponseCode.UNAUTHORIZED, requestId));

        proposal.Approve(userId);
        await _repository.UpdateAsync(proposal, cancellationToken);

        await _repository.RecordAuditEventAsync(
            Domain.Copilot.ProposalAuditEvent.Record(
                proposal.Id, proposal.OrganizationId,
                Domain.Copilot.ProposalAuditEvent.EventTypes.Approved,
                actorId    : userId.ToString(),
                actorEmail : _userContext.Email,
                notes      : dto?.Notes),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS,
            ActionProposalGenerator.MapToDto(proposal), requestId));
    }

    // ── POST /copilot/proposals/{id}/reject ───────────────────────────────────

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<CopilotActionProposalDto>>> RejectProposal(
        Guid id,
        [FromBody] RejectProposalRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var userId    = _userContext.UserId;
        var roles     = _userContext.Roles;

        var proposal = await _repository.GetAsync(id, cancellationToken);
        if (proposal is null)
            return NotFound(ApiResponseFactory.Failure<CopilotActionProposalDto>(
                ApiResponseCode.RESOURCE_NOT_FOUND, requestId));

        var check = _policy.CanReject(proposal, userId, roles);
        if (!check.IsAllowed)
            return StatusCode(403, ApiResponseFactory.Failure<CopilotActionProposalDto>(
                ApiResponseCode.UNAUTHORIZED, requestId));

        proposal.Reject(userId, dto.Reason);
        await _repository.UpdateAsync(proposal, cancellationToken);

        await _repository.RecordAuditEventAsync(
            Domain.Copilot.ProposalAuditEvent.Record(
                proposal.Id, proposal.OrganizationId,
                Domain.Copilot.ProposalAuditEvent.EventTypes.Rejected,
                actorId    : userId.ToString(),
                actorEmail : _userContext.Email,
                notes      : dto.Reason),
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS,
            ActionProposalGenerator.MapToDto(proposal), requestId));
    }

    // ── POST /copilot/proposals/{id}/execute ──────────────────────────────────

    [HttpPost("{id:guid}/execute")]
    [RequirePermission("Copilot.Proposal.Execute")]
    public async Task<ActionResult<ApiResponse<ProposalExecutionResultDto>>> ExecuteProposal(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var userId    = _userContext.UserId;

        var result = await _execution.ExecuteAsync(id, userId, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponseFactory.Failure<ProposalExecutionResultDto>(
                ApiResponseCode.VALIDATION_FAILED, requestId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, requestId));
    }

    // ── GET /copilot/proposals/{id}/audit-trail ───────────────────────────────

    [HttpGet("{id:guid}/audit-trail")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProposalAuditEventDto>>>> GetAuditTrail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;

        var events = await _repository.GetAuditTrailAsync(id, cancellationToken);
        var dtos = events.Select(e => new ProposalAuditEventDto
        {
            Id            = e.Id,
            ProposalId    = e.ProposalId,
            EventType     = e.EventType,
            ActorId       = e.ActorId,
            ActorEmail    = e.ActorEmail,
            Notes         = e.Notes,
            ContextJson   = e.ContextJson,
            OccurredAtUtc = e.OccurredAtUtc,
        }).ToList();

        return Ok(ApiResponseFactory.Success(ApiResponseCode.REPORT_GENERATED,
            (IReadOnlyList<ProposalAuditEventDto>)dtos, requestId));
    }
}
