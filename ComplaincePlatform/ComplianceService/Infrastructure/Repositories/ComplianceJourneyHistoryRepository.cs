using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Infrastructure.Repositories;

public class ComplianceJourneyHistoryRepository : IComplianceJourneyHistoryRepository
{
    private readonly ComplianceDbContext _dbContext;

    public ComplianceJourneyHistoryRepository(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ComplianceJourneyHistory>> GetByJourneyIdAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ComplianceJourneyHistories
            .Where(x => x.ComplianceJourneyId == complianceJourneyId)
            .OrderBy(x => x.ChangedAt) // Ascending chronological order for audit
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ComplianceJourneyHistory history, CancellationToken cancellationToken = default)
    {
        await _dbContext.ComplianceJourneyHistories.AddAsync(history, cancellationToken);
    }

    public async Task<(List<ComplianceJourneyHistory> Items, int TotalCount)> GetTransitionsByHospitalAndFrameworkAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        ComplianceLifecycleState? fromState = null,
        ComplianceLifecycleState? toState = null,
        Guid? changedBy = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Build query with joins to ensure tenant isolation
        var query = from history in _dbContext.ComplianceJourneyHistories
                    join journey in _dbContext.ComplianceJourneys
                        on history.ComplianceJourneyId equals journey.Id
                    where journey.HospitalId == hospitalId
                       && journey.FrameworkId == frameworkId
                       && journey.TenantId == tenantId
                    select history;

        // Apply optional filters
        if (fromDate.HasValue)
        {
            query = query.Where(h => h.ChangedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.ChangedAt <= toDate.Value);
        }

        if (fromState.HasValue)
        {
            query = query.Where(h => h.OldStatus == fromState.Value);
        }

        if (toState.HasValue)
        {
            query = query.Where(h => h.NewStatus == toState.Value);
        }

        if (changedBy.HasValue)
        {
            query = query.Where(h => h.ChangedBy == changedBy.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var items = await query
            .OrderBy(h => h.ChangedAt) // Ascending chronological order
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
