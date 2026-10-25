using Platform.Dashboard.Models;

namespace ComplianceService.Application.Dashboard.Summary;

public interface IDashboardSummaryEngine
{
    Task<DashboardSummaryPayload> GetAsync(
        DashboardContext context,
        CancellationToken cancellationToken = default);
}
