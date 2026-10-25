using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.PortRegistry;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.PortRegistry;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.SharedRuntime;

/// <summary>
/// Plans and gates Shared Runtime execution. Process start/stop/health/ownership stay in D2.
/// Never starts workspace-scoped, Cursor, or Claude instances.
/// </summary>
public sealed class SharedRuntimeService : ISharedRuntimeService
{
    private readonly IRuntimeProfileOrchestrator orchestrator;
    private readonly IServiceRuntimeHost runtimeHost;
    private readonly IServiceInstanceService instances;
    private readonly IPortRegistryService ports;
    private readonly IPortAvailabilityService availability;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly ILoggingService logger;
    private readonly object gate = new();
    private readonly SemaphoreSlim startLock = new(1, 1);
    private CancellationTokenSource? startupCts;

    public SharedRuntimeService(
        IRuntimeProfileOrchestrator orchestrator,
        IServiceRuntimeHost runtimeHost,
        IServiceInstanceService instances,
        IPortRegistryService ports,
        IPortAvailabilityService availability,
        DevelopmentWorkspaceRegistry workspaces,
        ILoggingService logger)
    {
        this.orchestrator = orchestrator;
        this.runtimeHost = runtimeHost;
        this.instances = instances;
        this.ports = ports;
        this.availability = availability;
        this.workspaces = workspaces;
        this.logger = logger;
    }

    public event Action? StateChanged;

    public SharedRuntimeSession Session { get; } = new();

    public SharedRuntimePlan? LastPlan { get; private set; }

    public bool IsStarting { get; private set; }

    public SharedRuntimePlan Plan(RuntimeProfileItem profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        lock (gate)
        {
            Session.Status = SharedRuntimeStatus.Validating;
            Session.ProfileId = profile.Id;
            Session.ProfileName = profile.Name;
        }

        var issues = new List<SharedRuntimeValidationIssue>();
        var entries = new List<SharedRuntimePlanEntry>();
        var canonical = ResolveCanonicalSourcePath();

        if (string.IsNullOrWhiteSpace(profile.Id) && string.IsNullOrWhiteSpace(profile.Name))
        {
            issues.Add(Issue("invalid-profile", "Runtime Profile is missing.", phase: "validate"));
        }

        var validation = orchestrator.Validate(profile);
        if (!validation.IsValid)
        {
            foreach (var issue in validation.Issues)
            {
                issues.Add(Issue(issue.Code, issue.Message, phase: "profile"));
            }
        }

        var order = validation.StartupOrder;
        if (order.Count == 0 && validation.IsValid)
        {
            issues.Add(Issue("no-services", "Profile has no services in startup order.", phase: "profile"));
        }

        foreach (var serviceName in order)
        {
            entries.Add(PlanMember(profile, serviceName, canonical, issues));
        }

        var canStart = issues.Count == 0 && entries.TrueForAll(e => e.Action != SharedRuntimeAction.Block);
        var plan = new SharedRuntimePlan
        {
            ProfileId = profile.Id,
            ProfileName = profile.Name,
            ScopeDisplay = "Shared",
            CanonicalSource = canonical ?? "—",
            CanStart = canStart,
            StartupOrder = order,
            Entries = entries,
            Issues = issues
        };

        lock (gate)
        {
            LastPlan = plan;
            Session.Status = canStart ? SharedRuntimeStatus.Planned : SharedRuntimeStatus.Blocked;
        }

        logger.Info("SharedRuntime", $"Plan for '{profile.Name}': {plan.Summary}");
        StateChanged?.Invoke();
        return plan;
    }

