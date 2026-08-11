using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceService.Controllers.Internal;

[ApiController]
[Route("internal/v1/sla")]
[Authorize(Policy = "InternalOnly")]
public class InternalSlaController : ControllerBase
{
    private readonly ISlaPolicyService _policyService;
    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly IEscalationService _escalationService;

    public InternalSlaController(
        ISlaPolicyService policyService,
        ISlaEvaluationService slaEvaluation,
        IEscalationService escalationService)
    {
        _policyService = policyService;
        _slaEvaluation = slaEvaluation;
        _escalationService = escalationService;
    }

    [HttpGet("policies")]
    public async Task<IActionResult> ListPolicies(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? hospitalId,
        CancellationToken ct)
    {
        var policies = await _policyService.ListPoliciesAsync(organizationId, hospitalId, ct);
        return Ok(policies);
    }

    [HttpPost("policies")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreateSlaPolicyRequestDto body, CancellationToken ct)
    {
        if (body.OrganizationId == Guid.Empty || string.IsNullOrWhiteSpace(body.Name))
        {
            return BadRequest(new { message = "OrganizationId and Name are required." });
        }

        var created = await _policyService.CreatePolicyAsync(body, ct);
        return Ok(created);
    }

    [HttpGet("deficiencies/{id:guid}")]
    public async Task<IActionResult> GetDeficiencySla(Guid id, CancellationToken ct)
    {
        var sla = await _slaEvaluation.EvaluateAsync(id, persist: true, ct);
        return Ok(sla);
    }

    [HttpGet("deficiencies/{id:guid}/escalations")]
    public async Task<IActionResult> GetDeficiencyEscalations(Guid id, CancellationToken ct)
    {
        var history = await _escalationService.GetHistoryAsync(id, ct);
        return Ok(history);
    }

    [HttpPost("deficiencies/{id:guid}/acknowledge-escalation")]
    public async Task<IActionResult> AcknowledgeEscalation(
        Guid id,
        [FromBody] AcknowledgeEscalationRequestDto body,
        CancellationToken ct)
    {
        if (body.EscalationId == Guid.Empty)
        {
            return BadRequest(new { message = "EscalationId is required." });
        }

        var result = await _escalationService.AcknowledgeAsync(
            id, body.EscalationId, User.Identity?.Name, ct);

        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] Guid organizationId, CancellationToken ct) =>
        Ok(await _escalationService.GetMetricsAsync(organizationId, ct));
}

public class AcknowledgeEscalationRequestDto
{
    public Guid EscalationId { get; set; }
}
