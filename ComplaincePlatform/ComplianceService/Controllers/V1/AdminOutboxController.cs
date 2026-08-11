using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1;

/// <summary>
/// Phase 7.5 — human-gated poison outbox inspection and replay (SUPER_ADMIN / ORG_ADMIN).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/outbox")]
[Authorize(Roles = ApplicationRoles.Authorization.SuperAdminOrOrgAdmin)]
public sealed class AdminOutboxController : ControllerBase
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<AdminOutboxController> _logger;

    public AdminOutboxController(ComplianceDbContext db, ILogger<AdminOutboxController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Lists outbox messages that have exhausted retries (RetryCount &gt;= MaxRetries).</summary>
    [HttpGet("poison")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PoisonOutboxMessageDto>>>> ListPoison(
        CancellationToken cancellationToken)
    {
        var poison = await _db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.RetryCount >= OutboxConstants.MaxRetries)
            .OrderByDescending(m => m.OccurredOn)
            .Take(200)
            .Select(m => new PoisonOutboxMessageDto(
                m.Id,
                m.AggregateId,
                m.Type,
                m.OccurredOn,
                m.RetryCount,
                m.Error,
                m.ProjectionProcessedOn,
                m.RelayProcessedOn))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, poison, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Resets RetryCount (and Error) so processors will pick the message up again.
    /// Does not clear ProjectionProcessedOn / RelayProcessedOn — only replays still-pending failures.
    /// </summary>
    [HttpPost("{id:guid}/replay")]
    public async Task<ActionResult<ApiResponse<object>>> Replay(
        Guid id,
        CancellationToken cancellationToken)
    {
        var message = await _db.OutboxMessages.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (message is null)
        {
            return NotFound(ApiResponseFactory.Failure<object>(
                ApiResponseCode.RESOURCE_NOT_FOUND,
                HttpContext.TraceIdentifier));
        }

        if (message.RetryCount < OutboxConstants.MaxRetries)
        {
            return BadRequest(ApiResponseFactory.Failure<object>(
                ApiResponseCode.VALIDATION_FAILED,
                HttpContext.TraceIdentifier));
        }

        // Reset retry state so processors/relay pick the message up again (pending flags unchanged).
        message.RetryCount = 0;
        message.Error = null;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Outbox message {MessageId} replayed by {User} (type {Type})",
            message.Id,
            User.Identity?.Name ?? "unknown",
            message.Type);

        return Ok(ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            new { message.Id, message.RetryCount, OutboxConstants.MaxRetries },
            HttpContext.TraceIdentifier));
    }
}

public sealed record PoisonOutboxMessageDto(
    Guid Id,
    Guid AggregateId,
    string Type,
    DateTime OccurredOn,
    int RetryCount,
    string? Error,
    DateTime? ProjectionProcessedOn,
    DateTime? RelayProcessedOn);
