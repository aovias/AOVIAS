using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.SharedRuntime;

namespace ComplianceServiceManager.Services.SharedRuntime;

/// <summary>
/// Explicit Shared Runtime planning and gated execution over D2 Runtime Profiles.
/// Does not execute workspace/Cursor/Claude instances. Does not start processes during Plan.
/// </summary>
public interface ISharedRuntimeService
{
    event Action? StateChanged;

    SharedRuntimeSession Session { get; }

    SharedRuntimePlan? LastPlan { get; }

    bool IsStarting { get; }

    SharedRuntimePlan Plan(RuntimeProfileItem profile);

    SharedRuntimeValidationIssue? ValidateInstanceForSharedRuntime(ServiceInstanceItem instance);

    Task<SharedRuntimeExecutionResult> StartAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default);

    Task<SharedRuntimeExecutionResult> StopAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default);

    void CancelStartup();
}
