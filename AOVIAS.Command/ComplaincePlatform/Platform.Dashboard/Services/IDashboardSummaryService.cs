using Platform.Dashboard.Models;

namespace Platform.Dashboard.Services;

public interface IDashboardSummaryService
{
    Task<DashboardSummaryResult> GetSummaryAsync(
        DashboardContext context,
        CancellationToken cancellationToken = default);
}
