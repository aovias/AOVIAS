using ComplianceServiceManager.Models.ServiceInstances;

namespace ComplianceServiceManager.Services.ServiceInstances;

/// <summary>
/// Read-only awareness of service execution contexts.
/// Does not allocate ports, launch processes, or mutate D2/S3 runtime.
/// </summary>
public interface IServiceInstanceService
{
    event Action? InstancesChanged;

    IReadOnlyList<ServiceInstanceItem> GetInstances();

    IReadOnlyList<ServiceInstanceItem> GetInstancesForService(string serviceId);

    /// <summary>
    /// Ensures a Shared instance exists for each discovered service name, then reconciles all.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    ServiceInstanceItem EnsureSharedInstance(string serviceId, string serviceName);

    ServiceInstanceItem EnsureWorkspaceInstance(
        string serviceId,
        string serviceName,
        string workspaceId,
        string? agentSessionId = null,
        string? runtimeProfileId = null);

    /// <summary>
    /// Placeholder rows for agent workspaces that do not yet have a configured instance.
    /// </summary>
    IReadOnlyList<ServiceInstanceItem> GetNotConfiguredPlaceholders(string serviceId, string serviceName);
}
