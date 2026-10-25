using System.IO;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.AgentSessions;
using ComplianceServiceManager.Models.PortRegistry;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.SharedRuntime;
using ComplianceServiceManager.Models.WorkspaceRuntime;
using ComplianceServiceManager.Models.Workspaces;
using ComplianceServiceManager.Services.AgentSessions;
using ComplianceServiceManager.Services.PortRegistry;
using ComplianceServiceManager.Services.RuntimeProfiles;
using ComplianceServiceManager.Services.ServiceInstances;
using ComplianceServiceManager.Services.Workspaces;

namespace ComplianceServiceManager.Services.WorkspaceRuntime;

/// <summary>
/// Workspace-scoped Plan → Validate → Start → Health → Own → Stop.
/// Reuses D1 identity, D2 profiles, V2.1 instances, V2.2 ports, V2.3 health probing.
/// Never stops Shared Runtime or another workspace. Never mutates Git.
/// </summary>
public sealed class WorkspaceRuntimeService : IWorkspaceRuntimeService
{
    private readonly IRuntimeProfileOrchestrator orchestrator;
    private readonly IServiceRuntimeHost runtimeHost;
    private readonly IServiceInstanceService instances;
    private readonly IPortRegistryService ports;
    private readonly IPortAvailabilityService availability;
    private readonly DevelopmentWorkspaceRegistry workspaces;
    private readonly IHealthProbe health;
    private readonly WorkspaceRuntimeProcessMap launches;
    private readonly IAgentSessionService? sessions;
    private readonly ILoggingService logger;
    private readonly object gate = new();
    private readonly Dictionary<string, WorkspaceRuntimeSession> sessionsByWorkspace = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorkspaceRuntimePlan> lastPlans = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SemaphoreSlim> startLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CancellationTokenSource> startupCts = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> starting = new(StringComparer.OrdinalIgnoreCase);
    private TimeSpan healthWaitTimeout = TimeSpan.FromSeconds(90);
    private TimeSpan healthPollInterval = TimeSpan.FromSeconds(2);

    public WorkspaceRuntimeService(
        IRuntimeProfileOrchestrator orchestrator,
        IServiceRuntimeHost runtimeHost,
        IServiceInstanceService instances,
        IPortRegistryService ports,
        IPortAvailabilityService availability,
        DevelopmentWorkspaceRegistry workspaces,
        IHealthProbe health,
        WorkspaceRuntimeProcessMap launches,
        ILoggingService logger,
        IAgentSessionService? sessions = null)
    {
        this.orchestrator = orchestrator;
        this.runtimeHost = runtimeHost;
        this.instances = instances;
        this.ports = ports;
        this.availability = availability;
        this.workspaces = workspaces;
        this.health = health;
        this.launches = launches;
        this.logger = logger;
        this.sessions = sessions;
    }

    public event Action? StateChanged;

    internal void ConfigureHealthWaitForTests(TimeSpan timeout, TimeSpan pollInterval)
    {
        healthWaitTimeout = timeout;
        healthPollInterval = pollInterval;
    }

    public WorkspaceRuntimeSession GetSession(string workspaceId)
    {
        lock (gate)
        {
            return GetOrCreateSession(workspaceId);
        }
    }

    public WorkspaceRuntimePlan? GetLastPlan(string workspaceId)
    {
        lock (gate)
        {
            return lastPlans.TryGetValue(workspaceId, out var plan) ? plan : null;
        }
    }

    public bool IsStarting(string workspaceId)
    {
        lock (gate)
        {
            return starting.Contains(workspaceId);
        }
    }

