using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services.WorkspaceRuntime;

public sealed class WorkspaceRuntimeProcessMap : IWorkspaceRuntimeTruth
{
    private readonly object gate = new();
    private readonly Dictionary<string, WorkspaceLaunchSnapshot> byInstance = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(
        string instanceId,
        string workspaceId,
        string serviceName,
        ServiceInfoModel launch,
        bool owned,
        bool startedByRuntime = false)
    {
        lock (gate)
        {
            byInstance[instanceId] = new WorkspaceLaunchSnapshot(
                workspaceId,
                serviceName,
                launch,
                owned,
                startedByRuntime);
        }
    }

    public void Remove(string instanceId)
    {
        lock (gate)
        {
            byInstance.Remove(instanceId);
        }
    }

    public void ClearWorkspace(string workspaceId)
    {
        lock (gate)
        {
            var ids = byInstance
                .Where(pair => string.Equals(pair.Value.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .ToList();
            foreach (var id in ids)
            {
                byInstance.Remove(id);
            }
        }
    }

    public bool TryGetLaunch(string instanceId, out WorkspaceLaunchSnapshot snapshot)
    {
        lock (gate)
        {
            return byInstance.TryGetValue(instanceId, out snapshot);
        }
    }

    public IReadOnlyCollection<string> GetOwnedServiceNames(string workspaceId)
    {
        lock (gate)
        {
            return byInstance.Values
                .Where(item => item.Owned && string.Equals(item.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.ServiceName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public IReadOnlyList<WorkspaceLaunchSnapshot> GetAll()
    {
        lock (gate)
        {
            return byInstance.Values.ToList();
        }
    }

    public IReadOnlyList<(string InstanceId, WorkspaceLaunchSnapshot Snapshot)> GetEntries()
    {
        lock (gate)
        {
            return byInstance.Select(pair => (pair.Key, pair.Value)).ToList();
        }
    }
}
