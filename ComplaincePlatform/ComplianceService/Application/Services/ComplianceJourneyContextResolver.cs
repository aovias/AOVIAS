using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <inheritdoc />
public sealed class ComplianceJourneyContextResolver : IComplianceJourneyContextResolver
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ComplianceJourneyContextResolver> _logger;

    public ComplianceJourneyContextResolver(
        ComplianceDbContext db,
        ILogger<ComplianceJourneyContextResolver> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResolvedJourneyContext?> ResolveAsync(
        Guid hospitalId,
        Guid frameworkOrJourneyId,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty || frameworkOrJourneyId == Guid.Empty)
            return null;

        // 1. Treat as ComplianceJourney.Id (portal "frameworkId" is often the journey GUID).
        var byJourneyId = await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.Id == frameworkOrJourneyId && j.HospitalId == hospitalId)
            .Select(j => new ResolvedJourneyContext(j.HospitalId, j.FrameworkId, j.Id, j.OrganizationId))
            .FirstOrDefaultAsync(cancellationToken);

        if (byJourneyId != null)
            return byJourneyId;

        // 2. Treat as canonical ComplianceFramework.Id for this hospital.
        var byFrameworkId = await _db.ComplianceJourneys
            .AsNoTracking()
            .Where(j => j.FrameworkId == frameworkOrJourneyId && j.HospitalId == hospitalId)
            .OrderByDescending(j => j.EnrollmentDate)
            .Select(j => new ResolvedJourneyContext(j.HospitalId, j.FrameworkId, j.Id, j.OrganizationId))
            .FirstOrDefaultAsync(cancellationToken);

        if (byFrameworkId != null)
            return byFrameworkId;

        _logger.LogWarning(
            "Could not resolve framework context for HospitalId={HospitalId} FrameworkOrJourneyId={Id}.",
            hospitalId, frameworkOrJourneyId);

        return null;
    }
}
