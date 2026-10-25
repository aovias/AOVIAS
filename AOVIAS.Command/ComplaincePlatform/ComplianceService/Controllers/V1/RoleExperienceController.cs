using ComplianceService.Application.Contracts;
using ComplianceService.Application.RoleExperience;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>Phase 3 Sprint 1 — Role Experience / Persona Intelligence home.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/role-experience")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
[ValidateHospitalId]
public sealed class RoleExperienceController : ControllerBase
{
    private readonly IRoleExperienceService _service;
    private readonly IUserContextService _userContext;

    public RoleExperienceController(
        IRoleExperienceService service,
        IUserContextService userContext)
    {
        _service = service;
        _userContext = userContext;
    }

    /// <summary>GET /api/v1/role-experience/home</summary>
    [HttpGet("home")]
    public async Task<ActionResult<ApiResponse<RoleExperienceHomeDto>>> GetHome(
        [FromQuery] Guid? frameworkId = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;
        var organizationId = _userContext.OrganizationId;
        var hospitalId = _userContext.HospitalId;

        if (organizationId == Guid.Empty)
        {
            var missing = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(missing.HttpStatus,
                ApiResponseFactory.Failure<RoleExperienceHomeDto>(
                    ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, requestId));
        }

        var home = await _service.GetHomeAsync(
            _userContext.Roles,
            organizationId,
            hospitalId,
            frameworkId,
            cancellationToken);

        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus,
            ApiResponseFactory.Success(ApiResponseCode.SUCCESS, home, requestId));
    }
}
