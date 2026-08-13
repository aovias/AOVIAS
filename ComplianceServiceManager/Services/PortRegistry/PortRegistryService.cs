using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.PortRegistry;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.PortRegistry;

public sealed class PortRegistryService : IPortRegistryService
{
    private readonly PortRegistryStore store;
    private readonly IPortAvailabilityService availability;
    private readonly IServiceInstanceService instances;
    private readonly ServiceInstanceRegistry instanceRegistry;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly IServiceRuntimeHost runtimeHost;
    private readonly ILoggingService logger;
    private readonly object gate = new();
    private List<PortAllocationItem> items = [];

    public PortRegistryService(
        PortRegistryStore store,
        IPortAvailabilityService availability,
        IServiceInstanceService instances,
        ServiceInstanceRegistry instanceRegistry,
        DevelopmentWorkspaceRegistry workspaces,
        IServiceRuntimeHost runtimeHost,
        ILoggingService logger)
    {
        this.store = store;
        this.availability = availability;
        this.instances = instances;
        this.instanceRegistry = instanceRegistry;
        this.workspaces = workspaces;
        this.runtimeHost = runtimeHost;
        this.logger = logger;
    }

    public event Action? AllocationsChanged;

    public IReadOnlyList<PortAllocationItem> GetAllocations()
    {
        lock (gate)
        {
            return items.ToList();
        }
    }

    public PortAllocationItem? GetByPort(int port)
    {
        lock (gate)
        {
            return items.FirstOrDefault(item => item.Port == port);
        }
    }

