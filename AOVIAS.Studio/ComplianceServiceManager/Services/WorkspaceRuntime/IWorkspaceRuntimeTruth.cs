using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services.WorkspaceRuntime;

/// <summary>Live workspace-launch truth for ServiceInstance reconciliation. Not a second process manager.</summary>
public interface IWorkspaceRuntimeTruth
{
    bool TryGetLaunch(string instanceId, out WorkspaceLaunchSnapshot snapshot);

    IReadOnlyCollection<string> GetOwnedServiceNames(string workspaceId);

    IReadOnlyList<WorkspaceLaunchSnapshot> GetAll();
}

public readonly struct WorkspaceLaunchSnapshot
{
    public WorkspaceLaunchSnapshot(
        string workspaceId,
        string serviceName,
        ServiceInfoModel launch,
        bool owned,
        bool startedByRuntime = false)
    {
        WorkspaceId = workspaceId;
        ServiceName = serviceName;
        Launch = launch;
        Owned = owned;
        StartedByRuntime = startedByRuntime;
    }

    public string WorkspaceId { get; }

    public string ServiceName { get; }

    public ServiceInfoModel Launch { get; }

    public bool Owned { get; }

    /// <summary>
    /// True when Workspace Runtime started this process in the current launch object.
    /// Distinct from <see cref="Owned"/>: a failed start is started-by-runtime but not owned.
    /// </summary>
    public bool StartedByRuntime { get; }

    public int? ProcessId => Launch.ProcessId;

    public int? Port => Launch.HttpsPort ?? Launch.HttpPort ?? Launch.Port;

    public string? SourcePath => Launch.ProjectPath;
}
