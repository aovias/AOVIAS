using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public interface IRepairAnalyticsService
{
    Task RecordAsync(ServiceInfoModel service, TimeSpan duration, bool success, FailureCategory category, CancellationToken cancellationToken);

    RepairAnalyticsSnapshot GetSnapshot();
}
