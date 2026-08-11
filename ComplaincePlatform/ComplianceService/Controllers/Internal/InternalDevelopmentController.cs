using ComplianceService.Application.Services;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Scripts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Development;

namespace ComplianceService.Controllers.Internal;

/// <summary>
/// Development-only internal endpoints for QA environment bootstrap.
/// </summary>
[ApiController]
[Route("internal/development")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalDevelopmentController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly SeedSampleSnapshotData _snapshotSeeder;
    private readonly ComplianceJourneyService _journeyService;
    private readonly ComplianceDbContext _dbContext;
    private readonly ILogger<InternalDevelopmentController> _logger;

    public InternalDevelopmentController(
        IWebHostEnvironment environment,
        SeedSampleSnapshotData snapshotSeeder,
        ComplianceJourneyService journeyService,
        ComplianceDbContext dbContext,
        ILogger<InternalDevelopmentController> logger)
    {
        _environment = environment;
        _snapshotSeeder = snapshotSeeder;
        _journeyService = journeyService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("qa-snapshot/{hospitalId:guid}")]
    public async Task<IActionResult> SeedQaSnapshot(
        Guid hospitalId,
        [FromQuery] Guid? organizationId,
        CancellationToken cancellationToken)
    {
        if (!NonProductionEnvironment.AllowDevelopmentSeeding(_environment))
            return NotFound();

        if (hospitalId == Guid.Empty)
            return BadRequest(new { error = "hospitalId is required." });

        await _snapshotSeeder.SeedAsync(hospitalId, cancellationToken);

        Guid? journeyId = null;
        if (organizationId.HasValue && organizationId.Value != Guid.Empty)
        {
            var framework = await _dbContext.ComplianceFrameworks
                .AsNoTracking()
                .Where(f => f.PlatformId != null && f.PlatformId != "")
                .OrderBy(f => f.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (framework != null)
            {
                var journey = await _journeyService.CreateJourneyAsync(
                    tenantId: organizationId.Value,
                    organizationId: organizationId.Value,
                    hospitalId: hospitalId,
                    frameworkId: framework.Id,
                    creatingUserId: Guid.Empty,
                    cancellationToken: cancellationToken);
                journeyId = journey.Id;
            }
            else
            {
                _logger.LogWarning(
                    "QA snapshot seeded for hospital {HospitalId} but no compliance framework was found for journey bootstrap.",
                    hospitalId);
            }
        }

        _logger.LogInformation(
            "QA compliance snapshot seeded for hospital {HospitalId} (journeyId={JourneyId})",
            hospitalId,
            journeyId);

        return Ok(new { hospitalId, seeded = true, journeyId });
    }
}
