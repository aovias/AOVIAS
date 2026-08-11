using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(
        IAuditLogRepository repository,
        ILogger<AuditLogsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AuditLog>>>> GetAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        try
        {
            var query = _repository.Query();

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(l => l.EntityType == entityType);
            }

            if (entityId.HasValue && entityId.Value != Guid.Empty)
            {
                query = query.Where(l => l.EntityId == entityId.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(l => l.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(l => l.Timestamp <= to.Value);
            }

            var results = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(500)
                .ToListAsync(ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                results,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");

            var result = ApiResponseFactory.Failure<List<AuditLog>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }

    [HttpGet("{entityType}/{entityId:guid}")]
    public async Task<ActionResult<ApiResponse<List<AuditLog>>>> GetAuditLogsForEntity(
        string entityType,
        Guid entityId,
        CancellationToken ct = default)
    {
        var traceId = HttpContext.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(entityType) || entityId == Guid.Empty)
        {
            var result = ApiResponseFactory.Failure<List<AuditLog>>(
                ApiResponseCode.VALIDATION_FAILED,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
            return StatusCode(meta.HttpStatus, result);
        }

        try
        {
            var logs = await _repository.GetByEntityAsync(entityType, entityId, ct);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                logs,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving audit logs for entity {EntityType} {EntityId}",
                entityType, entityId);

            var result = ApiResponseFactory.Failure<List<AuditLog>>(
                ApiResponseCode.INTERNAL_ERROR,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.INTERNAL_ERROR];
            return StatusCode(meta.HttpStatus, result);
        }
    }
}