    public WorkspaceRuntimePlan Plan(
        RuntimeProfileItem profile,
        string workspaceId,
        DevelopmentWorkspaceItem? snapshot,
        IReadOnlyList<string>? requestedServices = null,
        string? agentSessionId = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var issues = new List<WorkspaceRuntimeValidationIssue>();
        var warnings = new List<string>();
        var entries = new List<WorkspaceRuntimePlanEntry>();

        lock (gate)
        {
            GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Validating;
        }

        var workspace = workspaces.GetEnabledWorkspaces()
            .FirstOrDefault(item => string.Equals(item.Id, workspaceId, StringComparison.OrdinalIgnoreCase));
        if (workspace is null || workspace.IsMain)
        {
            issues.Add(Issue("missing-workspace", "Workspace is missing, disabled, or is the integration main workspace.", phase: "workspace"));
            return FinishPlan(profile, workspaceId, "—", snapshot, agentSessionId, issues, warnings, entries, []);
        }

        if (workspace.AgentOwner is not (WorkspaceAgentOwner.Cursor or WorkspaceAgentOwner.Claude))
        {
            issues.Add(Issue("ownership-mismatch", $"Workspace '{workspace.Id}' is not an agent workspace.", phase: "workspace"));
        }

        if (!Directory.Exists(workspace.Path))
        {
            issues.Add(Issue("missing-workspace-path", $"Workspace path does not exist: {workspace.Path}", phase: "workspace"));
            return FinishPlan(profile, workspaceId, workspace.Path, snapshot, agentSessionId, issues, warnings, entries, []);
        }

        var branch = snapshot?.Branch;
        var prefix = WorkspaceSourceMapper.ExpectedBranchPrefix(workspace.Id);
        if (snapshot is not null)
        {
            if (snapshot.Availability == WorkspaceAvailability.Unavailable)
            {
                issues.Add(Issue("workspace-inaccessible", snapshot.StatusReason ?? "Workspace Git state is unavailable.", phase: "workspace"));
            }

            if (snapshot.Availability == WorkspaceAvailability.Detached)
            {
                issues.Add(Issue("branch-mismatch", "Workspace HEAD is detached. Workspace Runtime will not execute.", phase: "workspace"));
            }

            if (!string.IsNullOrWhiteSpace(prefix)
                && (string.IsNullOrWhiteSpace(branch) || !branch.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add(Issue(
                    "branch-mismatch",
                    $"Workspace branch '{branch ?? "(none)"}' does not match expected prefix '{prefix}'.",
                    phase: "workspace"));
            }
        }

        if (!string.IsNullOrWhiteSpace(agentSessionId) && sessions is not null)
        {
            var session = sessions.GetSessions().FirstOrDefault(item =>
                string.Equals(item.Id, agentSessionId, StringComparison.OrdinalIgnoreCase));
            if (session is null)
            {
                issues.Add(Issue("agent-session", "AgentSession was supplied but was not found.", phase: "session"));
            }
            else if (!string.Equals(session.WorkspaceId, workspace.Id, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(Issue(
                    "agent-session-mismatch",
                    $"AgentSession workspace '{session.WorkspaceId}' does not match runtime workspace '{workspace.Id}'.",
                    phase: "session"));
            }
            else if (session.Status == AgentSessionStatus.Stale)
            {
                warnings.Add("Associated AgentSession is stale. Runtime may proceed; refresh the session when convenient.");
            }
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
        var requested = (requestedServices ?? Array.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in requested.Where(item =>
                     !order.Contains(item, StringComparer.OrdinalIgnoreCase)))
        {
            issues.Add(Issue(
                "unknown-requested-service",
                $"Requested service '{name}' is not in the selected Runtime Profile.",
                name,
                phase: "service"));
        }

        var knownRoots = workspaces.GetEnabledWorkspaces().Select(item => item.Path).ToList();
        var dependsOn = profile.Services
            .Where(entry => entry.Enabled)
            .ToDictionary(
                entry => entry.ServiceName,
                entry => entry.DependsOn.Where(name => !string.IsNullOrWhiteSpace(name)).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var serviceName in order)
        {
            var isolate = requested.Contains(serviceName);
            entries.Add(PlanMember(
                profile,
                serviceName,
                workspace,
                isolate,
                dependsOn,
                requested,
                knownRoots,
                issues));
        }

        var canStart = issues.Count == 0
                       && entries.TrueForAll(entry => entry.Action != WorkspaceRuntimeAction.Blocked);
        return FinishPlan(profile, workspace.Id, workspace.Path, snapshot, agentSessionId, issues, warnings, entries, order, workspace.DisplayName, canStart);
    }

    public WorkspaceRuntimeValidationIssue? ValidateInstanceForWorkspaceRuntime(ServiceInstanceItem instance, string workspaceId)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (instance.ExecutionScope != ServiceExecutionScope.Workspace)
        {
            return Issue("shared-instance", "Shared ServiceInstance cannot be mutated by Workspace Runtime.", instance.ServiceName, instance.InstanceId, phase: "instance");
        }

        if (!string.Equals(instance.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase))
        {
            return Issue(
                "cross-workspace",
                $"Instance belongs to workspace '{instance.WorkspaceId}' and cannot be mutated by '{workspaceId}'.",
                instance.ServiceName,
                instance.InstanceId,
                phase: "instance");
        }

        return null;
    }

    public async Task<WorkspaceRuntimeExecutionResult> StartAsync(
        RuntimeProfileItem profile,
        string workspaceId,
        DevelopmentWorkspaceItem? snapshot,
        IReadOnlyList<string>? requestedServices = null,
        string? agentSessionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var startLock = GetStartLock(workspaceId);
        if (!await startLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return WorkspaceRuntimeExecutionResult.Block("Workspace Runtime startup already in progress for this workspace.");
        }

        CancellationTokenSource? linked = null;
        try
        {
            lock (gate)
            {
                starting.Add(workspaceId);
            }

            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lock (gate)
            {
                if (startupCts.TryGetValue(workspaceId, out var previous))
                {
                    previous.Dispose();
                }

                startupCts[workspaceId] = linked;
            }

            var plan = Plan(profile, workspaceId, snapshot, requestedServices, agentSessionId);
            if (!plan.CanStart)
            {
                var reason = plan.Issues.FirstOrDefault()?.Message ?? "Workspace Runtime plan is blocked. No process was started.";
                logger.Warning("WorkspaceRuntime", $"Start blocked for {workspaceId}: {reason}");
                StateChanged?.Invoke();
                return WorkspaceRuntimeExecutionResult.Block(reason, plan);
            }

            lock (gate)
            {
                var session = GetOrCreateSession(workspaceId);
                session.SessionId = Guid.NewGuid().ToString("N")[..12];
                session.StartedAt = DateTimeOffset.UtcNow;
                session.Status = SharedRuntimeStatus.Starting;
                session.ProfileId = profile.Id;
                session.AgentSessionId = agentSessionId;
                session.StartedHead = snapshot?.Head;
            }

            StateChanged?.Invoke();

            var startedThisAttempt = new List<StartedWorkspaceLaunch>();
            try
            {
                    foreach (var entry in plan.Entries.Where(item => item.Action == WorkspaceRuntimeAction.StartWorkspace))
                    {
                        linked.Token.ThrowIfCancellationRequested();
                        if (string.IsNullOrWhiteSpace(entry.InstanceId))
                        {
                            continue;
                        }

                        if (!launches.TryGetLaunch(entry.InstanceId, out var snapshotLaunch))
                        {
                            var leftover = await AbortStartedLaunchesAsync(startedThisAttempt, workspaceId).ConfigureAwait(false);
                            lock (gate)
                            {
                                GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Failed;
                            }

                            StateChanged?.Invoke();
                            return WorkspaceRuntimeExecutionResult.Fail(
                                FormatStartFailure($"Launch context missing for '{entry.ServiceName}'.", leftover, startedThisAttempt),
                                plan);
                        }

                        try
                        {
                            await runtimeHost.StartAsync(snapshotLaunch.Launch, linked.Token).ConfigureAwait(false);
                        }
                        finally
                        {
                            TrackStartedLaunch(startedThisAttempt, entry, snapshotLaunch.Launch, workspaceId);
                        }

                        if (snapshotLaunch.Launch.ProcessId is null)
                        {
                            var leftover = await AbortStartedLaunchesAsync(startedThisAttempt, workspaceId).ConfigureAwait(false);
                            lock (gate)
                            {
                                GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Failed;
                            }

                            StateChanged?.Invoke();
                            return WorkspaceRuntimeExecutionResult.Fail(
                                FormatStartFailure(
                                    $"'{entry.ServiceName}' did not start a process on the registered workspace port.",
                                    leftover,
                                    startedThisAttempt),
                                plan);
                        }

                        var healthy = await WaitForHealthyAsync(snapshotLaunch.Launch, linked.Token).ConfigureAwait(false);
                        if (!healthy)
                        {
                            var leftover = await AbortStartedLaunchesAsync(startedThisAttempt, workspaceId).ConfigureAwait(false);
                            lock (gate)
                            {
                                GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Failed;
                            }

                            StateChanged?.Invoke();
                            return WorkspaceRuntimeExecutionResult.Fail(
                                FormatStartFailure(
                                    $"'{entry.ServiceName}' started but did not become healthy on the workspace port.",
                                    leftover,
                                    startedThisAttempt),
                                plan);
                        }

                        if (!TryConfirmRegisteredPort(entry, snapshotLaunch.Launch, out var portMismatch))
                        {
                            var leftover = await AbortStartedLaunchesAsync(startedThisAttempt, workspaceId).ConfigureAwait(false);
                            lock (gate)
                            {
                                GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Failed;
                            }

                            StateChanged?.Invoke();
                            return WorkspaceRuntimeExecutionResult.Fail(
                                FormatStartFailure(portMismatch ?? "Registered workspace port was not bound.", leftover, startedThisAttempt),
                                plan);
                        }

                        launches.Upsert(
                            entry.InstanceId,
                            workspaceId,
                            entry.ServiceName,
                            snapshotLaunch.Launch,
                            owned: true,
                            startedByRuntime: true);
                        lock (gate)
                        {
                            GetOrCreateSession(workspaceId).OwnedServiceNames.Add(entry.ServiceName);
                        }
                    }
            }
                catch (OperationCanceledException)
                {
                    var leftover = await AbortStartedLaunchesAsync(startedThisAttempt, workspaceId).ConfigureAwait(false);
                    lock (gate)
                    {
                        GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Failed;
                    }

                    StateChanged?.Invoke();
                    return WorkspaceRuntimeExecutionResult.Fail(
                        FormatStartFailure(
                            "Workspace Runtime startup cancelled.",
                            leftover,
                            startedThisAttempt),
                        plan,
                        SharedRuntimeStatus.Failed);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    var leftover = await AbortStartedLaunchesAsync(startedThisAttempt, workspaceId).ConfigureAwait(false);
                    lock (gate)
                    {
                        GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Failed;
                    }

                    StateChanged?.Invoke();
                    logger.Error("WorkspaceRuntime", $"Start failed for {workspaceId}: {ex.Message}");
                    return WorkspaceRuntimeExecutionResult.Fail(
                        FormatStartFailure($"Workspace Runtime start failed: {ex.Message}", leftover, startedThisAttempt),
                        plan);
                }

            lock (gate)
            {
                GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Running;
            }

            StateChanged?.Invoke();
            logger.Info("WorkspaceRuntime", $"Started workspace runtime '{workspaceId}'.");
            return WorkspaceRuntimeExecutionResult.Ok(plan, SharedRuntimeStatus.Running);
        }
        finally
        {
            lock (gate)
            {
                starting.Remove(workspaceId);
                if (linked is not null
                    && startupCts.TryGetValue(workspaceId, out var current)
                    && ReferenceEquals(current, linked))
                {
                    startupCts.Remove(workspaceId);
                }
            }

            startLock.Release();
            StateChanged?.Invoke();
        }
    }

    public async Task<WorkspaceRuntimeExecutionResult> StopAsync(string workspaceId, CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            GetOrCreateSession(workspaceId).Status = SharedRuntimeStatus.Stopping;
        }

        StateChanged?.Invoke();
        var targets = launches.GetEntries()
            .Where(item =>
                string.Equals(item.Snapshot.WorkspaceId, workspaceId, StringComparison.OrdinalIgnoreCase)
                && (item.Snapshot.Owned || item.Snapshot.StartedByRuntime))
            .ToList();

        var leftover = false;
        foreach (var (instanceId, snapshot) in targets)
        {
            leftover |= await StopTrackedLaunchAsync(
                instanceId,
                workspaceId,
                snapshot.ServiceName,
                snapshot.Launch,
                snapshot.Port).ConfigureAwait(false);
        }

        if (leftover)
        {
            lock (gate)
            {
                var session = GetOrCreateSession(workspaceId);
                session.OwnedServiceNames.Clear();
                session.Status = SharedRuntimeStatus.Failed;
            }

            StateChanged?.Invoke();
            logger.Error("WorkspaceRuntime", $"Stop for '{workspaceId}' left a process running. Needs attention.");
            return WorkspaceRuntimeExecutionResult.Fail(
                "Stop did not terminate the workspace process. Needs attention — PID remains. Ownership was not claimed as healthy.",
                GetLastPlan(workspaceId),
                SharedRuntimeStatus.Failed);
        }

        launches.ClearWorkspace(workspaceId);

        lock (gate)
        {
            var session = GetOrCreateSession(workspaceId);
            session.OwnedServiceNames.Clear();
            session.Status = SharedRuntimeStatus.Stopped;
            session.StartedAt = null;
        }

        StateChanged?.Invoke();
        logger.Info("WorkspaceRuntime", $"Stopped owned services for workspace '{workspaceId}'.");
        return WorkspaceRuntimeExecutionResult.Ok(GetLastPlan(workspaceId), SharedRuntimeStatus.Stopped);
    }

    public void CancelStartup(string workspaceId)
    {
        lock (gate)
        {
            if (startupCts.TryGetValue(workspaceId, out var cts))
            {
                cts.Cancel();
            }
        }

        logger.Info("WorkspaceRuntime", $"Cancel requested for workspace '{workspaceId}'.");
    }

    private WorkspaceRuntimePlanEntry PlanMember(
        RuntimeProfileItem profile,
        string serviceName,
        ConfiguredWorkspace workspace,
        bool isolate,
        IReadOnlyDictionary<string, List<string>> dependsOn,
        HashSet<string> requested,
        IReadOnlyList<string> knownRoots,
        List<WorkspaceRuntimeValidationIssue> issues)
    {
        var template = runtimeHost.Services.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, serviceName, StringComparison.OrdinalIgnoreCase));
        if (template is null)
        {
            issues.Add(Issue("missing-service", $"Service '{serviceName}' is not in current discovery.", serviceName, phase: "service"));
            return BlockedEntry(serviceName, "Service definition is missing.");
        }

