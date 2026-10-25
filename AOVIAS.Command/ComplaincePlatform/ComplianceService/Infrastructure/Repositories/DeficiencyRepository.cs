using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Infrastructure.Repositories;

/// <summary>
/// EF Core repository for ComplianceDeficiency aggregate.
/// Internal queries use IgnoreQueryFilters to operate across tenants safely
/// when called from the evaluation engine background context.
/// </summary>
public class DeficiencyRepository : IDeficiencyRepository
{
    private readonly ComplianceDbContext _db;

    public DeficiencyRepository(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ComplianceDeficiency deficiency, CancellationToken ct = default)
    {
        await _db.ComplianceDeficiencies.AddAsync(deficiency, ct);
    }

    public async Task<ComplianceDeficiency?> FindOpenAsync(
        Guid obligationId,
        DeficiencyType type,
        CancellationToken ct = default)
    {
        return await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                d => d.ComplianceObligationId == obligationId
                  && d.DeficiencyType == type
                  && d.Status != DeficiencyStatus.Closed,
                ct);
    }

    public async Task<IReadOnlyList<ComplianceDeficiency>> GetByHospitalFrameworkAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        return await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.HospitalId == hospitalId && d.FrameworkId == frameworkId)
            .OrderByDescending(d => d.DetectedDate)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ComplianceDeficiency>> GetOpenByObligationAsync(
        Guid obligationId,
        CancellationToken ct = default)
    {
        return await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .Where(d => d.ComplianceObligationId == obligationId
                     && d.Status != DeficiencyStatus.Closed)
            .ToListAsync(ct);
    }

    public async Task<ComplianceDeficiency?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }
}
