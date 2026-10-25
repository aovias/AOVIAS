using ComplianceService.Application.Dashboard.Projection;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ComplianceService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/dashboard-projection")]
[Authorize]
public sealed class DashboardProjectionDiagnosticsController : ControllerBase
{
    private readonly ComplianceDbContext _dbContext;
    private readonly DashboardProjectionRebuildService _rebuildService;
    private readonly DashboardProjectionOptions _options;
    private readonly IWebHostEnvironment _environment;

    public DashboardProjectionDiagnosticsController(
        ComplianceDbContext dbContext,
        DashboardProjectionRebuildService rebuildService,
        IOptions<DashboardProjectionOptions> options,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _rebuildService = rebuildService;
        _options = options.Value;
        _environment = environment;
    }

    [HttpGet("diagnostics")]
    public async Task<IActionResult> GetDiagnostics(CancellationToken ct)
    {
        if (!CanExposeDiagnostics())
        {
            return NotFound();
        }

        var backlog = await _dbContext.OutboxMessages
            .AsNoTracking()
            .CountAsync(m => m.ProjectionProcessedOn == null, ct);

        var corrupted = await _dbContext.DashboardProjections
            .AsNoTracking()
            .CountAsync(p => p.IsCorrupted, ct);

        var complete = await _dbContext.DashboardProjections
            .AsNoTracking()
            .CountAsync(p => p.IsComplete && !p.IsCorrupted, ct);

        var latest = await _dbContext.DashboardProjections
            .AsNoTracking()
            .OrderByDescending(p => p.LastUpdatedAt)
            .Select(p => new
            {
                p.OrganizationId,
                p.HospitalId,
                p.FrameworkId,
                p.DataVersion,
                p.LastUpdatedAt,
                p.LastEventType,
                p.IsComplete,
                p.IsCorrupted,
            })
            .Take(10)
            .ToListAsync(ct);

        return Ok(new
        {
            Enabled = _options.Enabled,
            OutboxBacklog = backlog,
            CompleteProjections = complete,
            CorruptedProjections = corrupted,
            LatestProjections = latest,
        });
    }

    [HttpPost("rebuild")]
    public async Task<IActionResult> Rebuild(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid? frameworkId,
        CancellationToken ct)
    {
        if (!CanExposeDiagnostics())
        {
            return NotFound();
        }

        await _rebuildService.RebuildHospitalAsync(organizationId, hospitalId, frameworkId, ct);
        return Accepted(new { organizationId, hospitalId, frameworkId });
    }

    private bool CanExposeDiagnostics() => _environment.IsDevelopment() || _options.ExposeDiagnosticsHeaders;
}
