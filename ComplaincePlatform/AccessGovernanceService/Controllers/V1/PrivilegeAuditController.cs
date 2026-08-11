using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;

namespace AccessGovernanceService.Controllers.V1;

/// <summary>Phase 7.1 — privilege audit export for access reviews.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/privilege-audit")]
[Authorize]
public sealed class PrivilegeAuditController : ControllerBase
{
    private readonly AccessGovernanceDbContext _db;

    public PrivilegeAuditController(AccessGovernanceDbContext db) => _db = db;

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int take = 500,
        CancellationToken cancellationToken = default)
    {
        var traceId = HttpContext.TraceIdentifier;
        take = Math.Clamp(take, 1, 2000);
        var from = fromUtc ?? DateTime.UtcNow.AddDays(-90);
        var to = toUtc ?? DateTime.UtcNow;

        var rows = await _db.AccessAuditLogs.AsNoTracking()
            .Where(x => x.OccurredAt >= from && x.OccurredAt <= to)
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
            .Select(x => new
            {
                x.Id,
                x.EntityType,
                x.EntityId,
                x.Action,
                x.ActorUserId,
                x.OccurredAt
            })
            .ToListAsync(cancellationToken);

        var response = ApiResponseFactory.Success(
            ApiResponseCode.SUCCESS,
            new { from, to, count = rows.Count, items = rows },
            traceId);
        return Ok(response);
    }
}