    public PortAllocationItem? GetByInstanceId(string instanceId)
    {
        lock (gate)
        {
            return items.FirstOrDefault(item =>
                string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public PortOccupancyObservation ObservePort(int port) => availability.Observe(port);

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Ensure shared instances exist for discovered services so reconciliation has context.
        await instances.RefreshAsync(cancellationToken);

        var definitions = store.GetAll();
        var reconciled = new List<PortAllocationItem>(definitions.Count);
        foreach (var definition in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            reconciled.Add(Reconcile(definition));
        }

        lock (gate)
        {
            items = reconciled.OrderBy(item => item.Port).ToList();
        }

        AllocationsChanged?.Invoke();
        logger.Info("PortRegistry", $"Reconciled {reconciled.Count} port allocation(s).");
    }

    public PortRegistryResult ValidateAssignment(int port, string instanceId)
    {
        if (port is <= 0 or > 65535)
        {
            return PortRegistryResult.Fail("Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return PortRegistryResult.Fail("Service instance id is required.");
        }

        var instance = ResolveInstance(instanceId);
        if (instance is null)
        {
            return PortRegistryResult.Fail("Service instance does not exist or is not configured.");
        }

        if (instance.ExecutionScope == ServiceExecutionScope.Workspace)
        {
            if (string.IsNullOrWhiteSpace(instance.WorkspaceId))
            {
                return PortRegistryResult.Fail("Workspace-scoped instance is missing WorkspaceId.");
            }

            var workspace = workspaces.GetDocument().Repositories
                .SelectMany(repo => repo.Workspaces)
                .FirstOrDefault(ws => string.Equals(ws.Id, instance.WorkspaceId, StringComparison.OrdinalIgnoreCase));
            if (workspace is null || !workspace.Enabled)
            {
                return PortRegistryResult.Fail("Workspace does not exist or is disabled.");
            }
        }

        var existingPort = store.GetByPort(port);
        if (existingPort is not null
            && !string.Equals(existingPort.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
        {
            return PortRegistryResult.Fail($"Port {port} is already assigned to another service instance.");
        }

        return PortRegistryResult.Ok();
    }

    public PortRegistryResult Assign(int port, string instanceId)
    {
        lock (gate)
        {
            var validation = ValidateAssignment(port, instanceId);
            if (!validation.Succeeded)
            {
                return validation;
            }

            var instance = ResolveInstance(instanceId)!;

            var existingPort = store.GetByPort(port);
            if (existingPort is not null
                && !string.Equals(existingPort.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
            {
                return PortRegistryResult.Fail($"Port {port} is already assigned to another service instance.");
            }

            var definition = new PortAllocationDefinition
            {
                Port = port,
                InstanceId = instance.InstanceId,
                ServiceId = instance.ServiceId,
                WorkspaceId = instance.WorkspaceId,
                CreatedAt = store.GetByInstanceId(instanceId)?.CreatedAt ?? DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            try
            {
                store.Upsert(definition);
            }
            catch (InvalidOperationException ex)
            {
                return PortRegistryResult.Fail(ex.Message);
            }

            var item = Reconcile(definition);
            items = store.GetAll().Select(Reconcile).OrderBy(a => a.Port).ToList();
            AllocationsChanged?.Invoke();
            logger.Info("PortRegistry", $"Assigned port {port} to instance {instanceId}.");
            return PortRegistryResult.Ok(item);
        }
    }

    public PortRegistryResult Release(int port)
    {
        lock (gate)
        {
            if (!store.RemoveByPort(port))
            {
                return PortRegistryResult.Fail($"No registry assignment found for port {port}.");
            }

            items = store.GetAll().Select(Reconcile).OrderBy(a => a.Port).ToList();
            AllocationsChanged?.Invoke();
            logger.Info("PortRegistry", $"Released port {port} from registry (process not stopped).");
            return PortRegistryResult.Ok();
        }
    }

    private PortAllocationItem Reconcile(PortAllocationDefinition definition)
    {
        var item = new PortAllocationItem
        {
            Port = definition.Port,
            InstanceId = definition.InstanceId,
            ServiceId = definition.ServiceId,
            IsAssigned = true,
            CreatedAt = definition.CreatedAt,
            UpdatedAt = definition.UpdatedAt
        };

        var instance = instances.GetInstances()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.InstanceId, definition.InstanceId, StringComparison.OrdinalIgnoreCase));

        if (instance is null)
        {
            item.ServiceName = definition.ServiceId;
            item.InstanceDisplay = "—";
            item.WorkspaceId = definition.WorkspaceId;
            item.WorkspaceDisplay = OperatorLabels.Workspace(definition.WorkspaceId);
            item.ScopeDisplay = string.IsNullOrWhiteSpace(definition.WorkspaceId) ? "Shared" : OperatorLabels.Workspace(definition.WorkspaceId);
            item.ApplyEffectiveStatus(PortEffectiveStatus.AssignedUnavailable, "Service instance is missing.");
            ApplyOccupancy(item, definition.Port, expectedProcessId: null);
            return item;
        }

        item.ServiceName = instance.ServiceName;
        item.ExecutionScope = instance.ExecutionScope;
        item.ScopeDisplay = instance.ScopeDisplay;
        item.InstanceDisplay = BuildInstanceDisplay(instance);
        item.WorkspaceId = instance.WorkspaceId;
        item.WorkspaceDisplay = instance.WorkspaceDisplay;

        if (instance.ExecutionScope == ServiceExecutionScope.Workspace)
        {
            var workspace = workspaces.GetDocument().Repositories
                .SelectMany(repo => repo.Workspaces)
                .FirstOrDefault(ws => string.Equals(ws.Id, instance.WorkspaceId, StringComparison.OrdinalIgnoreCase));
            if (workspace is null || !workspace.Enabled
                || instance.State == ServiceInstanceState.Unavailable)
            {
                var reason = instance.State == ServiceInstanceState.Unavailable
                    ? (instance.StatusReason ?? "Workspace unavailable.")
                    : "Workspace unavailable.";
                item.ApplyEffectiveStatus(PortEffectiveStatus.AssignedUnavailable, reason);
                ApplyOccupancy(item, definition.Port, expectedProcessId: null);
                return item;
            }
        }

        var expectedPid = ResolveExpectedProcessId(instance);
        ApplyOccupancy(item, definition.Port, expectedPid);
        return item;
    }

    private void ApplyOccupancy(PortAllocationItem item, int port, int? expectedProcessId)
    {
        var observation = availability.Observe(port);
        item.OccupancyDisplay = observation.AvailabilityDisplay;
        item.OccupantProcessId = observation.ProcessId;

        // Preserve Unavailable (e.g. missing workspace/instance) — occupancy is supplemental only.
        if (item.EffectiveStatus == PortEffectiveStatus.AssignedUnavailable)
        {
            if (!string.IsNullOrWhiteSpace(observation.Error))
            {
                item.OccupantDisplay = "Unknown";
            }
            else if (!observation.IsListening)
            {
                item.OccupantDisplay = "—";
            }
            else
            {
                item.OccupantDisplay = observation.ProcessId is int pid
                    ? $"PID {pid}" + (string.IsNullOrWhiteSpace(observation.ProcessName) ? string.Empty : $" ({observation.ProcessName})")
                    : "PID Unknown";
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(observation.Error))
        {
            item.OccupantDisplay = "Unknown";
            item.ApplyEffectiveStatus(PortEffectiveStatus.AssignedUnavailable, observation.Error);
            return;
        }

        if (!observation.IsListening)
        {
            item.OccupantDisplay = "—";
            item.ApplyEffectiveStatus(PortEffectiveStatus.AssignedAvailable);
            return;
        }

        item.OccupantDisplay = observation.ProcessId is int processId
            ? $"PID {processId}" + (string.IsNullOrWhiteSpace(observation.ProcessName) ? string.Empty : $" ({observation.ProcessName})")
            : "PID Unknown";

        if (expectedProcessId is int expected
            && observation.ProcessId is int actual
            && expected == actual)
        {
            item.ApplyEffectiveStatus(PortEffectiveStatus.AssignedOccupied, "Occupied by expected process.");
            return;
        }

        item.ApplyEffectiveStatus(
            PortEffectiveStatus.AssignedConflict,
            "Port is occupied by a process that is not authoritatively linked to this instance.");
    }

    private ServiceInstanceItem? ResolveInstance(string instanceId)
    {
        var live = instances.GetInstances()
            .FirstOrDefault(item => string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        if (live is not null)
        {
            return live;
        }

        var definition = instanceRegistry.GetById(instanceId);
        return definition is null ? null : ServiceInstanceItem.FromDefinition(definition);
    }

    private int? ResolveExpectedProcessId(ServiceInstanceItem instance)
    {
        if (instance.ExecutionScope != ServiceExecutionScope.Shared)
        {
            return null;
        }

        var service = runtimeHost.Services.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, instance.ServiceId, StringComparison.OrdinalIgnoreCase));
        return service?.ProcessId;
    }

    private static string BuildInstanceDisplay(ServiceInstanceItem instance)
    {
        if (instance.ExecutionScope == ServiceExecutionScope.Shared)
        {
            return "Shared";
        }

        if (!string.IsNullOrWhiteSpace(instance.WorkspaceId))
        {
            var id = instance.WorkspaceId.Trim();
            return string.Equals(id, "cursor", StringComparison.OrdinalIgnoreCase) ? "Cursor"
                : string.Equals(id, "claude", StringComparison.OrdinalIgnoreCase) ? "Claude"
                : id;
        }

        return instance.WorkspaceDisplay;
    }
}
