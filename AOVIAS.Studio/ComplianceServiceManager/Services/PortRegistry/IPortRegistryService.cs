using ComplianceServiceManager.Models.PortRegistry;

namespace ComplianceServiceManager.Services.PortRegistry;

/// <summary>
/// Central local port registry. Assigns/releases ports to ServiceInstances and reconciles OS availability.
/// Does not start/stop processes, mutate Git, or control agents.
/// </summary>
public interface IPortRegistryService
{
    event Action? AllocationsChanged;

    IReadOnlyList<PortAllocationItem> GetAllocations();

    PortAllocationItem? GetByPort(int port);

    PortAllocationItem? GetByInstanceId(string instanceId);

    Task RefreshAsync(CancellationToken cancellationToken = default);

    PortRegistryResult ValidateAssignment(int port, string instanceId);

    PortRegistryResult Assign(int port, string instanceId);

    PortRegistryResult Release(int port);

    PortOccupancyObservation ObservePort(int port);
}
