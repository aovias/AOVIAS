using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-schedule")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public sealed class AuditScheduleController : ControllerBase
{
    private readonly IAuditScheduleService _schedule;
    private readonly IUserContextService _userContext;
    private readonly ILogger<AuditScheduleController> _logger;

    public AuditScheduleController(
        IAuditScheduleService schedule,
        IUserContextService userContext,
        ILogger<AuditScheduleController> logger)
    {
        _schedule = schedule;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ScheduledAuditDto>>>> List(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<ScheduledAuditDto>>(traceId);

        var list = await _schedule.ListAsync(orgId, new AuditCalendarQuery
        {
            Year = year,
            Month = month,
            HospitalId = hospitalId ?? (_userContext.HospitalId == Guid.Empty ? null : _userContext.HospitalId),
            FrameworkId = frameworkId,
            From = from,
            To = to,
        }, ct);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ScheduledAuditDto>>>> Calendar(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] Guid? hospitalId,
        [FromQuery] Guid? frameworkId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<IReadOnlyList<ScheduledAuditDto>>(traceId);

        var list = await _schedule.GetCalendarAsync(orgId, new AuditCalendarQuery
        {
            Year = year,
            Month = month,
            HospitalId = hospitalId ?? (_userContext.HospitalId == Guid.Empty ? null : _userContext.HospitalId),
            FrameworkId = frameworkId,
            From = from,
            To = to,
        }, ct);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, list, traceId));
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin + "," + ApplicationRoles.MANAGER)]
    public async Task<ActionResult<ApiResponse<ScheduledAuditDto>>> Create(
        [FromBody] CreateScheduledAuditRequest request, CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;
        var orgId = _userContext.OrganizationId;
        if (orgId == Guid.Empty)
            return OrgMissing<ScheduledAuditDto>(traceId);

        try
        {
            var dto = await _schedule.CreateAsync(orgId, _userContext.HospitalId, request, ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, dto, traceId));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid scheduled audit create");
            return BadRequest(ApiResponseFactory.Failure<ScheduledAuditDto>(ApiResponseCode.VALIDATION_FAILED, traceId));
        }
    }

    private ActionResult<ApiResponse<T>> OrgMissing<T>(string traceId)
    {
        var r = ApiResponseFactory.Failure<T>(ApiResponseCode.ORGANIZATION_CONTEXT_MISSING, traceId);
        var m = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
        return StatusCode(m.HttpStatus, r);
    }
}
