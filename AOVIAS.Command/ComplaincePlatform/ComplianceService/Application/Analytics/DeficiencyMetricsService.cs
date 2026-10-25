using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Analytics;

public sealed class DeficiencyMetricsService : IDeficiencyMetricsService
{
    private readonly ComplianceDbContext _db;
    private readonly ICapaQueryService _capaQuery;
    private readonly IEscalationService _escalationService;

    public DeficiencyMetricsService(
        ComplianceDbContext db,
        ICapaQueryService capaQuery,
        IEscalationService escalationService)
    {
        _db = db;
        _capaQuery = capaQuery;
        _escalationService = escalationService;
    }

    public async Task<DeficiencyMetricCountsDto> GetCountsAsync(AnalyticsScope scope, CancellationToken ct = default)
    {
        var rows = await QueryScopedAsync(scope, ct).ToListAsync(ct);

        return new DeficiencyMetricCountsDto
        {
            Total = rows.Count,
            Open = rows.Count(DeficiencyMetricDefinitions.IsOpen),
            Closed = rows.Count(DeficiencyMetricDefinitions.IsClosed),
            Cancelled = rows.Count(DeficiencyMetricDefinitions.IsCancelled),
            Verified = rows.Count(DeficiencyMetricDefinitions.IsVerified),
            CriticalOpen = rows.Count(d => DeficiencyMetricDefinitions.IsOpen(d) && DeficiencyMetricDefinitions.IsCriticalDeficiency(d)),
            BlockingOpen = rows.Count(d => DeficiencyMetricDefinitions.IsOpen(d) && DeficiencyMetricDefinitions.IsCriticalDeficiency(d)),
            RecurringOpen = rows.Count(d => DeficiencyMetricDefinitions.IsOpen(d) && DeficiencyMetricDefinitions.IsRecurring(d)),
            EscalatedOpen = rows.Count(d => DeficiencyMetricDefinitions.IsOpen(d) && DeficiencyMetricDefinitions.IsEscalated(d)),
            CriticalEscalatedOpen = rows.Count(d =>
                DeficiencyMetricDefinitions.IsOpen(d)
                && DeficiencyMetricDefinitions.IsEscalated(d)
                && DeficiencyMetricDefinitions.IsCriticalDeficiency(d)),
            AuditOriginOpen = rows.Count(d =>
                DeficiencyMetricDefinitions.IsOpen(d)
                && d.OriginType == DeficiencyOriginType.AuditFinding),
        };
    }

    public async Task<IReadOnlyList<ComplianceDeficiency>> GetScopedOpenAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        await QueryScopedAsync(scope, ct)
            .Where(d => d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled)
            .ToListAsync(ct);

    public Task<int> CountOpenFindingsAsync(Guid organizationId, CancellationToken ct = default) =>
        _db.Findings
            .AsNoTracking()
            .CountAsync(
                f => f.TenantId == organizationId
                     && f.Status != FindingStatus.Closed
                     && f.Status != FindingStatus.Verified,
                ct);

    public Task<CapaMetricsDto> GetCapaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        _capaQuery.GetMetricsAsync(scope.OrganizationId, scope.HospitalId, scope.FrameworkId, ct);

    public Task<SlaEscalationMetricsDto> GetSlaMetricsAsync(AnalyticsScope scope, CancellationToken ct = default) =>
        _escalationService.GetMetricsForScopeAsync(scope.OrganizationId, scope.HospitalId, scope.FrameworkId, ct);

    private IQueryable<ComplianceDeficiency> QueryScopedAsync(AnalyticsScope scope, CancellationToken ct)
    {
        _ = ct;
        // Tenant filter deliberately left in place — see DeficiencyCommandCenterQueryService.
        // These counts feed the same dashboard and must be scoped identically, or the header
        // totals and the CAPA/SLA panels below them disagree.
        return _db.ComplianceDeficiencies
            .AsNoTracking()
            .Where(d => d.HospitalId == scope.HospitalId && d.FrameworkId == scope.FrameworkId);
    }
}
