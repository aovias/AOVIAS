using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Services.AgentSessions;
using ComplianceServiceManager.Services.LocalResources;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.WorkspaceRuntime;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.ServiceInstances;

/// <summary>
/// Reconciles persisted instance identity with D1 workspace, D2 profile, D4 session, and S3/D2 runtime truth.
/// </summary>
public sealed class ServiceInstanceService : IServiceInstanceService
{
    private readonly ServiceInstanceRegistry registry;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly IGitWorkspaceService git;
    private readonly IServiceRuntimeHost runtimeHost;
    private readonly IRuntimeProfileOrchestrator? profiles;
    private readonly IAgentSessionService? sessions;
    private readonly ILocalResourceMonitoringService? resources;
    private readonly IWorkspaceRuntimeTruth? workspaceTruth;
    private readonly ILoggingService logger;
    private readonly object gate = new();
    private List<ServiceInstanceItem> items = [];

    public ServiceInstanceService(
        ServiceInstanceRegistry registry,
        DevelopmentWorkspaceRegistry workspaces,
        IGitWorkspaceService git,
        IServiceRuntimeHost runtimeHost,
        ILoggingService logger,
        IRuntimeProfileOrchestrator? profiles = null,
        IAgentSessionService? sessions = null,
        ILocalResourceMonitoringService? resources = null,
        IWorkspaceRuntimeTruth? workspaceTruth = null)
    {
        this.registry = registry;
        this.workspaces = workspaces;
        this.git = git;
        this.runtimeHost = runtimeHost;
        this.logger = logger;
        this.profiles = profiles;
        this.sessions = sessions;
        this.resources = resources;
        this.workspaceTruth = workspaceTruth;
    }

    public event Action? InstancesChanged;

    public IReadOnlyList<ServiceInstanceItem> GetInstances()
    {
        lock (gate)
        {
            return items.ToList();
        }
    }

    public IReadOnlyList<ServiceInstanceItem> GetInstancesForService(string serviceId)
    {
        lock (gate)
        {
            return items
                .Where(item => string.Equals(item.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        foreach (var service in runtimeHost.Services)
        {
            if (string.IsNullOrWhiteSpace(service.Name))
            {
                continue;
            }

            EnsureSharedInstance(service.Name, service.Name);
        }

        var definitions = registry.GetAll();
        var reconciled = new List<ServiceInstanceItem>(definitions.Count);
        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reconciled.Add(await ReconcileAsync(definition, cancellationToken));
        }

        lock (gate)
        {
            items = reconciled;
        }

        InstancesChanged?.Invoke();
        logger.Info("ServiceInstances", $"Reconciled {reconciled.Count} service instance(s).");
    }

    public ServiceInstanceItem EnsureSharedInstance(string serviceId, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            throw new ArgumentException("ServiceId is required.", nameof(serviceId));
        }

        var existing = registry.GetByServiceId(serviceId)
            .FirstOrDefault(item => item.ExecutionScope == ServiceExecutionScope.Shared);
        if (existing is not null)
        {
            return ServiceInstanceItem.FromDefinition(existing);
        }

        var definition = new ServiceInstanceDefinition
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            ServiceId = serviceId.Trim(),
            ServiceName = string.IsNullOrWhiteSpace(serviceName) ? serviceId.Trim() : serviceName.Trim(),
            ExecutionScope = ServiceExecutionScope.Shared,
            Ownership = ServiceInstanceOwnership.Shared,
            WorkspaceId = null,
            AgentSessionId = null,
            RuntimeProfileId = null,
            CreatedAt = DateTimeOffset.UtcNow
        };
        registry.Upsert(definition);
        return ServiceInstanceItem.FromDefinition(definition);
    }

