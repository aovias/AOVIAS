using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Configs;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain.Enums;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Controller for managing audit findings for a compliance journey.
/// Provides simple CRUD-style endpoints that sit on top of the existing
/// ComplianceAuditFinding repository and journey lifecycle engine.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/journeys")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class AuditFindingsController : ControllerBase
{
    private readonly IComplianceAuditFindingRepository _auditFindingRepository;
    private readonly IComplianceJourneyRepository _journeyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _userContext;
    private readonly IFindingAdapter _adapter;
    private readonly IFindingAuditTrailService _auditTrailService;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IShadowComparisonService _shadowComparisonService;
    private readonly MigrationSafetyOptions _migrationSafetyOptions;
    private readonly ComplianceDbContext _dbContext;
    private readonly ILogger<AuditFindingsController> _logger;

    public AuditFindingsController(
        IComplianceAuditFindingRepository auditFindingRepository,
        IComplianceJourneyRepository journeyRepository,
        IUnitOfWork unitOfWork,
        IUserContextService userContext,
        IFindingAdapter adapter,
        IFindingAuditTrailService auditTrailService,
        IComplianceEventEmitter eventEmitter,
        IShadowComparisonService shadowComparisonService,
        IOptions<MigrationSafetyOptions> migrationSafetyOptions,
        ComplianceDbContext dbContext,
        ILogger<AuditFindingsController> logger)
    {
        _auditFindingRepository = auditFindingRepository;
        _journeyRepository = journeyRepository;
        _unitOfWork = unitOfWork;
        _userContext = userContext;
        _adapter = adapter;
        _auditTrailService = auditTrailService;
        _eventEmitter = eventEmitter;
        _shadowComparisonService = shadowComparisonService;
        _migrationSafetyOptions = migrationSafetyOptions.Value;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Lists all audit findings for a given compliance journey.
    /// </summary>
    [HttpGet("{journeyId:guid}/audit-findings")]
    public async Task<ActionResult<ApiResponse<List<AuditFindingDto>>>> GetAuditFindings(
        Guid journeyId,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<AuditFindingDto>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<AuditFindingDto>>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var journey = await _journeyRepository.GetByIdAsync(journeyId, ct);
            if (journey == null || journey.TenantId != tenantId)
            {
                var result = ApiResponseFactory.Failure<List<AuditFindingDto>>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta.HttpStatus, result);
            }

            var findings = await _auditFindingRepository.GetByJourneyIdAsync(journeyId, ct);

            var dtos = findings
                .Select(_adapter.ToAuditDto)
                .ToList();

            if (_migrationSafetyOptions.EnableShadowMode)
            {
                var legacyCount = await _dbContext.ComplianceAuditFindings
                    .AsNoTracking()
                    .CountAsync(f => f.ComplianceJourneyId == journeyId, ct);
                await _shadowComparisonService.CompareAndLogAsync(
                    nameof(Finding),
                    journeyId,
                    "AuditFindingsList",
                    new { Count = legacyCount },
                    new { Count = dtos.Count },
                    _userContext.UserId,
                    ct);
            }

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                dtos,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while retrieving audit findings for journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<List<AuditFindingDto>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    /// <summary>
    /// Creates a new audit finding for a given compliance journey.
    /// </summary>
    [HttpPost("{journeyId:guid}/audit-findings")]
    public async Task<ActionResult<ApiResponse<AuditFindingDto>>> CreateAuditFinding(
        Guid journeyId,
        [FromBody] CreateAuditFindingRequest request,
        CancellationToken ct)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (journeyId == Guid.Empty || request == null || request.ObjectiveElementId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<AuditFindingDto>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        var tenantId = _userContext.OrganizationId;
        if (tenantId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<AuditFindingDto>(
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.ORGANIZATION_CONTEXT_MISSING];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var journey = await _journeyRepository.GetByIdAsync(journeyId, ct);
            if (journey == null || journey.TenantId != tenantId)
            {
                var result = ApiResponseFactory.Failure<AuditFindingDto>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta.HttpStatus, result);
            }

            // Basic rule: allow findings only when journey is in AuditScheduled or AuditCompleted
            if (journey.Status is not ComplianceLifecycleState.AuditScheduled
                and not ComplianceLifecycleState.AuditCompleted)
            {
                var result = ApiResponseFactory.Failure<AuditFindingDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
                return StatusCode(meta.HttpStatus, result);
            }

            if (!Enum.IsDefined(typeof(NonConformanceType), request.Type))
            {
                var result = ApiResponseFactory.Failure<AuditFindingDto>(
                    ApiResponseCode.VALIDATION_FAILED,
                    traceId);

                var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
                return StatusCode(meta.HttpStatus, result);
            }

            var dueDate = request.DueDate ?? DateTime.UtcNow.AddDays(30);

            var finding = new ComplianceAuditFinding(
                complianceJourneyId: journeyId,
                objectiveElementId: request.ObjectiveElementId,
                type: request.Type,
                description: request.Description ?? string.Empty,
                dueDate: dueDate);

            finding.Scope = FindingScope.Audit;
            finding.TenantId = tenantId;
            finding.Status = FindingStatus.Open;
            finding.Severity = request.Type == NonConformanceType.MajorNC
                ? FindingSeverity.Major
                : request.Type == NonConformanceType.MinorNC
                    ? FindingSeverity.Minor
                    : FindingSeverity.Minor;
            finding.ComplianceStatus = request.Type == NonConformanceType.Observation
                ? ComplianceEvaluationStatus.PartiallyCompliant
                : ComplianceEvaluationStatus.NonCompliant;
            finding.IsMandatory = false;
            finding.CreatedByUserId = _userContext.UserId;
            finding.LastUpdatedBy = _userContext.UserId;

            await _auditFindingRepository.AddAsync(finding, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            await _eventEmitter.EmitAsync(
                ComplianceEventType.FindingCreated,
                finding.Id,
                nameof(Finding),
                tenantId,
                _userContext.UserId,
                new
                {
                    finding.Scope,
                    finding.Type,
                    finding.ComplianceStatus,
                    finding.ObjectiveElementId
                },
                ct);
            await _auditTrailService.LogAsync(
                finding.Id,
                "FindingCreated",
                _userContext.UserId,
                "User",
                null,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    finding.Type,
                    finding.Status,
                    finding.ComplianceStatus,
                    finding.ObjectiveElementId
                }),
                ct);

            var dto = _adapter.ToAuditDto(finding);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                dto,
                traceId);

            var successMeta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(successMeta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while creating audit finding for journey {JourneyId}", journeyId);

            var result = ApiResponseFactory.Failure<AuditFindingDto>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }
}