        var shared = instances.GetInstancesForService(serviceName)
            .FirstOrDefault(item => item.ExecutionScope == ServiceExecutionScope.Shared && item.IsConfigured);

        if (!isolate)
        {
            return PlanReuseShared(serviceName, template, shared, dependsOn, requested, issues);
        }

        var mapped = WorkspaceSourceMapper.ResolveUnderWorkspace(template.ProjectPath, workspace.Path, knownRoots);
        if (string.IsNullOrWhiteSpace(mapped) || (!File.Exists(mapped) && !Directory.Exists(mapped)))
        {
            issues.Add(Issue(
                "missing-workspace-source",
                $"'{serviceName}' source does not exist under workspace '{workspace.Path}'.",
                serviceName,
                phase: "source"));
            return BlockedEntry(serviceName, "Workspace source is missing.");
        }

        if (WorkspaceSourceMapper.IsUnder(mapped, ResolveMainPath() ?? string.Empty)
            && !string.Equals(workspace.Id, "main", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(Issue(
                "canonical-source-conflict",
                $"'{serviceName}' resolved to the canonical main path. Workspace Runtime must use {workspace.Path}.",
                serviceName,
                phase: "source"));
            return BlockedEntry(serviceName, "Source is not under the target workspace.");
        }

        foreach (var other in workspaces.GetEnabledWorkspaces().Where(item =>
                     !item.IsMain && !string.Equals(item.Id, workspace.Id, StringComparison.OrdinalIgnoreCase)))
        {
            if (WorkspaceSourceMapper.IsUnder(mapped, other.Path))
            {
                issues.Add(Issue(
                    "cross-workspace-source",
                    $"'{serviceName}' source is under workspace '{other.Id}', not '{workspace.Id}'.",
                    serviceName,
                    phase: "source"));
                return BlockedEntry(serviceName, "Source belongs to another workspace.");
            }
        }

        var instance = instances.EnsureWorkspaceInstance(serviceName, serviceName, workspace.Id, runtimeProfileId: profile.Id);
        var instanceIssue = ValidateInstanceForWorkspaceRuntime(instance, workspace.Id);
        if (instanceIssue is not null)
        {
            issues.Add(instanceIssue);
            return BlockedEntry(serviceName, instanceIssue.Message, instance.InstanceId);
        }

        var clone = CloneForWorkspace(template, mapped);
        var (port, portIssue) = ResolveWorkspacePort(instance, clone);
        if (portIssue is not null)
        {
            issues.Add(portIssue);
            return BlockedEntry(serviceName, portIssue.Message, instance.InstanceId, port);
        }

        ApplyPortToClone(clone, port);
        var hadLaunch = launches.TryGetLaunch(instance.InstanceId, out var existing);
        if (!hadLaunch)
        {
            launches.Upsert(instance.InstanceId, workspace.Id, serviceName, clone, owned: false);
            existing = new WorkspaceLaunchSnapshot(workspace.Id, serviceName, clone, owned: false);
        }

        if (existing.Launch.Status == ServiceStatus.Running)
        {
            if (existing.Launch.Health is HealthStatus.Healthy or HealthStatus.Starting or HealthStatus.Unknown)
            {
                launches.Upsert(
                    instance.InstanceId,
                    workspace.Id,
                    serviceName,
                    existing.Launch,
                    owned: existing.Owned,
                    startedByRuntime: existing.StartedByRuntime);
                return new WorkspaceRuntimePlanEntry
                {
                    ServiceName = serviceName,
                    InstanceId = instance.InstanceId,
                    ScopeDisplay = ServiceInstanceItem.FormatWorkspaceLabel(workspace.Id),
                    Port = port,
                    Action = WorkspaceRuntimeAction.ReuseWorkspace,
                    ActionDisplay = "REUSE WORKSPACE",
                    Reason = existing.Owned
                        ? "Already running in this workspace — ownership retained."
                        : "Already running in this workspace — not claimed.",
                    OwnedAfterStart = existing.Owned,
                    OwnerDisplay = existing.Owned
                        ? ServiceInstanceItem.FormatOwnershipDisplay(ServiceInstanceOwnership.Workspace, workspace.Id)
                        : "External",
                    SourcePath = existing.Launch.ProjectPath
                };
            }

            issues.Add(Issue(
                "unhealthy-running",
                $"'{serviceName}' is already running in this workspace but is not healthy. Workspace Runtime will not blindly restart it.",
                serviceName,
                instance.InstanceId,
                port,
                "reuse"));
            return BlockedEntry(serviceName, "Already running but unhealthy.", instance.InstanceId, port);
        }

        if (port is int assignedPort)
        {
            var occupancy = availability.Observe(assignedPort);
            if (occupancy.IsListening)
            {
                var expectedPid = existing.Launch.ProcessId;
                if (expectedPid is int pid && occupancy.ProcessId is int actual && pid == actual)
                {
                    return new WorkspaceRuntimePlanEntry
                    {
                        ServiceName = serviceName,
                        InstanceId = instance.InstanceId,
                        ScopeDisplay = ServiceInstanceItem.FormatWorkspaceLabel(workspace.Id),
                        Port = assignedPort,
                        Action = WorkspaceRuntimeAction.ReuseWorkspace,
                        ActionDisplay = "REUSE WORKSPACE",
                        Reason = "Port occupied by expected workspace process.",
                        OwnedAfterStart = existing.Owned,
                        OwnerDisplay = existing.Owned
                            ? ServiceInstanceItem.FormatOwnershipDisplay(ServiceInstanceOwnership.Workspace, workspace.Id)
                            : "External",
                        SourcePath = clone.ProjectPath
                    };
                }

                var occupant = occupancy.ProcessId is int occ
                    ? $"PID {occ}" + (string.IsNullOrWhiteSpace(occupancy.ProcessName) ? string.Empty : $" ({occupancy.ProcessName})")
                    : "PID Unknown";
                issues.Add(Issue(
                    "port-conflict",
                    $"Workspace port {assignedPort} for '{serviceName}' is occupied by {occupant}.",
                    serviceName,
                    instance.InstanceId,
                    assignedPort,
                    "port"));
                return BlockedEntry(serviceName, $"Port occupied by {occupant}.", instance.InstanceId, assignedPort);
            }
        }

        launches.Upsert(instance.InstanceId, workspace.Id, serviceName, clone, owned: false);
        return new WorkspaceRuntimePlanEntry
        {
            ServiceName = serviceName,
            InstanceId = instance.InstanceId,
            ScopeDisplay = ServiceInstanceItem.FormatWorkspaceLabel(workspace.Id),
            Port = port,
            Action = WorkspaceRuntimeAction.StartWorkspace,
            ActionDisplay = "START WORKSPACE",
            Reason = $"Will start from {workspace.Path}.",
            OwnedAfterStart = true,
            OwnerDisplay = ServiceInstanceItem.FormatOwnershipDisplay(ServiceInstanceOwnership.Workspace, workspace.Id),
            SourcePath = clone.ProjectPath
        };
    }