    public SharedRuntimeValidationIssue? ValidateInstanceForSharedRuntime(ServiceInstanceItem instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (instance.ExecutionScope != ServiceExecutionScope.Shared)
        {
            var workspace = instance.WorkspaceId ?? "unknown";
            var reason = string.Equals(workspace, "cursor", StringComparison.OrdinalIgnoreCase)
                ? "Cursor workspace-scoped instance cannot run in Shared Runtime."
                : string.Equals(workspace, "claude", StringComparison.OrdinalIgnoreCase)
                    ? "Claude workspace-scoped instance cannot run in Shared Runtime."
                    : "Workspace-scoped instance cannot run in Shared Runtime.";
            return Issue(
                "workspace-instance",
                reason,
                instance.ServiceName,
                instance.InstanceId,
                phase: "instance");
        }

        if (!string.IsNullOrWhiteSpace(instance.WorkspaceId))
        {
            return Issue(
                "workspace-instance",
                "Shared Runtime instances must not carry a workspace identity.",
                instance.ServiceName,
                instance.InstanceId,
                phase: "instance");
        }

        if (!string.IsNullOrWhiteSpace(instance.AgentSessionId))
        {
            return Issue(
                "agent-session",
                "Shared Runtime must not be bound to an AgentSession.",
                instance.ServiceName,
                instance.InstanceId,
                phase: "instance");
        }

        return null;
    }

    public async Task<SharedRuntimeExecutionResult> StartAsync(
        RuntimeProfileItem profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!await startLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return SharedRuntimeExecutionResult.Block("Shared Runtime startup already in progress.");
        }

        CancellationTokenSource? linked = null;
        try
        {
            IsStarting = true;
            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lock (gate)
            {
                startupCts?.Dispose();
                startupCts = linked;
            }

            var plan = Plan(profile);
            if (!plan.CanStart)
            {
                var reason = plan.Issues.FirstOrDefault()?.Message
                             ?? "Shared Runtime plan is blocked. No process was started.";
                logger.Warning("SharedRuntime", $"Start blocked for '{profile.Name}': {reason}");
                StateChanged?.Invoke();
                return SharedRuntimeExecutionResult.Block(reason, plan);
            }

            lock (gate)
            {
                Session.SessionId = Guid.NewGuid().ToString("N")[..12];
                Session.StartedAt = DateTimeOffset.UtcNow;
                Session.Status = SharedRuntimeStatus.Starting;
                Session.OwnedServiceNames.Clear();
            }

            StateChanged?.Invoke();
            logger.Info("SharedRuntime", $"Starting Shared Runtime '{profile.Name}' via D2.");

            try
            {
                await orchestrator.StartAsync(profile, linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                CaptureOwnership(profile);
                lock (gate)
                {
                    Session.Status = SharedRuntimeStatus.Failed;
                }

                StateChanged?.Invoke();
                return SharedRuntimeExecutionResult.Fail(
                    "Shared Runtime startup cancelled. Already-started owned services remain running.",
                    plan,
                    SharedRuntimeStatus.Failed);
            }

            CaptureOwnership(profile);
            var status = MapProfileStatus(profile.Status);
            lock (gate)
            {
                Session.Status = status;
            }

            StateChanged?.Invoke();
            var ok = status is SharedRuntimeStatus.Running or SharedRuntimeStatus.Degraded;
            return ok
                ? SharedRuntimeExecutionResult.Ok(plan, status)
                : SharedRuntimeExecutionResult.Fail(profile.ProgressMessage ?? "Shared Runtime start failed.", plan, status);
        }
        finally
        {
            IsStarting = false;
            lock (gate)
            {
                if (ReferenceEquals(startupCts, linked))
                {
                    startupCts = null;
                }
            }

            startLock.Release();
            StateChanged?.Invoke();
        }
    }

    public async Task<SharedRuntimeExecutionResult> StopAsync(
        RuntimeProfileItem profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        lock (gate)
        {
            Session.Status = SharedRuntimeStatus.Stopping;
        }

        StateChanged?.Invoke();
        logger.Info("SharedRuntime", $"Stopping Shared Runtime '{profile.Name}' (owned services only).");

        await orchestrator.StopAsync(profile, cancellationToken).ConfigureAwait(false);

        lock (gate)
        {
            Session.OwnedServiceNames.Clear();
            Session.Status = SharedRuntimeStatus.Stopped;
            Session.StartedAt = null;
        }

        StateChanged?.Invoke();
        return SharedRuntimeExecutionResult.Ok(LastPlan, SharedRuntimeStatus.Stopped);
    }

