using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Infrastructure.Repositories;

public class ComplianceAuditFindingRepository : IComplianceAuditFindingRepository
{
    private readonly ComplianceDbContext _dbContext;

    public ComplianceAuditFindingRepository(ComplianceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Finding>> GetByJourneyIdAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Findings
            .Where(x => x.Scope == FindingScope.Audit && x.ComplianceJourneyId == complianceJourneyId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Finding>> GetUnresolvedByJourneyIdAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Findings
            .Where(x => x.Scope == FindingScope.Audit && x.ComplianceJourneyId == complianceJourneyId && !x.IsResolved)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMajorNcCountAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Findings
            .CountAsync(x => x.ComplianceJourneyId == complianceJourneyId
                && x.Type == NonConformanceType.MajorNC
                && !x.IsResolved, cancellationToken);
    }

    public async Task<int> GetMinorNcCountAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Findings
            .CountAsync(x => x.ComplianceJourneyId == complianceJourneyId
                && x.Type == NonConformanceType.MinorNC
                && !x.IsResolved, cancellationToken);
    }

    public async Task<bool> HasUnresolvedMajorNcAsync(
        Guid complianceJourneyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Findings
            .AnyAsync(x => x.ComplianceJourneyId == complianceJourneyId
                && x.Type == NonConformanceType.MajorNC
                && !x.IsResolved, cancellationToken);
    }

    public async Task<List<Finding>> GetOrganizationFindingsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Findings
            .Where(x => x.Scope == FindingScope.Organization && x.TenantId == organizationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Finding?> GetOrganizationFindingByIdAsync(
        Guid organizationId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Findings
            .FirstOrDefaultAsync(
                x => x.Scope == FindingScope.Organization && x.TenantId == organizationId && x.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(Finding finding, CancellationToken cancellationToken = default)
    {
        await _dbContext.Findings.AddAsync(finding, cancellationToken);
    }

    public async Task UpdateAsync(Finding finding, CancellationToken cancellationToken = default)
    {
        _dbContext.Findings.Update(finding);
        await Task.CompletedTask;
    }
}