    private WorkspaceRuntimePlanEntry PlanReuseShared(
        string serviceName,
        ServiceInfoModel template,
        ServiceInstanceItem? shared,
        IReadOnlyDictionary<string, List<string>> dependsOn,
        HashSet<string> requested,
        List<WorkspaceRuntimeValidationIssue> issues)
    {
        if (shared is null)
        {
            issues.Add(Issue("missing-shared-instance", $"Shared ServiceInstance is missing for dependency '{serviceName}'.", serviceName, phase: "dependency"));
            return BlockedEntry(serviceName, "Shared dependency instance is missing.");
        }

        if (template.Status == ServiceStatus.Running
            && template.Health is HealthStatus.Healthy or HealthStatus.Starting or HealthStatus.Unknown)
        {
            return new WorkspaceRuntimePlanEntry
            {
                ServiceName = serviceName,
                InstanceId = shared.InstanceId,
                ScopeDisplay = "Shared",
                Port = template.HttpsPort ?? template.HttpPort ?? template.Port,
                Action = WorkspaceRuntimeAction.ReuseShared,
                ActionDisplay = "REUSE SHARED",
                Reason = "Healthy shared dependency — not claimed by Workspace Runtime.",
                OwnedAfterStart = false,
                OwnerDisplay = "Shared Runtime",
                SourcePath = template.ProjectPath
            };
        }

        var neededByIsolated = dependsOn.Any(pair =>
            requested.Contains(pair.Key)
            && pair.Value.Any(dep => string.Equals(dep, serviceName, StringComparison.OrdinalIgnoreCase)));
        if (neededByIsolated)
        {
            issues.Add(Issue(
                "unhealthy-shared-dependency",
                $"Isolated service(s) require healthy shared '{serviceName}'. It is not healthy and will not be auto-duplicated.",
                serviceName,
                shared.InstanceId,
                phase: "dependency"));
            return BlockedEntry(serviceName, "Required shared dependency is not healthy.", shared.InstanceId);
        }

        return new WorkspaceRuntimePlanEntry
        {
            ServiceName = serviceName,
            InstanceId = shared.InstanceId,
            ScopeDisplay = "Shared",
            Port = template.HttpsPort ?? template.HttpPort ?? template.Port,
            Action = WorkspaceRuntimeAction.ReuseShared,
            ActionDisplay = "REUSE SHARED",
            Reason = "Not selected for isolation — leave on Shared Runtime.",
            OwnedAfterStart = false,
            OwnerDisplay = "Shared Runtime",
            SourcePath = template.ProjectPath
        };
    }