    public void CancelStartup()
    {
        lock (gate)
        {
            startupCts?.Cancel();
        }

        logger.Info("SharedRuntime", "Cancel requested for Shared Runtime startup.");
    }

    private SharedRuntimePlanEntry PlanMember(
        RuntimeProfileItem profile,
        string serviceName,
        string? canonicalSource,
        List<SharedRuntimeValidationIssue> issues)
    {
        var service = runtimeHost.Services.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, serviceName, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            issues.Add(Issue("missing-service", $"Service '{serviceName}' is not in current discovery.", serviceName, phase: "service"));
            return BlockedEntry(serviceName, "Service definition is missing.");
        }

        var shared = instances.GetInstancesForService(serviceName)
            .FirstOrDefault(item => item.ExecutionScope == ServiceExecutionScope.Shared && item.IsConfigured);
        if (shared is null)
        {
            issues.Add(Issue(
                "missing-instance",
                $"Shared ServiceInstance is missing for '{serviceName}'.",
                serviceName,
                phase: "instance"));
            return BlockedEntry(serviceName, "Shared ServiceInstance is missing.");
        }

        var instanceIssue = ValidateInstanceForSharedRuntime(shared);
        if (instanceIssue is not null)
        {
            issues.Add(instanceIssue);
            return BlockedEntry(serviceName, instanceIssue.Message, shared.InstanceId);
        }

        var sourceIssue = ValidateCanonicalSource(service, canonicalSource);
        if (sourceIssue is not null)
        {
            issues.Add(sourceIssue);
            return BlockedEntry(serviceName, sourceIssue.Message, shared.InstanceId);
        }

        var healthIssue = ValidateHealthConfiguration(service);
        if (healthIssue is not null)
        {
            issues.Add(healthIssue);
            return BlockedEntry(serviceName, healthIssue.Message, shared.InstanceId);
        }

        var (port, portIssue) = ResolvePort(shared, service);
        if (portIssue is not null)
        {
            issues.Add(portIssue);
            return BlockedEntry(serviceName, portIssue.Message, shared.InstanceId, port);
        }

        var previouslyOwned = profile.OwnedServiceNames.Contains(serviceName);
        if (service.Status == ServiceStatus.Running)
        {
            if (service.Health is HealthStatus.Healthy or HealthStatus.Starting or HealthStatus.Unknown)
            {
                return new SharedRuntimePlanEntry
                {
                    ServiceName = serviceName,
                    InstanceId = shared.InstanceId,
                    ScopeDisplay = "Shared",
                    Port = port,
                    Action = SharedRuntimeAction.Reuse,
                    ActionDisplay = "REUSE",
                    Reason = previouslyOwned
                        ? "Already running — ownership retained."
                        : "Already running — external/not claimed.",
                    OwnedAfterStart = previouslyOwned,
                    OwnerDisplay = previouslyOwned ? "Shared Runtime" : "External"
                };
            }

            issues.Add(Issue(
                "unhealthy-running",
                $"'{serviceName}' is already running but not healthy. Shared Runtime will not blindly restart it.",
                serviceName,
                shared.InstanceId,
                port,
                "reuse"));
            return BlockedEntry(serviceName, "Already running but unhealthy.", shared.InstanceId, port);
        }

