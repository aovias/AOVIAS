using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.Commands;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Queries;
using OrganizationService.Application.Services;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;
using System.Security.Claims;

namespace OrganizationService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/organizations/{orgId:guid}/settings")]
[Authorize(Policy = "OrganizationScope")]
public class OrganizationSettingsController(IMediator mediator) : ControllerBase
{
    private Guid ActorUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? Guid.Empty.ToString());

    private bool CanAccessOrganization(Guid orgId)
    {
        if (User.IsInRole(ApplicationRoles.SUPER_ADMIN)) return true;
        var claim = User.FindFirst("organization_id")?.Value;
        return Guid.TryParse(claim, out var userOrgId) && userOrgId == orgId;
    }

    private bool CanEditSettings() =>
        User.IsInRole(ApplicationRoles.SUPER_ADMIN) || User.IsInRole(ApplicationRoles.ORG_ADMIN);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> Get(Guid orgId, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        var result = await mediator.Send(new GetOrganizationSettingsQuery(orgId, ActorUserId(), CanEditSettings()), ct);
        if (result == null)
            return NotFound(ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> Update(Guid orgId, [FromBody] UpdateOrganizationSettingsRequest request, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));
        if (!CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        try
        {
            var result = await mediator.Send(new UpdateOrganizationSettingsCommand(orgId, request, ActorUserId()), ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already in use", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.RESOURCE_CONFLICT, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("logo")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> UploadLogo(Guid orgId, IFormFile file, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId) || !CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));

        await using var stream = file.OpenReadStream();
        try
        {
            var result = await mediator.Send(new UploadOrganizationLogoCommand(orgId, stream, file.FileName, file.ContentType, file.Length, ActorUserId()), ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }
    }

    [HttpDelete("logo")]
    public async Task<ActionResult<ApiResponse<OrganizationSettingsDto>>> RemoveLogo(Guid orgId, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId) || !CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationSettingsDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        var result = await mediator.Send(new RemoveOrganizationLogoCommand(orgId, ActorUserId()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
    }

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo(Guid orgId, [FromServices] Application.Contracts.IOrganizationSettingsAppService settingsService, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId))
            return Forbid();

        var logo = await settingsService.OpenLogoAsync(orgId, ct);
        if (logo == null) return NotFound();

        return File(logo.Value.Stream, logo.Value.ContentType);
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrganizationAuditLogDto>>>> AuditLogs(
        Guid orgId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int take = 200,
        CancellationToken ct = default)
    {
        if (!CanAccessOrganization(orgId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<IReadOnlyList<OrganizationAuditLogDto>>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        var data = await mediator.Send(new ListOrganizationAuditLogsQuery(orgId, from, to, take), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpGet("holidays")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrgHolidayDto>>>> ListHolidays(
        Guid orgId,
        [FromQuery] int? year,
        [FromServices] IOrganizationSettingsAppService settingsService,
        CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<IReadOnlyList<OrgHolidayDto>>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        var data = await settingsService.ListHolidaysAsync(orgId, year, ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
    }

    [HttpPost("holidays")]
    public async Task<ActionResult<ApiResponse<OrgHolidayDto>>> AddHoliday(
        Guid orgId,
        [FromBody] CreateOrgHolidayRequest request,
        [FromServices] IOrganizationSettingsAppService settingsService,
        CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId) || !CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrgHolidayDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        try
        {
            var data = await settingsService.AddHolidayAsync(orgId, request, ActorUserId(), ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, HttpContext.TraceIdentifier));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponseFactory.Failure<OrgHolidayDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(ApiResponseFactory.Failure<OrgHolidayDto>(ApiResponseCode.RESOURCE_CONFLICT, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(ApiResponseFactory.Failure<OrgHolidayDto>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }
    }

    [HttpDelete("holidays/{holidayId:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveHoliday(
        Guid orgId,
        Guid holidayId,
        [FromServices] IOrganizationSettingsAppService settingsService,
        CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId) || !CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<bool>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        var removed = await settingsService.RemoveHolidayAsync(orgId, holidayId, ct);
        if (!removed)
            return NotFound(ApiResponseFactory.Failure<bool>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, true, HttpContext.TraceIdentifier));
    }

    [HttpGet("branding")]
    public async Task<ActionResult<ApiResponse<OrganizationBrandingDto>>> GetBranding(Guid orgId, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        var result = await mediator.Send(new GetOrganizationBrandingQuery(orgId, ActorUserId(), CanEditSettings()), ct);
        if (result == null)
            return NotFound(ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.RESOURCE_NOT_FOUND, HttpContext.TraceIdentifier));

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
    }

    [HttpPut("branding")]
    public async Task<ActionResult<ApiResponse<OrganizationBrandingDto>>> UpdateBranding(Guid orgId, [FromBody] UpdateOrganizationBrandingRequest request, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId) || !CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        try
        {
            var result = await mediator.Send(new UpdateOrganizationBrandingCommand(orgId, request, ActorUserId()), ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("branding/assets/{assetKey}")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<OrganizationBrandingDto>>> UploadBrandingAsset(Guid orgId, string assetKey, IFormFile file, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId) || !CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));

        await using var stream = file.OpenReadStream();
        try
        {
            var result = await mediator.Send(new UploadBrandingAssetCommand(orgId, assetKey, stream, file.FileName, file.ContentType, file.Length, ActorUserId()), ct);
            return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
        }
        catch (InvalidOperationException)
        {
            return BadRequest(ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));
        }
    }

    [HttpDelete("branding/assets/{assetKey}")]
    public async Task<ActionResult<ApiResponse<OrganizationBrandingDto>>> RemoveBrandingAsset(Guid orgId, string assetKey, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId) || !CanEditSettings())
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponseFactory.Failure<OrganizationBrandingDto>(ApiResponseCode.FORBIDDEN, HttpContext.TraceIdentifier));

        var result = await mediator.Send(new RemoveBrandingAssetCommand(orgId, assetKey, ActorUserId()), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, HttpContext.TraceIdentifier));
    }

    [HttpGet("branding/assets/{assetKey}")]
    public async Task<IActionResult> GetBrandingAsset(Guid orgId, string assetKey, [FromServices] IOrganizationBrandingAppService brandingService, CancellationToken ct)
    {
        if (!CanAccessOrganization(orgId)) return Forbid();

        var asset = await brandingService.OpenAssetAsync(orgId, assetKey, ct);
        if (asset == null) return NotFound();

        return File(asset.Value.Stream, asset.Value.ContentType);
    }
}
