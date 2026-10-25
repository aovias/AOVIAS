using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Services;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Development;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;
using System.Net;

namespace OrganizationService.Controllers.Internal;

[ApiController]
[Route("internal")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalOrganizationController : ControllerBase
{
    private readonly OrganizationAppService _orgService;
    private readonly HospitalAppService _hospitalService;
    private readonly OrganizationMemberAppService _memberService;
    private readonly IDepartmentHierarchyAppService _departmentService;
    private readonly OrganizationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<InternalOrganizationController> _logger;

    public InternalOrganizationController(
        OrganizationAppService orgService,
        HospitalAppService hospitalService,
        OrganizationMemberAppService memberService,
        IDepartmentHierarchyAppService departmentService,
        OrganizationDbContext dbContext,
        IWebHostEnvironment environment,
        ILogger<InternalOrganizationController> logger)
    {
        _orgService = orgService;
        _hospitalService = hospitalService;
        _memberService = memberService;
        _departmentService = departmentService;
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var org = await _orgService.CreateInternalAsync(dto);
            _logger.LogInformation("Organization created via internal endpoint: {OrganizationId}, {Name}", org.Id, org.Name);
            
            var data = new { id = org.Id, name = org.Name, platformId = org.PlatformId };

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)data,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create organization: {Error}", ex.Message);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization");

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    /// <summary>Idempotent org defaults seed for SaaS signup (CRP Phase 2).</summary>
    [HttpPost("organizations/{id:guid}/ensure-defaults")]
    public async Task<IActionResult> EnsureOrganizationDefaults(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _orgService.EnsureDefaultsAsync(id, cancellationToken);
            _logger.LogInformation(
                "EnsureDefaults for org {OrganizationId}: settingsCreated={SettingsCreated}",
                id,
                result.SettingsCreated);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)new
                {
                    organizationId = result.OrganizationId,
                    settingsCreated = result.SettingsCreated,
                    alreadyHadSettings = result.AlreadyHadSettings
                },
                traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "EnsureDefaults failed for org {OrganizationId}", id);
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnsureDefaults error for org {OrganizationId}", id);
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpDelete("organizations/{id}")]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var deleted = await _orgService.DeleteAsync(id);
            if (!deleted)
            {
                var failure = ApiResponseFactory.Failure<object>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta.HttpStatus, failure);
            }

            _logger.LogInformation("Organization deleted via internal endpoint: {OrganizationId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting organization {OrganizationId}", id);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpPost("hospitals")]
    public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var hospital = await _hospitalService.CreateAsync(dto);
            _logger.LogInformation("Hospital created via internal endpoint: {HospitalId}, {Name}", hospital.Id, hospital.Name);
            
            var data = new
            {
                id = hospital.Id,
                name = hospital.Name,
                code = hospital.Code,
                type = hospital.Type,
                bedCapacity = hospital.BedCapacity,
                countryCode = hospital.CountryCode,
                stateCode = hospital.StateCode,
                isOnboarded = hospital.IsOnboarded
            };

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)data,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create hospital: {Error}", ex.Message);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hospital");

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpDelete("hospitals/{id}")]
    public async Task<IActionResult> DeleteHospital(Guid id)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var deleted = await _hospitalService.DeleteAsync(id);
            if (!deleted)
            {
                var failure = ApiResponseFactory.Failure<object>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta.HttpStatus, failure);
            }

            _logger.LogInformation("Hospital deleted via internal endpoint: {HospitalId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hospital {HospitalId}", id);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("hospitals/{id}")]
    public async Task<IActionResult> GetHospital(Guid id)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var hospital = await _hospitalService.GetAsync(id);
            if (hospital == null)
            {
                var failure = ApiResponseFactory.Failure<object>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var metaNotFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(metaNotFound.HttpStatus, failure);
            }

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                hospital,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospital {HospitalId}", id);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("master-departments/{id:guid}")]
    public async Task<IActionResult> GetMasterDepartment(Guid id, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var dept = await _dbContext.MasterDepartments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dept == null)
        {
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { dept.Id, dept.Name }, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("development/master-data-status")]
    public async Task<IActionResult> GetDevelopmentMasterDataStatus(CancellationToken cancellationToken)
    {
        if (!NonProductionEnvironment.AllowDevelopmentSeeding(_environment))
            return NotFound();

        var traceId = HttpContext.TraceIdentifier;
        var masterDepartments = await _dbContext.MasterDepartments.CountAsync(d => d.IsActive, cancellationToken);
        var lookupCategories = await _dbContext.LookupCategories.CountAsync(cancellationToken);

        var data = new
        {
            masterDepartments,
            lookupCategories,
            ready = masterDepartments >= 10 && lookupCategories >= 3,
        };

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)data, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpGet("organizations/{organizationId}/hospitals")]
    public async Task<IActionResult> ListHospitalsByOrganization(Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var hospitals = await _hospitalService.ListByOrganizationAsync(organizationId, cancellationToken);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                hospitals,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing hospitals for organization {OrganizationId}", organizationId);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("organizations/{organizationId}/business-calendar")]
    public async Task<IActionResult> GetBusinessCalendar(
        Guid organizationId,
        [FromQuery] int? year,
        [FromServices] IOrganizationSettingsAppService settingsService,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var calendar = await settingsService.GetBusinessCalendarAsync(organizationId, year, cancellationToken);
        if (calendar == null)
        {
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }

        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, calendar, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }

    [HttpPut("hospitals/{id}")]
    public async Task<IActionResult> UpdateHospital(Guid id, [FromBody] UpdateHospitalDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var updated = await _hospitalService.UpdateAsync(id, dto);
            if (updated == null)
            {
                var failure = ApiResponseFactory.Failure<object>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var metaNotFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(metaNotFound.HttpStatus, failure);
            }

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                updated,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update hospital {HospitalId}: {Error}", id, ex.Message);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital {HospitalId}", id);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpPut("hospitals/{id}/wizard-profile")]
    public async Task<IActionResult> ApplyHospitalWizardProfile(Guid id, [FromBody] HospitalWizardProfileDto dto, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var updated = await _hospitalService.ApplyWizardProfileAsync(id, dto, cancellationToken);
            if (updated == null)
            {
                var failure = ApiResponseFactory.Failure<object>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var metaNotFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(metaNotFound.HttpStatus, failure);
            }

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                updated,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Wizard profile validation failed for hospital {HospitalId}", id);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying wizard profile for hospital {HospitalId}", id);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpPost("organization-members")]
    public async Task<IActionResult> CreateOrganizationMember([FromBody] CreateOrganizationMemberDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var member = await _memberService.CreateAsync(dto);
            _logger.LogInformation("Organization member created via internal endpoint: {MemberId}, User: {UserId}, Org: {OrganizationId}", 
                member.Id, member.UserId, member.OrganizationId);
            
            var data = new
            {
                id = member.Id,
                userId = member.UserId,
                organizationId = member.OrganizationId,
                departmentId = member.DepartmentId,
                role = member.Role,
                firstName = member.FirstName,
                lastName = member.LastName,
                email = member.Email,
                phoneNumber = member.PhoneNumber,
                alternatePhoneNumber = member.AlternatePhoneNumber,
                profilePicUrl = member.ProfilePicUrl,
                preferences = member.Preferences,
                createdAt = member.CreatedAt
            };

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)data,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create organization member: {Error}", ex.Message);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization member");

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpPut("organization-members/{memberId}")]
    public async Task<IActionResult> UpdateOrganizationMember(Guid memberId, [FromBody] UpdateOrganizationMemberDto dto, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var member = await _memberService.UpdateAsync(memberId, dto, cancellationToken);
            if (member == null)
            {
                var notFound = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var notFoundMeta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(notFoundMeta.HttpStatus, notFound);
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, member, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to update organization member {MemberId}: {Error}", memberId, ex.Message);
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, failure);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization member {MemberId}", memberId);
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("organization-members/{memberId}")]
    public async Task<IActionResult> GetOrganizationMember(Guid memberId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var member = await _memberService.GetAsync(memberId, cancellationToken);
            if (member == null)
            {
                var notFound = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var notFoundMeta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(notFoundMeta.HttpStatus, notFound);
            }

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, member, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization member {MemberId}", memberId);
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("organizations/{organizationId}/organization-members")]
    public async Task<IActionResult> ListOrganizationMembers(Guid organizationId, CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var members = await _memberService.ListByOrganizationAsync(organizationId, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, members, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing organization members for organization {OrganizationId}", organizationId);
            var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.INTERNAL_ERROR, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpGet("users/{userId}/auth-context")]
    public async Task<IActionResult> GetUserAuthContext(
        Guid userId,
        [FromQuery] Guid? organizationId = null,
        [FromQuery] Guid? hospitalId = null)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var now = DateTime.UtcNow;
            var q = _dbContext.Members.Where(m => m.UserId == userId && !m.IsDeleted
                && m.MembershipStatus == Domain.Enums.MembershipStatuses.Active
                && m.EffectiveFrom <= now && (m.EffectiveTo == null || m.EffectiveTo > now));
            if (organizationId.HasValue) q = q.Where(m => m.OrganizationId == organizationId);

            OrganizationMember? member = null;
            Guid? resolvedHospitalId = null;

            if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
            {
                var hospital = await _dbContext.Hospitals
                    .AsNoTracking()
                    .FirstOrDefaultAsync(h => h.Id == hospitalId.Value && !h.IsDeleted);

                if (hospital != null)
                {
                    // Prefer an exact hospital membership, else an org-wide membership in that hospital's org.
                    member = await q
                        .Where(m => m.OrganizationId == hospital.OrganizationId
                            && (m.HospitalId == null || m.HospitalId == hospital.Id))
                        .OrderByDescending(m => m.HospitalId == hospital.Id)
                        .ThenByDescending(m => m.IsPrimary)
                        .ThenBy(m => m.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (member != null)
                        resolvedHospitalId = hospital.Id;
                }
            }

            member ??= await q.OrderByDescending(m => m.IsPrimary).ThenBy(m => m.CreatedAt).FirstOrDefaultAsync();

            if (member == null)
            {
                var empty = new
                {
                    organizationId = Guid.Empty,
                    hospitalId = Guid.Empty,
                    membershipId = Guid.Empty,
                    departmentId = (Guid?)null,
                    role = string.Empty
                };
                return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                    ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)empty, traceId));
            }

            if (!resolvedHospitalId.HasValue)
            {
                resolvedHospitalId = member.HospitalId;
                if (!resolvedHospitalId.HasValue)
                {
                    var hospital = await _dbContext.Hospitals
                        .Where(h => h.OrganizationId == member.OrganizationId && !h.IsDeleted)
                        .OrderBy(h => h.CreatedAt)
                        .FirstOrDefaultAsync();
                    resolvedHospitalId = hospital?.Id;
                }
            }

            var data = new
            {
                organizationId = member.OrganizationId,
                hospitalId = resolvedHospitalId ?? Guid.Empty,
                membershipId = member.Id,
                departmentId = member.DepartmentId,
                role = member.Role
            };

            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus,
                ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)data, traceId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving auth context for user {UserId}", userId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR].HttpStatus,
                ApiResponseFactory.Failure<object>(ApiResponseCode.INTERNAL_ERROR, traceId));
        }
    }

    [HttpGet("users/{userId}/organization-hospital")]
    public async Task<IActionResult> GetUserOrganizationAndHospital(Guid userId)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            // Get user's first organization membership
            var member = await _dbContext.Members
                .Where(m => m.UserId == userId)
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (member == null)
            {
                _logger.LogInformation("No organization membership found for user {UserId}", userId);

                var emptyData = new { organizationId = Guid.Empty, hospitalId = Guid.Empty };

                var emptyResponse = ApiResponseFactory.Success(
                    ApiResponseCode.SUCCESS,
                    (object)emptyData,
                    traceId);

                var emptyMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
                return StatusCode(emptyMeta.HttpStatus, emptyResponse);
            }

            var organizationId = member.OrganizationId;

            // Get first hospital for the organization
            var hospital = await _dbContext.Hospitals
                .Where(h => h.OrganizationId == organizationId)
                .OrderBy(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            var hospitalId = hospital?.Id ?? Guid.Empty;

            _logger.LogInformation("Retrieved organization and hospital for user {UserId}: OrganizationId={OrganizationId}, HospitalId={HospitalId}",
                userId, organizationId, hospitalId);

            var data = new { organizationId, hospitalId };

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)data,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization and hospital for user {UserId}", userId);

            var failure = ApiResponseFactory.Failure<object>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, failure);
        }
    }

    [HttpPut("departments/upsert")]
    public async Task<ActionResult<ApiResponse<IntegrationUpsertResponse>>> UpsertDepartment(
        [FromBody] IntegrationUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        try
        {
            var result = await _departmentService.UpsertFromIntegrationAsync(request, cancellationToken);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
        }
        catch (InvalidOperationException ex)
        {
            var failure = ApiResponseFactory.Failure<IntegrationUpsertResponse>(ApiResponseCode.VALIDATION_FAILED, traceId);
            failure.Error!.Message = ex.Message;
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }
    }

    [HttpPost("departments/{id:guid}/integration-rollback")]
    public async Task<ActionResult<ApiResponse<bool>>> RollbackDepartment(
        Guid id,
        [FromBody] IntegrationRollbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        var ok = await _departmentService.RollbackIntegrationAsync(request.OrganizationId, id, cancellationToken);
        if (!ok)
        {
            var failure = ApiResponseFactory.Failure<bool>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND].HttpStatus, failure);
        }
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, true, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, response);
    }
}