        if (port is int assignedPort)
        {
            var occupancy = availability.Observe(assignedPort);
            if (!string.IsNullOrWhiteSpace(occupancy.Error))
            {
                issues.Add(Issue(
                    "port-check-failed",
                    $"Port {assignedPort} availability check failed: {occupancy.Error}",
                    serviceName,
                    shared.InstanceId,
                    assignedPort,
                    "port"));
                return BlockedEntry(serviceName, occupancy.Error, shared.InstanceId, assignedPort);
            }

            if (occupancy.IsListening)
            {
                var expectedPid = service.ProcessId;
                if (expectedPid is int pid && occupancy.ProcessId is int actual && pid == actual)
                {
                    return new SharedRuntimePlanEntry
                    {
                        ServiceName = serviceName,
                        InstanceId = shared.InstanceId,
                        ScopeDisplay = "Shared",
                        Port = assignedPort,
                        Action = SharedRuntimeAction.Reuse,
                        ActionDisplay = "REUSE",
                        Reason = "Port occupied by expected process.",
                        OwnedAfterStart = previouslyOwned,
                        OwnerDisplay = previouslyOwned ? "Shared Runtime" : "External"
                    };
                }

                var occupant = occupancy.ProcessId is int occ
                    ? $"PID {occ}" + (string.IsNullOrWhiteSpace(occupancy.ProcessName) ? string.Empty : $" ({occupancy.ProcessName})")
                    : "PID Unknown";
                issues.Add(Issue(
                    "port-conflict",
                    $"Port {assignedPort} is occupied by an unrelated process ({occupant}).",
                    serviceName,
                    shared.InstanceId,
                    assignedPort,
                    "port"));
                return BlockedEntry(serviceName, $"Port occupied by {occupant}.", shared.InstanceId, assignedPort);
            }
        }

