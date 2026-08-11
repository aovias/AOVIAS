using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// [DEPRECATED] This controller is deprecated. Use FrameworkController.EnrollHospitalInFramework instead.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/regulations")]
[Obsolete("Use POST /api/v{version}/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll instead")]
public class RegulationsController : ControllerBase
{
    private readonly IRegulationService _service;

    public RegulationsController(IRegulationService service)
    {
        _service = service;
    }

    /// <summary>
    /// [DEPRECATED] Use POST /api/v{version}/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll instead.
    /// </summary>
    [HttpPost("enroll")]
    [Obsolete("Use FrameworkController.EnrollHospitalInFramework instead")]
    public async Task<ActionResult<ApiResponse<object>>> Enroll([FromBody] EnrollRegulationRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        var regulationId = await _service.EnrollAsync(request, ct);

        var data = new { RegulationId = regulationId };

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            (object)data,
            traceId);

        var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(successMeta.HttpStatus, response);
    }
}
