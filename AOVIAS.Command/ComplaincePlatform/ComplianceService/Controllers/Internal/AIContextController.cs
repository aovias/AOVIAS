using ComplianceService.Application.AI;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.AI.Providers;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/ai")]
[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
public sealed class AIContextController : ControllerBase
{
    private readonly IAIContextBundleService _contextBundleService;
    private readonly IAIExplainContextService _explainService;

    public AIContextController(
        IAIContextBundleService contextBundleService,
        IAIExplainContextService explainService)
    {
        _contextBundleService = contextBundleService;
        _explainService = explainService;
    }

    [HttpPost("context-bundle")]
    public async Task<ActionResult<AIContextBundleDto>> GetContextBundle(
        [FromBody] AIContextBundleRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await _contextBundleService.BuildAsync(request, cancellationToken);
        return Ok(data);
    }

    [HttpPost("context-version/validate")]
    public async Task<ActionResult<AIContextVersionValidationResponseDto>> ValidateContextVersion(
        [FromBody] AIContextVersionValidationRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await _contextBundleService.ValidateVersionAsync(request, cancellationToken);
        return Ok(data);
    }

    [HttpPost("command-preconditions/validate")]
    public async Task<ActionResult<AICommandPreconditionResponseDto>> ValidateCommandPreconditions(
        [FromBody] AICommandPreconditionRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await _contextBundleService.ValidateCommandPreconditionsAsync(request, cancellationToken);
        return Ok(data);
    }

    [HttpPost("explain")]
    public async Task<ActionResult<AIExplainContextResponseDto>> ExplainContext(
        [FromBody] AIExplainContextRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await _explainService.ExplainAsync(request, cancellationToken);
        return Ok(data);
    }

    [HttpPost("readonly-tools/execute")]
    public async Task<ActionResult<AIReadOnlyToolResponseDto>> ExecuteReadOnlyTool(
        [FromBody] AIReadOnlyToolRequestDto request,
        CancellationToken cancellationToken)
    {
        var data = await _explainService.ExecuteReadOnlyToolAsync(request, cancellationToken);
        return Ok(data);
    }

    [HttpGet("validation-gates")]
    public ActionResult<ApiResponse<object>> GetValidationGates()
    {
        var traceId = HttpContext.TraceIdentifier;
        var payload = new
        {
            deterministicScoringIsolation = true,
            aiGroundingRequired = true,
            auditTrailRequired = true
        };
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, payload, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }
}