    public ServiceInstanceItem EnsureWorkspaceInstance(
        string serviceId,
        string serviceName,
        string workspaceId,
        string? agentSessionId = null,
        string? runtimeProfileId = null)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
        {
            throw new ArgumentException("ServiceId is required.", nameof(serviceId));
        }

        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            throw new ArgumentException("WorkspaceId is required for workspace-scoped instances.", nameof(workspaceId));
        }

        var existing = registry.GetByServiceId(serviceId)
            .FirstOrDefault(item =>
                item.ExecutionScope == ServiceExecutionScope.Workspace
                && string.Equals(item.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(agentSessionId) || !string.IsNullOrWhiteSpace(runtimeProfileId))
            {
                existing.AgentSessionId = string.IsNullOrWhiteSpace(agentSessionId) ? existing.AgentSessionId : agentSessionId;
                existing.RuntimeProfileId = string.IsNullOrWhiteSpace(runtimeProfileId) ? existing.RuntimeProfileId : runtimeProfileId;
                registry.Upsert(existing);
            }

            return ServiceInstanceItem.FromDefinition(existing);
        }

        var definition = new ServiceInstanceDefinition
        {
            InstanceId = Guid.NewGuid().ToString("N"),
            ServiceId = serviceId.Trim(),
            ServiceName = string.IsNullOrWhiteSpace(serviceName) ? serviceId.Trim() : serviceName.Trim(),
            ExecutionScope = ServiceExecutionScope.Workspace,
            Ownership = ServiceInstanceOwnership.Workspace,
            WorkspaceId = workspaceId.Trim(),
            AgentSessionId = string.IsNullOrWhiteSpace(agentSessionId) ? null : agentSessionId.Trim(),
            RuntimeProfileId = string.IsNullOrWhiteSpace(runtimeProfileId) ? null : runtimeProfileId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        registry.Upsert(definition);
        return ServiceInstanceItem.FromDefinition(definition);
    }

    public IReadOnlyList<ServiceInstanceItem> GetNotConfiguredPlaceholders(string serviceId, string serviceName)
    {
        var configuredWorkspaceIds = new HashSet<string>(
            registry.GetByServiceId(serviceId)
                .Where(item => item.ExecutionScope == ServiceExecutionScope.Workspace && !string.IsNullOrWhiteSpace(item.WorkspaceId))
                .Select(item => item.WorkspaceId!),
            StringComparer.OrdinalIgnoreCase);

        var placeholders = new List<ServiceInstanceItem>();
        foreach (var workspace in workspaces.GetDocument().Repositories
                     .SelectMany(repo => repo.Workspaces)
                     .Where(ws => !ws.IsMain && ws.Enabled))
        {
            if (configuredWorkspaceIds.Contains(workspace.Id))
            {
                continue;
            }

            placeholders.Add(ServiceInstanceItem.CreateNotConfiguredPlaceholder(
                serviceId,
                serviceName,
                workspace.Id,
                OperatorLabels.Workspace(workspace.Id)));
        }

        return placeholders;
    }

    private async Task<ServiceInstanceItem> ReconcileAsync(
        ServiceInstanceDefinition definition,
        CancellationToken cancellationToken)
    {
        var item = ServiceInstanceItem.FromDefinition(definition);
        item.AttentionReasons.Clear();

        if (definition.ExecutionScope == ServiceExecutionScope.Shared)
        {
            item.WorkspaceDisplay = "Shared";
            item.AgentDisplay = "Shared";
            item.Branch = null;
            item.SourcePath = workspaces.GetDocument().Repositories
                .SelectMany(repo => repo.Workspaces)
                .FirstOrDefault(ws => ws.IsMain)?.Path;
            ApplySharedRuntimeTruth(item, definition.ServiceId);
            item.ScopeDisplay = ServiceInstanceItem.FormatScopeDisplay(definition.ExecutionScope, definition.WorkspaceId);
            item.OwnershipDisplay = ServiceInstanceItem.FormatOwnershipDisplay(definition.Ownership, definition.WorkspaceId);
            ApplyAssociations(item, definition);
            return item;
        }

        // Workspace-scoped
        var workspaceConfig = workspaces.GetDocument().Repositories
            .SelectMany(repo => repo.Workspaces)
            .FirstOrDefault(ws => string.Equals(ws.Id, definition.WorkspaceId, StringComparison.OrdinalIgnoreCase));

        if (workspaceConfig is null || !workspaceConfig.Enabled)
        {
            item.WorkspaceDisplay = definition.WorkspaceId ?? "Unknown";
            item.ApplyState(ServiceInstanceState.Unavailable, "Workspace is missing or disabled.");
            item.AttentionReasons.Add("Workspace is missing or disabled.");
            item.ApplyPort(null);
            item.ApplyProcessId(null);
            ApplyAssociations(item, definition);
            return item;
        }

        item.WorkspaceDisplay = OperatorLabels.Workspace(workspaceConfig.Id);
        item.AgentDisplay = OperatorLabels.Agent(workspaceConfig.Id);
        item.SourcePath = workspaceConfig.Path;

        try
        {
            if (!Directory.Exists(workspaceConfig.Path))
            {
                item.ApplyState(ServiceInstanceState.Unavailable, "Workspace path does not exist.");
                item.AttentionReasons.Add("Workspace path does not exist.");
                item.ApplyPort(null);
                item.ApplyProcessId(null);
            }
            else
            {
                var gitState = await git.GetWorkspaceStateAsync(workspaceConfig.Path, cancellationToken);
                item.Branch = gitState.Branch;
                ApplyWorkspaceRuntimeTruth(item, definition, workspaceConfig.Path);
            }
        }
        catch (Exception ex)
        {
            item.ApplyState(ServiceInstanceState.Unknown, ex.Message);
            item.AttentionReasons.Add(ex.Message);
            item.ApplyPort(null);
            item.ApplyProcessId(null);
        }

        ApplyAssociations(item, definition);
        item.ScopeDisplay = ServiceInstanceItem.FormatScopeDisplay(definition.ExecutionScope, definition.WorkspaceId);
        return item;
    }

    private void ApplyWorkspaceRuntimeTruth(
        ServiceInstanceItem item,
        ServiceInstanceDefinition definition,
        string workspacePath)
    {
        item.ScopeDisplay = ServiceInstanceItem.FormatScopeDisplay(definition.ExecutionScope, definition.WorkspaceId);

        if (workspaceTruth is not null && workspaceTruth.TryGetLaunch(definition.InstanceId, out var snapshot))
        {
            item.SourcePath = snapshot.SourcePath ?? workspacePath;
            item.ApplyPort(snapshot.Port);
            item.ApplyProcessId(snapshot.ProcessId);
            item.OwnershipDisplay = ServiceInstanceItem.FormatOwnershipDisplay(
                definition.Ownership,
                definition.WorkspaceId,
                snapshot.Owned);
            var reason = snapshot.Owned
                ? $"Owner: {ServiceInstanceItem.FormatOwnershipDisplay(definition.Ownership, definition.WorkspaceId)}"
                : "Already running — not claimed by Workspace Runtime.";
            item.ApplyState(MapServiceStatus(snapshot.Launch.Status), reason);
            return;
        }

        item.SourcePath = workspacePath;
        item.ApplyPort(null);
        item.ApplyProcessId(null);
        item.OwnershipDisplay = ServiceInstanceItem.FormatOwnershipDisplay(definition.Ownership, definition.WorkspaceId);
        item.ApplyState(ServiceInstanceState.Defined, "Configured — awaiting Workspace Runtime start.");
    }

    private void ApplySharedRuntimeTruth(ServiceInstanceItem item, string serviceId)
    {
        var service = runtimeHost.Services.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, serviceId, StringComparison.OrdinalIgnoreCase));

        if (service is null)
        {
            item.ApplyState(ServiceInstanceState.Defined, "Service not currently discovered.");
            item.ApplyPort(null);
            item.ApplyProcessId(null);
            return;
        }

        // Port/PID only when the shared runtime host already knows them — never invent.
        var knownPort = service.HttpsPort ?? service.HttpPort ?? service.Port;
        item.ApplyPort(knownPort);
        item.ApplyProcessId(service.ProcessId);

        item.ApplyState(MapServiceStatus(service.Status), service.LastAction);

        // Optional S3 confirmation note — do not invent PIDs from resource rows alone without host match.
        _ = resources?.LatestSnapshot;
    }

    private void ApplyAssociations(ServiceInstanceItem item, ServiceInstanceDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.RuntimeProfileId) && profiles is not null)
        {
            var profile = profiles.GetProfiles().FirstOrDefault(candidate =>
                string.Equals(candidate.Id, definition.RuntimeProfileId, StringComparison.OrdinalIgnoreCase));
            item.RuntimeProfileDisplay = profile?.Name ?? $"{definition.RuntimeProfileId} (missing)";
            if (profile is null)
            {
                item.AttentionReasons.Add("Runtime profile reference is missing.");
            }
        }
        else
        {
            item.RuntimeProfileDisplay = "—";
        }

        if (!string.IsNullOrWhiteSpace(definition.AgentSessionId) && sessions is not null)
        {
            var session = sessions.GetSessions().FirstOrDefault(candidate =>
                string.Equals(candidate.Id, definition.AgentSessionId, StringComparison.OrdinalIgnoreCase));
            item.AgentSessionDisplay = session is null
                ? $"{definition.AgentSessionId} (missing)"
                : $"{session.AgentDisplay}: {session.Title}";
            if (session is null)
            {
                item.AttentionReasons.Add("Agent session reference is missing.");
            }
        }
        else
        {
            item.AgentSessionDisplay = "None";
        }
    }

    private static ServiceInstanceState MapServiceStatus(ServiceStatus status)
        => status switch
        {
            ServiceStatus.Running => ServiceInstanceState.Running,
            ServiceStatus.Starting => ServiceInstanceState.Starting,
            ServiceStatus.Stopping => ServiceInstanceState.Stopping,
            ServiceStatus.Stopped => ServiceInstanceState.Stopped,
            ServiceStatus.Error or ServiceStatus.FailedRepair => ServiceInstanceState.Failed,
            _ => ServiceInstanceState.Unknown
        };
}
