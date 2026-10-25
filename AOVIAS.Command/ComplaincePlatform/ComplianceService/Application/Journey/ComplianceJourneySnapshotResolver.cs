using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Journey;

/// <summary>
/// Resolves the active <see cref="ComplianceJourney"/> for snapshot read models (hospital + framework).
/// </summary>
public static class ComplianceJourneySnapshotResolver
{
    /// <summary>
    /// Returns the most recently updated journey for the hospital and framework, or null if none exists.
    /// </summary>
    public static async Task<ComplianceJourney?> GetActiveJourneyForSnapshotAsync(
        ComplianceDbContext db,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty || frameworkId == Guid.Empty)
        {
            return null;
        }

        // Ignore global tenant filter: hospital+framework is validated by the API; filter can hide rows when tenant capture mismatches JWT.
        return await db.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(j => j.HospitalId == hospitalId && j.FrameworkId == frameworkId)
            .OrderByDescending(j => j.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Clients often pass <see cref="ComplianceJourney.Id"/> as "frameworkId". If the value matches a journey for this hospital, returns that journey's FrameworkId; otherwise returns the value unchanged.
    /// </summary>
    public static async Task<Guid> ResolveFrameworkOrJourneyIdAsync(
        ComplianceDbContext db,
        Guid hospitalId,
        Guid frameworkOrJourneyId,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty || frameworkOrJourneyId == Guid.Empty)
        {
            return frameworkOrJourneyId;
        }

        var journey = await db.ComplianceJourneys
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                j => j.Id == frameworkOrJourneyId && j.HospitalId == hospitalId,
                cancellationToken);

        return journey != null ? journey.FrameworkId : frameworkOrJourneyId;
    }
}
