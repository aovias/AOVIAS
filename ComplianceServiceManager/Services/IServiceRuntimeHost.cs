using System.Collections.ObjectModel;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

/// <summary>
/// Narrow process lifecycle surface used by runtime profiles (and testable without a full Studio stack).
/// </summary>
public interface IServiceRuntimeHost
{
    ObservableCollection<ServiceInfoModel> Services { get; }

    Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken);

    Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken);
}