    private (int? Port, WorkspaceRuntimeValidationIssue? Issue) ResolveWorkspacePort(
        ServiceInstanceItem instance,
        ServiceInfoModel clone)
    {
        var allocation = ports.GetByInstanceId(instance.InstanceId);
        if (allocation is null)
        {
            return (null, Issue(
                "missing-port",
                $"No workspace port is registered for '{instance.ServiceName}'. Assign a Port Registry port to the workspace instance before start.",
                instance.ServiceName,
                instance.InstanceId,
                phase: "port"));
        }

        var other = ports.GetByPort(allocation.Port);
        if (other is not null
            && !string.Equals(other.InstanceId, instance.InstanceId, StringComparison.OrdinalIgnoreCase))
        {
            return (allocation.Port, Issue(
                "port-registry-conflict",
                $"Port {allocation.Port} is assigned to another ServiceInstance.",
                instance.ServiceName,
                instance.InstanceId,
                allocation.Port,
                "port"));
        }

        return (allocation.Port, null);
    }

    private static void ApplyPortToClone(ServiceInfoModel clone, int? port)
    {
        if (port is not int assigned)
        {
            return;
        }

        if (clone.Kind == ServiceKind.AngularWebApp)
        {
            clone.HttpPort = assigned;
            clone.Port = assigned;
            clone.LaunchUrl = $"http://localhost:{assigned}";
            clone.HttpUrl = $"http://localhost:{assigned}";
        }
        else
        {
            clone.HttpsPort = assigned;
            clone.Port = assigned;
            clone.HttpsUrl = $"https://localhost:{assigned}";
            clone.LaunchUrl = clone.HttpsUrl;
        }

        // LaunchSettings.json in the workspace still carries the discovered/shared applicationUrl.
        // ASPNETCORE_URLS must be set on the process so the workspace port actually binds.
        var urls = string.Join(';', new[] { clone.HttpsUrl, clone.HttpUrl }.Where(url => !string.IsNullOrWhiteSpace(url)));
        if (string.IsNullOrWhiteSpace(urls))
        {
            return;
        }

        var environment = clone.LaunchEnvironmentVariables is Dictionary<string, string> existing
            ? existing
            : new Dictionary<string, string>(clone.LaunchEnvironmentVariables, StringComparer.OrdinalIgnoreCase);
        environment["ASPNETCORE_URLS"] = urls;
        clone.LaunchEnvironmentVariables = environment;
    }

