using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for OrganizationComplianceObligation.
/// All write methods use IgnoreQueryFilters to operate across tenants safely
/// when called from background or system contexts.
/// </summary>
public class ObligationRepository : IObligationRepository
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ObligationRepository> _logger;

    public ObligationRepository(
        ComplianceDbContext db,
        ILogger<ObligationRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OrganizationComplianceObligation>> GetByOrganizationAndFrameworkAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        return await _db.OrganizationComplianceObligations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organizationId && o.FrameworkId == frameworkId)
            .OrderBy(o => o.MaturityLevel)
            .ThenBy(o => o.Code)
            .ToListAsync(ct);
    }

    public async Task<OrganizationComplianceObligation?> GetByKeyAsync(
        Guid organizationId,
        Guid objectiveElementId,
        CancellationToken ct = default)
    {
        return await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.OrganizationId == organizationId
                  && o.ObjectiveElementId == objectiveElementId,
                ct);
    }

    public async Task UpsertBatchAsync(
        IReadOnlyList<OrganizationComplianceObligation> obligations,
        CancellationToken ct = default)
    {
        if (obligations.Count == 0)
        {
            return;
        }

        // Guard: do not insert obligations whose (OrganizationId, ObjectiveElementId) already exist.
        // This handles races and repeated calls idempotently at the application layer.
        // The unique DB constraint is the final safety net.
        var oeIds = obligations.Select(o => o.ObjectiveElementId).ToList();
        var orgId = obligations[0].OrganizationId;

        var existingOeIds = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == orgId && oeIds.Contains(o.ObjectiveElementId))
            .Select(o => o.ObjectiveElementId)
            .ToHashSetAsync(ct);

        var toInsert = obligations
            .Where(o => !existingOeIds.Contains(o.ObjectiveElementId))
            .ToList();

        if (toInsert.Count == 0)
        {
            _logger.LogDebug(
                "ObligationRepository: All {Count} obligations already exist. Nothing to insert.",
                obligations.Count);
            return;
        }

        await _db.OrganizationComplianceObligations.AddRangeAsync(toInsert, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ObligationRepository: Inserted {Count} new obligations for OrganizationId={OrgId}.",
            toInsert.Count, orgId);
    }

    public async Task<IReadOnlyList<ObligationStatusCount>> GetSummaryCountsAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var counts = await _db.OrganizationComplianceObligations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organizationId && o.FrameworkId == frameworkId)
            .GroupBy(o => new
            {
                MaturityLevel = (int)o.MaturityLevel,
                ImplementationStatus = (int)o.ImplementationStatus,
                AssessmentStatus = (int)o.AssessmentStatus,
                VerificationStatus = (int)o.VerificationStatus
            })
            .Select(g => new ObligationStatusCount
            {
                MaturityLevel = g.Key.MaturityLevel,
                ImplementationStatus = g.Key.ImplementationStatus,
                AssessmentStatus = g.Key.AssessmentStatus,
                VerificationStatus = g.Key.VerificationStatus,
                Count = g.Count()
            })
            .ToListAsync(ct);

        return counts;
    }
}
