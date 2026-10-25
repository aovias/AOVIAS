using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.WorkspaceRuntime;
using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Services.WorkspaceRuntime;

/// <summary>
/// Plans and gates workspace-scoped execution. Does not launch Cursor/Claude IDE processes.
/// Does not mutate Git or worktrees. Does not stop Shared Runtime-owned services.
/// </summary>
public interface IWorkspaceRuntimeService
{
    event Action? StateChanged;

    WorkspaceRuntimeSession GetSession(string workspaceId);

    WorkspaceRuntimePlan? GetLastPlan(string workspaceId);

    bool IsStarting(string workspaceId);

    WorkspaceRuntimePlan Plan(
        RuntimeProfileItem profile,
        string workspaceId,
        DevelopmentWorkspaceItem? snapshot,
        IReadOnlyList<string>? requestedServices = null,
        string? agentSessionId = null);

    WorkspaceRuntimeValidationIssue? ValidateInstanceForWorkspaceRuntime(ServiceInstanceItem instance, string workspaceId);

    Task<WorkspaceRuntimeExecutionResult> StartAsync(
        RuntimeProfileItem profile,
        string workspaceId,
        DevelopmentWorkspaceItem? snapshot,
        IReadOnlyList<string>? requestedServices = null,
        string? agentSessionId = null,
        CancellationToken cancellationToken = default);

    Task<WorkspaceRuntimeExecutionResult> StopAsync(
        string workspaceId,
        CancellationToken cancellationToken = default);

    void CancelStartup(string workspaceId);
}
