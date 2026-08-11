namespace ComplianceService.Application.Dashboard.Summary;

public interface IDashboardSummaryCache
{
    Task<DashboardSummaryPayload?> TryGetAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        Guid tenantId,
        Guid? hospitalId,
        Guid? frameworkId,
        DashboardSummaryPayload payload,
        CancellationToken cancellationToken = default);

    void Invalidate(Guid tenantId, Guid? hospitalId = null, Guid? frameworkId = null);
}