        return new SharedRuntimePlanEntry
        {
            ServiceName = serviceName,
            InstanceId = shared.InstanceId,
            ScopeDisplay = "Shared",
            Port = port,
            Action = SharedRuntimeAction.Start,
            ActionDisplay = "START",
            Reason = "Will start and receive Shared Runtime ownership.",
            OwnedAfterStart = true,
            OwnerDisplay = "Shared Runtime"
        };
    }

    private (int? Port, SharedRuntimeValidationIssue? Issue) ResolvePort(
        ServiceInstanceItem instance,
        ServiceInfoModel service)
    {
        var allocation = ports.GetByInstanceId(instance.InstanceId);
        var configured = allocation?.Port ?? service.HttpsPort ?? service.HttpPort ?? service.Port;

        if (configured is null)
        {
            if (service.Kind == ServiceKind.AngularWebApp && !string.IsNullOrWhiteSpace(service.LaunchUrl))
            {
                return (null, null);
            }

            return (null, Issue(
                "missing-port",
                $"No port is registered or configured for '{service.Name}'.",
                service.Name,
                instance.InstanceId,
                phase: "port"));
        }

        var other = ports.GetByPort(configured.Value);
        if (other is not null
            && !string.Equals(other.InstanceId, instance.InstanceId, StringComparison.OrdinalIgnoreCase))
        {
            return (configured, Issue(
                "port-registry-conflict",
                $"Port {configured.Value} is assigned to another ServiceInstance.",
                service.Name,
                instance.InstanceId,
                configured,
                "port"));
        }

        return (configured, null);
    }

    private SharedRuntimeValidationIssue? ValidateCanonicalSource(ServiceInfoModel service, string? canonicalSource)
    {
        if (string.IsNullOrWhiteSpace(service.ProjectPath)
            || (!Directory.Exists(service.ProjectPath) && !File.Exists(service.ProjectPath)))
        {
            return Issue(
                "missing-source",
                $"Source path for '{service.Name}' does not exist.",
                service.Name,
                phase: "source");
        }

        var full = Path.GetFullPath(service.ProjectPath);
        if (IsAgentWorktreePath(full))
        {
            return Issue(
                "non-canonical-source",
                $"'{service.Name}' source is under an agent worktree. Shared Runtime must use {canonicalSource ?? "the canonical AOVIAS repository"}.",
                service.Name,
                phase: "source");
        }

        if (!string.IsNullOrWhiteSpace(canonicalSource))
        {
            var canonical = Path.GetFullPath(canonicalSource);
            if (!full.StartsWith(canonical, StringComparison.OrdinalIgnoreCase))
            {
                return Issue(
                    "non-canonical-source",
                    $"'{service.Name}' is not under the canonical Shared Runtime source '{canonical}'.",
                    service.Name,
                    phase: "source");
            }
        }

        return null;
    }

    private static SharedRuntimeValidationIssue? ValidateHealthConfiguration(ServiceInfoModel service)
    {
        if (service.Kind == ServiceKind.AngularWebApp)
        {
            if (string.IsNullOrWhiteSpace(service.LaunchUrl)
                && service.HttpPort is null
                && service.Port is null)
            {
                return Issue(
                    "missing-health",
                    $"'{service.Name}' has no launch URL or port for health observation.",
                    service.Name,
                    phase: "health");
            }

            return null;
        }

        if (service.HttpsPort is null
            && service.HttpPort is null
            && service.Port is null
            && string.IsNullOrWhiteSpace(service.HttpsUrl)
            && string.IsNullOrWhiteSpace(service.HttpUrl)
            && string.IsNullOrWhiteSpace(service.LaunchUrl))
        {
            return Issue(
                "missing-health",
                $"'{service.Name}' has no health URL or port configuration.",
                service.Name,
                phase: "health");
        }

        return null;
    }

    private string? ResolveCanonicalSourcePath()
    {
        var main = workspaces.GetEnabledWorkspaces().FirstOrDefault(ws => ws.IsMain)
                   ?? workspaces.GetEnabledWorkspaces().FirstOrDefault(ws =>
                       string.Equals(ws.Id, "main", StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(main?.Path) ? null : Path.GetFullPath(main.Path);
    }

    private bool IsAgentWorktreePath(string fullPath)
    {
        foreach (var workspace in workspaces.GetEnabledWorkspaces())
        {
            if (workspace.IsMain || string.Equals(workspace.Id, "main", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (workspace.AgentOwner is WorkspaceAgentOwner.Cursor or WorkspaceAgentOwner.Claude
                && !string.IsNullOrWhiteSpace(workspace.Path))
            {
                var root = Path.GetFullPath(workspace.Path);
                if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return fullPath.Contains($"{Path.DirectorySeparatorChar}AOVIAS.Cursor{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
               || fullPath.Contains($"{Path.DirectorySeparatorChar}AOVIAS.Claude{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private void CaptureOwnership(RuntimeProfileItem profile)
    {
        lock (gate)
        {
            Session.OwnedServiceNames.Clear();
            Session.OwnedServiceNames.AddRange(profile.OwnedServiceNames);
        }
    }

    private static SharedRuntimeStatus MapProfileStatus(RuntimeProfileStatus status)
        => status switch
        {
            RuntimeProfileStatus.Running => SharedRuntimeStatus.Running,
            RuntimeProfileStatus.PartiallyRunning => SharedRuntimeStatus.Degraded,
            RuntimeProfileStatus.Starting => SharedRuntimeStatus.Starting,
            RuntimeProfileStatus.Stopping => SharedRuntimeStatus.Stopping,
            RuntimeProfileStatus.Failed => SharedRuntimeStatus.Failed,
            RuntimeProfileStatus.Invalid => SharedRuntimeStatus.Blocked,
            _ => SharedRuntimeStatus.Stopped
        };

    private static SharedRuntimePlanEntry BlockedEntry(string serviceName, string reason, string? instanceId = null, int? port = null)
        => new()
        {
            ServiceName = serviceName,
            InstanceId = instanceId,
            ScopeDisplay = "Shared",
            Port = port,
            Action = SharedRuntimeAction.Block,
            ActionDisplay = "BLOCK",
            Reason = reason,
            OwnedAfterStart = false,
            OwnerDisplay = "—"
        };

    private static SharedRuntimeValidationIssue Issue(
        string code,
        string message,
        string? service = null,
        string? instanceId = null,
        int? port = null,
        string? phase = null)
        => new()
        {
            Code = code,
            Message = message,
            ServiceName = service,
            InstanceId = instanceId,
            Port = port,
            Phase = phase
        };
}