    private static ServiceInfoModel CloneForWorkspace(ServiceInfoModel template, string mappedProjectPath)
    {
        var mappedWorking = Path.GetDirectoryName(mappedProjectPath);

        return new ServiceInfoModel
        {
            Name = template.Name,
            Kind = template.Kind,
            ProjectPath = mappedProjectPath,
            WorkingDirectory = mappedWorking ?? mappedProjectPath,
            LaunchProfileName = null,
            SuppressLaunchSettings = true,
            EnvironmentName = template.EnvironmentName,
            Environment = template.Environment,
            LaunchEnvironmentVariables = template.LaunchEnvironmentVariables.Count == 0
                ? new Dictionary<string, string>()
                : new Dictionary<string, string>(template.LaunchEnvironmentVariables),
            Status = ServiceStatus.Stopped,
            Health = HealthStatus.Unknown,
            LastAction = "Workspace launch prepared"
        };
    }

    private async Task<bool> WaitForHealthyAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + healthWaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HealthStatus status;
            try
            {
                status = await health.CheckAsync(service, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                status = HealthStatus.Unreachable;
            }

            service.Health = status;
            if (status == HealthStatus.Healthy)
            {
                return true;
            }

            await Task.Delay(healthPollInterval, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private void TrackStartedLaunch(
        List<StartedWorkspaceLaunch> started,
        WorkspaceRuntimePlanEntry entry,
        ServiceInfoModel launch,
        string workspaceId)
    {
        if (string.IsNullOrWhiteSpace(entry.InstanceId) || launch.ProcessId is null)
        {
            return;
        }

        if (started.Exists(item => string.Equals(item.InstanceId, entry.InstanceId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        launches.Upsert(
            entry.InstanceId,
            workspaceId,
            entry.ServiceName,
            launch,
            owned: false,
            startedByRuntime: true);
        started.Add(new StartedWorkspaceLaunch(entry.InstanceId, entry.ServiceName, launch, entry.Port));
    }

    private async Task<bool> AbortStartedLaunchesAsync(
        IReadOnlyList<StartedWorkspaceLaunch> started,
        string workspaceId)
    {
        var leftover = false;
        foreach (var item in started)
        {
            leftover |= await StopTrackedLaunchAsync(
                item.InstanceId,
                workspaceId,
                item.ServiceName,
                item.Launch,
                item.RegisteredPort).ConfigureAwait(false);
            lock (gate)
            {
                GetOrCreateSession(workspaceId).OwnedServiceNames.Remove(item.ServiceName);
            }
        }

        return leftover;
    }

    private async Task<bool> StopTrackedLaunchAsync(
        string instanceId,
        string workspaceId,
        string serviceName,
        ServiceInfoModel launch,
        int? registeredPort)
    {
        var pid = launch.ProcessId;
        try
        {
            await runtimeHost.StopAsync(launch, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Warning("WorkspaceRuntime", $"Cleanup stop failed for '{serviceName}' PID {pid}: {ex.Message}");
        }

        if (!LaunchRemains(launch, pid, registeredPort))
        {
            launches.Remove(instanceId);
            return false;
        }

        launch.Status = ServiceStatus.Error;
        launch.Health = HealthStatus.Unreachable;
        launch.ProcessId = pid ?? launch.ProcessId;
        launch.LastAction =
            $"Needs attention — process PID {launch.ProcessId} remains running after Workspace Runtime cleanup. Ownership was not claimed.";
        launches.Upsert(instanceId, workspaceId, serviceName, launch, owned: false, startedByRuntime: true);
        logger.Error("WorkspaceRuntime", launch.LastAction);
        return true;
    }

    private bool LaunchRemains(ServiceInfoModel launch, int? startedPid, int? registeredPort)
    {
        if (launch.Process is { HasExited: false })
        {
            return true;
        }

        if (launch.Status is ServiceStatus.Running or ServiceStatus.Starting)
        {
            return true;
        }

        if (registeredPort is int port && startedPid is int pid)
        {
            var occupancy = availability.Observe(port);
            if (occupancy.IsListening && occupancy.ProcessId == pid)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryConfirmRegisteredPort(
        WorkspaceRuntimePlanEntry entry,
        ServiceInfoModel launch,
        out string? mismatch)
    {
        var instancePort = entry.Port;
        var launchPort = launch.HttpsPort ?? launch.HttpPort ?? launch.Port;
        if (instancePort != launchPort)
        {
            mismatch = $"ServiceInstance port {instancePort} does not match launch/health port {launchPort}.";
            return false;
        }

        if (instancePort is not int registered)
        {
            mismatch = $"'{entry.ServiceName}' has no registered workspace port.";
            return false;
        }

        var occupancy = availability.Observe(registered);
        if (!occupancy.IsListening)
        {
            mismatch =
                $"Registered workspace port {registered} is not listening after start (process bound a different port).";
            return false;
        }

        if (occupancy.ProcessId is int observed
            && launch.ProcessId is int startedPid
            && observed != startedPid)
        {
            mismatch = $"Port {registered} is owned by PID {observed}, not the started process PID {startedPid}.";
            return false;
        }

        mismatch = null;
        return true;
    }

    private static string FormatStartFailure(
        string reason,
        bool leftover,
        IReadOnlyList<StartedWorkspaceLaunch> started)
    {
        if (!leftover)
        {
            return $"{reason} Process was terminated. No orphan remains. Ownership was not claimed.";
        }

        var pid = started.Select(item => item.Launch.ProcessId).FirstOrDefault(id => id is not null);
        return $"{reason} Needs attention — process PID {pid} remains running. Ownership was not claimed.";
    }

    private sealed record StartedWorkspaceLaunch(
        string InstanceId,
        string ServiceName,
        ServiceInfoModel Launch,
        int? RegisteredPort);

    private WorkspaceRuntimePlan FinishPlan(
        RuntimeProfileItem profile,
        string workspaceId,
        string sourceRoot,
        DevelopmentWorkspaceItem? snapshot,
        string? agentSessionId,
        List<WorkspaceRuntimeValidationIssue> issues,
        List<string> warnings,
        List<WorkspaceRuntimePlanEntry> entries,
        IReadOnlyList<string> order,
        string? displayName = null,
        bool? canStartOverride = null)
    {
        var canStart = canStartOverride ?? (issues.Count == 0 && entries.TrueForAll(e => e.Action != WorkspaceRuntimeAction.Blocked));
        var plan = new WorkspaceRuntimePlan
        {
            WorkspaceId = workspaceId,
            WorkspaceDisplay = displayName ?? snapshot?.DisplayName ?? workspaceId,
            Branch = snapshot?.Branch,
            ProfileId = profile.Id,
            ProfileName = profile.Name,
            SourceRoot = sourceRoot,
            AgentSessionId = agentSessionId,
            CanStart = canStart,
            StartupOrder = order,
            Entries = entries,
            Issues = issues,
            Warnings = warnings
        };

        lock (gate)
        {
            lastPlans[workspaceId] = plan;
            GetOrCreateSession(workspaceId).Status = canStart ? SharedRuntimeStatus.Planned : SharedRuntimeStatus.Blocked;
            GetOrCreateSession(workspaceId).ProfileId = profile.Id;
            GetOrCreateSession(workspaceId).AgentSessionId = agentSessionId;
        }

        logger.Info("WorkspaceRuntime", $"Plan for {workspaceId}/{profile.Name}: {plan.Summary}");
        StateChanged?.Invoke();
        return plan;
    }

    private string? ResolveMainPath()
        => workspaces.GetEnabledWorkspaces().FirstOrDefault(item => item.IsMain)?.Path;

    private WorkspaceRuntimeSession GetOrCreateSession(string workspaceId)
    {
        if (!sessionsByWorkspace.TryGetValue(workspaceId, out var session))
        {
            session = new WorkspaceRuntimeSession { WorkspaceId = workspaceId };
            sessionsByWorkspace[workspaceId] = session;
        }

        return session;
    }

    private SemaphoreSlim GetStartLock(string workspaceId)
    {
        lock (gate)
        {
            if (!startLocks.TryGetValue(workspaceId, out var item))
            {
                item = new SemaphoreSlim(1, 1);
                startLocks[workspaceId] = item;
            }

            return item;
        }
    }

    private static WorkspaceRuntimePlanEntry BlockedEntry(string serviceName, string reason, string? instanceId = null, int? port = null)
        => new()
        {
            ServiceName = serviceName,
            InstanceId = instanceId,
            Action = WorkspaceRuntimeAction.Blocked,
            ActionDisplay = "BLOCK",
            Reason = reason,
            Port = port,
            OwnerDisplay = "—"
        };

    private static WorkspaceRuntimeValidationIssue Issue(
        string code,
        string message,
        string? serviceName = null,
        string? instanceId = null,
        int? port = null,
        string? phase = null)
        => new()
        {
            Code = code,
            Message = message,
            ServiceName = serviceName,
            InstanceId = instanceId,
            Port = port,
            Phase = phase
        };
}
