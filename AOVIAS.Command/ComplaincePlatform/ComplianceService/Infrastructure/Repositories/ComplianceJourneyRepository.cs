using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Infrastructure.Repositories;

public class ComplianceJourneyRepository : IComplianceJourneyRepository
{
    private readonly ComplianceDbContext _dbContext;

    public ComplianceJourneyRepository(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ComplianceJourney?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Match GetByHospitalAndFrameworkAsync: tenant filter can hide a row by Id; callers enforce TenantId.
        return await _dbContext.ComplianceJourneys
            .IgnoreQueryFilters()
            .Include(x => x.Framework)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ComplianceJourney?> GetByHospitalAndFrameworkAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken = default)
    {
        // Hospital+framework is globally unique; the global tenant filter can hide an existing row
        // (e.g. filter capture mismatch) and cause duplicate insert / 500 on save.
        return await _dbContext.ComplianceJourneys
            .IgnoreQueryFilters()
            .Include(x => x.Framework)
            .FirstOrDefaultAsync(
                x => x.HospitalId == hospitalId && x.FrameworkId == frameworkId,
                cancellationToken);
    }

    public async Task<List<ComplianceJourney>> GetByHospitalIdAsync(
        Guid hospitalId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComplianceJourneys
            .Include(x => x.Framework)
            .Where(x => x.HospitalId == hospitalId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ComplianceJourney>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComplianceJourneys
            .Include(x => x.Framework)
            .Where(x => x.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ComplianceJourney>> GetExpiringJourneysAsync(
        DateTime expiryDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComplianceJourneys
            .Include(x => x.Framework)
            .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate.Value <= expiryDate
                && x.Status == ComplianceLifecycleState.Approved)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ComplianceJourney>> GetJourneysInRenewalWindowAsync(
        DateTime renewalWindowStart,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComplianceJourneys
            .Include(x => x.Framework)
            .Where(x => x.RenewalWindowStartDate.HasValue
                && x.RenewalWindowStartDate.Value <= renewalWindowStart
                && x.Status == ComplianceLifecycleState.Approved)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ComplianceJourney>> GetByStatusAsync(
        ComplianceLifecycleState status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComplianceJourneys
            .Include(x => x.Framework)
            .Where(x => x.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ComplianceJourney>> GetJourneysByHospitalIdsAsync(
        List<Guid> hospitalIds,
        CancellationToken cancellationToken = default)
    {
        if (hospitalIds == null || hospitalIds.Count == 0)
            return new List<ComplianceJourney>();

        return await _dbContext.ComplianceJourneys
            .Include(j => j.Framework)
            .Where(j => hospitalIds.Contains(j.HospitalId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ComplianceJourney journey, CancellationToken cancellationToken = default)
    {
        await _dbContext.ComplianceJourneys.AddAsync(journey, cancellationToken);
    }

    public async Task UpdateAsync(ComplianceJourney journey, CancellationToken cancellationToken = default)
    {
        _dbContext.ComplianceJourneys.Update(journey);
        await Task.CompletedTask;
    }
}
