using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/shadow")]
[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
public sealed class ShadowDiffController : ControllerBase
{
    private readonly IShadowComparisonService _shadowComparisonService;

    public ShadowDiffController(IShadowComparisonService shadowComparisonService)
    {
        _shadowComparisonService = shadowComparisonService;
    }

    [HttpGet("mismatches")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ShadowMismatchDto>>>> GetMismatches(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var list = await _shadowComparisonService.GetRecentMismatchesAsync(take, cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }
}
