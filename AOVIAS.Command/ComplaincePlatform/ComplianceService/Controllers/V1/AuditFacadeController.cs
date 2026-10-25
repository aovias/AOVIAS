using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Facade replacing legacy AuditService generate/get for AIOrchestrator and Gateway.
/// SSOT is ComplianceService AuditSession.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class AuditFacadeController : ControllerBase
{
    private readonly IAuditGenerateFacadeService _facade;
    private readonly ILogger<AuditFacadeController> _logger;

    public AuditFacadeController(IAuditGenerateFacadeService facade, ILogger<AuditFacadeController> logger)
    {
        _facade = facade;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<GenerateAuditFacadeResponse>>> Generate(
        [FromBody] GenerateAuditFacadeRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var dto = await _facade.GenerateAsync(request, ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid audit generate request");
            return BadRequest(ApiResponseFactory.Failure<GenerateAuditFacadeResponse>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<ApiResponse<GenerateAuditFacadeResponse>>> Get(Guid sessionId, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var dto = await _facade.GetAsync(sessionId, ct);
        if (dto == null)
            return NotFound(ApiResponseFactory.Failure<GenerateAuditFacadeResponse>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
    }
}
