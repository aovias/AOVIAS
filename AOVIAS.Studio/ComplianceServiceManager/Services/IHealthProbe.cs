using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

/// <summary>Health probe used while waiting for profile members to become ready.</summary>
public interface IHealthProbe
{
    Task<HealthStatus> CheckAsync(ServiceInfoModel service, CancellationToken cancellationToken);
}
