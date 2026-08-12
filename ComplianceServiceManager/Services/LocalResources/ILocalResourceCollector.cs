using ComplianceServiceManager.Models.LocalResources;

namespace ComplianceServiceManager.Services.LocalResources;

/// <summary>
/// Low-level local host/process sampler. Implementations must never throw for a single inaccessible process.
/// </summary>
public interface ILocalResourceCollector
{
    LocalHostResourceMetrics CollectHost(string diskPath, LocalResourceThresholds thresholds);

    LocalProcessResourceMetrics CollectProcess(int processId);
}
