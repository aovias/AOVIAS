using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Planning;
using Aovias.Deployment.Contracts.Reconciliation;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M2.6 D12 regression suite: drift detection, reconciliation planning, repair policy
/// evaluation, automatic repair, approval-required repair, scheduler overlap avoidance, event
/// generation, history persistence, and idempotent reconciliation.
/// </summary>
public sealed class ReconciliationEngineTests
{
    private static DeploymentPlan BuildApprovedPlan(string serviceId, int port)
    {
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = serviceId,
            Version = "1.0.0",
            ImageReference = $"aovias/{serviceId}:1.0.0",
            ContainerName = serviceId,
            EntryPoint = "App.dll",
            HealthEndpoint = "/health",
            Ports = port > 0 ? [port] : []
        });

        var contract = new InfrastructureContract
        {
            ServiceId = serviceId,
            SchemaVersion = ContractSchemaVersions.Current,
            Requirements = []
        };

        var admission = new AdmissionResult { Decision = AdmissionDecision.Approved, CorrelationId = "corr-recon-1" };

        var plan = new DeploymentPlanBuilder().CreatePlan(new DeploymentPlanRequest
        {
            Bundle = bundle,
            Contract = contract,
            Admission = admission,
            EnvironmentId = "local",
            AvailableCapabilities = [],
            GeneratedUtc = DateTimeOffset.UtcNow
        });

        return plan;
    }

    private static DesiredState BuildDesiredState(string serviceId, string containerName, string image)
        => new()
        {
            EnvironmentId = "local",
            ServiceId = serviceId,
            PlanId = "plan-1",
            PlanContentHash = "hash-1",
            GeneratedUtc = DateTimeOffset.UtcNow,
            Services = [new DesiredServiceState { ServiceId = serviceId, Required = true }],
            Containers = [new DesiredContainerState { ServiceId = serviceId, ContainerName = containerName, ImageReference = image, RestartPolicy = "unless-stopped" }],
            Networks = [new DesiredNetworkState { NetworkName = "aovias-net", External = true }],
            FilesystemPaths =
            [
                new DesiredFilesystemPathState { Path = "/opt/aovias/data", Permission = "775", Owner = "1654", IsSensitive = false },
                new DesiredFilesystemPathState { Path = "/opt/aovias/certs", Permission = "700", Owner = "1654", IsSensitive = true }
            ],
            Certificates = [new DesiredCertificateState { Id = "signing-cert", Required = true, MountPath = "/opt/aovias/certs", FileName = "signing.pfx" }]
        };

    // ---- D1: Desired State ----

    [Fact]
    public void DesiredStateProvider_BuildsFromPlanContractManifest_Deterministically()
    {
        var plan = BuildApprovedPlan("identity-service", 5062);
        var contract = new InfrastructureContract { ServiceId = "identity-service", SchemaVersion = ContractSchemaVersions.Current };
        var manifest = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "identity-service",
            DisplayName = "identity-service",
            Version = "1.0.0",
            ImageReference = "aovias/identity-service:1.0.0",
            ContainerName = "identity-service",
            EntryPoint = "App.dll",
            HealthEndpoint = "/health",
            Ports = [5062]
        }).Manifest;

        var provider = new DesiredStateProvider();
        var first = provider.BuildDesiredState(plan, contract, manifest, DateTimeOffset.UtcNow);
        var second = provider.BuildDesiredState(plan, contract, manifest, DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.Equal("identity-service", first.ServiceId);
        Assert.Single(first.Containers);
        Assert.Equal("aovias/identity-service:1.0.0", first.Containers[0].ImageReference);
        Assert.Equal(first.PlanContentHash, second.PlanContentHash);
        Assert.Equal(first.Containers[0].ContainerName, second.Containers[0].ContainerName);
    }

    // ---- D3/D4: Drift Detection + Classification ----

    [Fact]
    public void DriftDetection_MissingContainer_IsRecoverableAndAutoRepairable()
    {
        var desired = BuildDesiredState("gateway", "gateway", "aovias/gateway:1.0.0");
        var observed = new ObservedState
        {
            EnvironmentId = "local",
            GeneratedUtc = DateTimeOffset.UtcNow,
            Services = [new ObservedServiceState { ServiceId = "gateway", Running = false, DiscoveryStatus = DiscoveryStatus.Observed }],
            Containers = [new ObservedContainerState { ContainerName = "gateway", Exists = false, DiscoveryStatus = DiscoveryStatus.Observed }],
            Networks = [new ObservedNetworkState { NetworkName = "aovias-net", Exists = true, DiscoveryStatus = DiscoveryStatus.Observed }],
            FilesystemPaths =
            [
                new ObservedFilesystemPathState { Path = "/opt/aovias/data", Exists = true, Permission = "775", DiscoveryStatus = DiscoveryStatus.Observed },
                new ObservedFilesystemPathState { Path = "/opt/aovias/certs", Exists = true, Permission = "700", DiscoveryStatus = DiscoveryStatus.Observed }
            ],
            Certificates = [new ObservedCertificateState { Id = "signing-cert", Exists = true, DiscoveryStatus = DiscoveryStatus.Observed }]
        };

        var report = new DriftDetectionEngine().DetectDrift(desired, observed, "corr-1");

        Assert.True(report.HasDrift);
        var finding = Assert.Single(report.Findings, f => f.ResourceType == ReconciliationResourceTypes.Container);
        Assert.Equal(DriftCategory.MissingResource, finding.Category);
        Assert.Equal(DriftSeverity.Recoverable, finding.Severity);
    }

    [Fact]
    public void DriftDetection_VersionMismatch_IsCritical()
    {
        var desired = BuildDesiredState("gateway", "gateway", "aovias/gateway:2.0.0");
        var observed = BaselineObserved("gateway", imageReference: "aovias/gateway:1.0.0");

        var report = new DriftDetectionEngine().DetectDrift(desired, observed, "corr-2");

        var finding = Assert.Single(report.Findings, f => f.Category == DriftCategory.VersionMismatch);
        Assert.Equal(DriftSeverity.Critical, finding.Severity);
        Assert.Equal(ReconciliationResourceTypes.PackageVersion, finding.ResourceType);
    }

    [Fact]
    public void DriftDetection_MissingCertificate_IsCritical()
    {
        var desired = BuildDesiredState("gateway", "gateway", "aovias/gateway:1.0.0");
        var observed = BaselineObserved("gateway", certificateExists: false);

        var report = new DriftDetectionEngine().DetectDrift(desired, observed, "corr-3");

        var finding = Assert.Single(report.Findings, f => f.Category == DriftCategory.MissingCertificate);
        Assert.Equal(DriftSeverity.Critical, finding.Severity);
    }

    [Fact]
    public void DriftDetection_PermissionDriftOnSensitivePath_IsSecuritySeverity()
    {
        var desired = BuildDesiredState("gateway", "gateway", "aovias/gateway:1.0.0");
        var observed = BaselineObserved("gateway", sensitivePathPermission: "777");

        var report = new DriftDetectionEngine().DetectDrift(desired, observed, "corr-4");

        var finding = Assert.Single(report.Findings, f => f.Category == DriftCategory.PermissionDrift);
        Assert.Equal(DriftSeverity.Security, finding.Severity);
        Assert.Equal(ReconciliationResourceTypes.SensitivePath, finding.ResourceType);
    }

    [Fact]
    public void DriftDetection_NoDrift_WhenObservedMatchesDesired()
    {
        var desired = BuildDesiredState("gateway", "gateway", "aovias/gateway:1.0.0");
        var observed = BaselineObserved("gateway");

        var report = new DriftDetectionEngine().DetectDrift(desired, observed, "corr-5");

        Assert.False(report.HasDrift);
    }

    private static ObservedState BaselineObserved(
        string serviceId,
        string imageReference = "aovias/gateway:1.0.0",
        bool certificateExists = true,
        string sensitivePathPermission = "700")
        => new()
        {
            EnvironmentId = "local",
            GeneratedUtc = DateTimeOffset.UtcNow,
            Services = [new ObservedServiceState { ServiceId = serviceId, Running = true, DiscoveryStatus = DiscoveryStatus.Observed }],
            Containers = [new ObservedContainerState { ContainerName = serviceId, Exists = true, State = "running", Health = "healthy", ImageReference = imageReference, DiscoveryStatus = DiscoveryStatus.Observed }],
            Networks = [new ObservedNetworkState { NetworkName = "aovias-net", Exists = true, DiscoveryStatus = DiscoveryStatus.Observed }],
            FilesystemPaths =
            [
                new ObservedFilesystemPathState { Path = "/opt/aovias/data", Exists = true, Permission = "775", DiscoveryStatus = DiscoveryStatus.Observed },
                new ObservedFilesystemPathState { Path = "/opt/aovias/certs", Exists = true, Permission = sensitivePathPermission, DiscoveryStatus = DiscoveryStatus.Observed }
            ],
            Certificates = [new ObservedCertificateState { Id = "signing-cert", Exists = certificateExists, DiscoveryStatus = DiscoveryStatus.Observed }]
        };

    // ---- D5: Reconciliation Planner ----

    [Fact]
    public void ReconciliationPlanner_MapsFindingsToRepairTasks_InAscendingRiskOrder()
    {
        var report = new DriftReport
        {
            EnvironmentId = "local",
            ServiceId = "gateway",
            CorrelationId = "corr-6",
            GeneratedUtc = DateTimeOffset.UtcNow,
            Findings =
            [
                Finding(DriftCategory.MissingCertificate, ReconciliationResourceTypes.Certificate, DriftSeverity.Critical, "signing-cert"),
                Finding(DriftCategory.MissingResource, ReconciliationResourceTypes.Container, DriftSeverity.Recoverable, "gateway")
            ]
        };

        var plan = new ReconciliationPlanner().Plan(report);

        Assert.Equal(2, plan.RepairTasks.Count);
        Assert.Equal(RepairActionType.RestartContainer, plan.RepairTasks[0].ActionType); // Low risk first
        Assert.Equal(RepairActionType.CertificateReplacement, plan.RepairTasks[1].ActionType);
        Assert.True(plan.RequiresOperatorApproval);
    }

    private static DriftFinding Finding(DriftCategory category, string resourceType, DriftSeverity severity, string resourceId)
        => new()
        {
            FindingId = $"{category}:{resourceType}:{resourceId}",
            Category = category,
            Severity = severity,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Expected = "expected",
            Actual = "actual",
            Description = "test finding"
        };

    // ---- D7: Repair Policy ----

    [Fact]
    public void RepairPolicyEvaluator_AlwaysRepair_Approves()
    {
        var policySet = new RepairPolicySet
        {
            Environment = EnvironmentClass.Demo,
            Rules = [new RepairPolicyRule { ActionType = RepairActionType.RestartContainer, PolicyType = RepairPolicyType.AlwaysRepair }]
        };
        var task = BuildTask(RepairActionType.RestartContainer, requiresApprovalByDefault: false);
        var finding = Finding(DriftCategory.UnhealthyService, ReconciliationResourceTypes.Container, DriftSeverity.Recoverable, "gateway");

        var decision = new RepairPolicyEvaluator().Evaluate(task, finding, policySet, DateTimeOffset.UtcNow);

        Assert.Equal(PolicyDecision.Approve, decision);
    }

    [Fact]
    public void RepairPolicyEvaluator_MatrixApprovalRequired_CannotBeDowngradedByAlwaysRepairPolicy()
    {
        var policySet = new RepairPolicySet
        {
            Environment = EnvironmentClass.Demo,
            // Even if an operator misconfigures policy to always-repair certificate replacement...
            Rules = [new RepairPolicyRule { ActionType = RepairActionType.CertificateReplacement, PolicyType = RepairPolicyType.AlwaysRepair }]
        };
        var task = BuildTask(RepairActionType.CertificateReplacement, requiresApprovalByDefault: true);
        var finding = Finding(DriftCategory.MissingCertificate, ReconciliationResourceTypes.Certificate, DriftSeverity.Critical, "signing-cert");

        var decision = new RepairPolicyEvaluator().Evaluate(task, finding, policySet, DateTimeOffset.UtcNow);

        // ... the matrix's fixed opinion always wins (Final Engineering Rule: unsafe repairs
        // always require explicit operator approval).
        Assert.Equal(PolicyDecision.RequireOperatorApproval, decision);
    }

    [Fact]
    public void RepairPolicyEvaluator_UnconfiguredDefault_RequiresOperatorApproval()
    {
        var policySet = new RepairPolicySet { Environment = EnvironmentClass.Production, Rules = [] };
        var task = BuildTask(RepairActionType.ReloadConfiguration, requiresApprovalByDefault: null);
        var finding = Finding(DriftCategory.ConfigurationDrift, ReconciliationResourceTypes.ConfigurationKey, DriftSeverity.Warning, "Feature:Flag");

        var decision = new RepairPolicyEvaluator().Evaluate(task, finding, policySet, DateTimeOffset.UtcNow);

        Assert.Equal(PolicyDecision.RequireOperatorApproval, decision);
    }

    private static RepairTask BuildTask(RepairActionType actionType, bool? requiresApprovalByDefault)
        => new()
        {
            TaskId = $"repair:{actionType}",
            FindingId = $"finding:{actionType}",
            ActionType = actionType,
            ResourceType = ReconciliationResourceTypes.Container,
            ResourceId = "gateway",
            Description = "test task",
            Risk = RepairRisk.Low,
            RequiresApprovalByDefault = requiresApprovalByDefault
        };

    // ---- D11: Safety Framework ----

    [Fact]
    public void SafetyGate_UnknownActionType_IsAlwaysUnsafe()
    {
        var task = BuildTask(RepairActionType.Unknown, requiresApprovalByDefault: true);
        var context = new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true };

        var result = ReconciliationSafetyGate.Evaluate(task, context);

        Assert.False(result.Safe);
    }

    [Fact]
    public void SafetyGate_StalePlan_IsUnsafeEvenForLowRiskAction()
    {
        var task = BuildTask(RepairActionType.RestartContainer, requiresApprovalByDefault: false);
        var context = new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = false, PackageIntegrityVerified = true };

        var result = ReconciliationSafetyGate.Evaluate(task, context);

        Assert.False(result.Safe);
    }

    /// <summary>Sprint M2.7: maintenance mode suspends automatic repair even for an otherwise fully-safe, low-risk action — and is checked before every other condition, so its rejection message is unambiguous about the reason.</summary>
    [Fact]
    public void SafetyGate_MaintenanceModeActive_IsUnsafeEvenForLowRiskAction()
    {
        var task = BuildTask(RepairActionType.RestartContainer, requiresApprovalByDefault: false);
        var context = new SafetyContext
        {
            CapabilityAvailable = true,
            DeploymentPlanIsCurrent = true,
            PackageIntegrityVerified = true,
            MaintenanceModeActive = true
        };

        var result = ReconciliationSafetyGate.Evaluate(task, context);

        Assert.False(result.Safe);
        Assert.NotNull(result.Reason);
        Assert.Contains("maintenance mode", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Sprint M2.7: existing M2.6 <see cref="SafetyContext"/> construction sites that never set <see cref="SafetyContext.MaintenanceModeActive"/> keep working — it defaults to <see langword="false"/> and never blocks a repair by itself.</summary>
    [Fact]
    public void SafetyGate_MaintenanceModeActive_DefaultsToFalse()
    {
        var task = BuildTask(RepairActionType.RestartContainer, requiresApprovalByDefault: false);
        var context = new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true };

        Assert.False(context.MaintenanceModeActive);
        Assert.True(ReconciliationSafetyGate.Evaluate(task, context).Safe);
    }

    // ---- D6: Self-Healing Engine ----

    private sealed class RecordingExecutor : IRepairExecutor
    {
        public int InvocationCount { get; private set; }
        public bool CanHandle(RepairActionType actionType) => actionType == RepairActionType.RestartContainer;

        public Task<RepairExecutionOutcome> ExecuteAsync(RepairTask task, DriftFinding finding, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(RepairExecutionOutcome.Success("restarted"));
        }
    }

    [Fact]
    public async Task SelfHealingEngine_ApprovedSafeTask_ExecutesViaMatchingExecutor()
    {
        var executor = new RecordingExecutor();
        var registry = new RepairExecutorRegistry([executor]);
        var approvalStore = new InMemoryOperatorApprovalStore();
        var engine = new SelfHealingEngine(new RepairPolicyEvaluator(), registry, approvalStore);

        var finding = Finding(DriftCategory.UnhealthyService, ReconciliationResourceTypes.Container, DriftSeverity.Recoverable, "gateway");
        var driftReport = new DriftReport { EnvironmentId = "local", ServiceId = "gateway", CorrelationId = "corr-7", GeneratedUtc = DateTimeOffset.UtcNow, Findings = [finding] };
        var reconciliationPlan = new ReconciliationPlanner().Plan(driftReport);
        var policySet = new RepairPolicySet
        {
            Environment = EnvironmentClass.Demo,
            Rules = [new RepairPolicyRule { ActionType = RepairActionType.RestartContainer, PolicyType = RepairPolicyType.AlwaysRepair }]
        };

        var result = await engine.HealAsync(reconciliationPlan, driftReport, policySet, _ => new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true });

        Assert.Equal(1, executor.InvocationCount);
        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(RepairOutcomeStatus.Executed, outcome.Status);
        Assert.True(outcome.Succeeded);
        Assert.Contains(result.Events, e => e.Type == ReconciliationEventType.RepairStarted);
        Assert.Contains(result.Events, e => e.Type == ReconciliationEventType.RepairCompleted);
    }

    [Fact]
    public async Task SelfHealingEngine_NeverRepairPolicy_RejectsWithoutExecuting()
    {
        var executor = new RecordingExecutor();
        var registry = new RepairExecutorRegistry([executor]);
        var engine = new SelfHealingEngine(new RepairPolicyEvaluator(), registry, new InMemoryOperatorApprovalStore());

        var finding = Finding(DriftCategory.UnhealthyService, ReconciliationResourceTypes.Container, DriftSeverity.Recoverable, "gateway");
        var driftReport = new DriftReport { EnvironmentId = "local", ServiceId = "gateway", CorrelationId = "corr-8", GeneratedUtc = DateTimeOffset.UtcNow, Findings = [finding] };
        var reconciliationPlan = new ReconciliationPlanner().Plan(driftReport);
        var policySet = new RepairPolicySet
        {
            Environment = EnvironmentClass.Production,
            Rules = [new RepairPolicyRule { ActionType = RepairActionType.RestartContainer, PolicyType = RepairPolicyType.NeverRepair }]
        };

        var result = await engine.HealAsync(reconciliationPlan, driftReport, policySet, _ => new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true });

        Assert.Equal(0, executor.InvocationCount);
        Assert.Equal(RepairOutcomeStatus.Rejected, Assert.Single(result.Outcomes).Status);
        Assert.Contains(result.Events, e => e.Type == ReconciliationEventType.RepairRejected);
    }

    [Fact]
    public async Task SelfHealingEngine_ApprovalRequired_PendingUntilApproved_ThenExecutes()
    {
        var executor = new RecordingExecutor();
        var registry = new RepairExecutorRegistry([executor]);
        var approvalStore = new InMemoryOperatorApprovalStore();
        var engine = new SelfHealingEngine(new RepairPolicyEvaluator(), registry, approvalStore);

        var finding = Finding(DriftCategory.UnhealthyService, ReconciliationResourceTypes.Container, DriftSeverity.Recoverable, "gateway");
        var driftReport = new DriftReport { EnvironmentId = "local", ServiceId = "gateway", CorrelationId = "corr-9", GeneratedUtc = DateTimeOffset.UtcNow, Findings = [finding] };
        var reconciliationPlan = new ReconciliationPlanner().Plan(driftReport);
        var policySet = new RepairPolicySet
        {
            Environment = EnvironmentClass.Production,
            Rules = [new RepairPolicyRule { ActionType = RepairActionType.RestartContainer, PolicyType = RepairPolicyType.AskOperator }]
        };
        var safetyFactory = new Func<RepairTask, SafetyContext>(_ => new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true });

        var firstAttempt = await engine.HealAsync(reconciliationPlan, driftReport, policySet, safetyFactory);
        Assert.Equal(RepairOutcomeStatus.PendingApproval, Assert.Single(firstAttempt.Outcomes).Status);
        Assert.Equal(0, executor.InvocationCount);
        Assert.Contains(firstAttempt.Events, e => e.Type == ReconciliationEventType.OperatorApprovalRequired);

        var taskId = reconciliationPlan.RepairTasks[0].TaskId;
        approvalStore.Approve(taskId);

        var secondAttempt = await engine.HealAsync(reconciliationPlan, driftReport, policySet, safetyFactory);
        Assert.Equal(RepairOutcomeStatus.Executed, Assert.Single(secondAttempt.Outcomes).Status);
        Assert.Equal(1, executor.InvocationCount);
    }

    // ---- D8: Scheduler overlap avoidance ----

    private sealed class NoTargetsProvider : IReconciliationTargetProvider
    {
        public IReadOnlyList<ReconciliationTarget> GetCurrentTargets() => [];
    }

    private sealed class SlowOrchestrator : IReconciliationOrchestrator
    {
        public int CallCount;

        public async Task<ReconciliationResult> RunAsync(DeploymentPlan plan, InfrastructureContract contract, Aovias.Deployment.Contracts.ServiceManifest manifest, RepairPolicySet policySet, Func<RepairTask, SafetyContext> safetyContextFactory, string correlationId, string? resolvedNetworkName = null, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CallCount);
            await Task.Delay(200, cancellationToken);
            return new ReconciliationResult
            {
                DriftReport = new DriftReport { EnvironmentId = "local", ServiceId = "gateway", CorrelationId = correlationId, GeneratedUtc = DateTimeOffset.UtcNow, Findings = [] },
                Plan = new ReconciliationPlan { EnvironmentId = "local", ServiceId = "gateway", CorrelationId = correlationId, GeneratedUtc = DateTimeOffset.UtcNow, RepairTasks = [] },
                Healing = new SelfHealingResult(),
                DurationMs = 1
            };
        }
    }

    [Fact]
    public async Task Scheduler_NoTargets_ReturnsEmpty()
    {
        var scheduler = new ReconciliationScheduler(new SlowOrchestrator(), new NoTargetsProvider());
        var results = await scheduler.RequestReconciliationAsync(ReconciliationTrigger.Manual);
        Assert.Empty(results);
    }

    [Fact]
    public async Task Scheduler_OverlappingRequests_CoalesceRatherThanRunConcurrently()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var contract = new InfrastructureContract { ServiceId = "gateway", SchemaVersion = ContractSchemaVersions.Current };
        var manifest = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "gateway",
            DisplayName = "gateway",
            Version = "1.0.0",
            ImageReference = "aovias/gateway:1.0.0",
            ContainerName = "gateway",
            EntryPoint = "App.dll",
            HealthEndpoint = "/health"
        }).Manifest;

        var target = new ReconciliationTarget
        {
            Plan = plan,
            Contract = contract,
            Manifest = manifest,
            PolicySet = new RepairPolicySet { Environment = EnvironmentClass.Demo },
            SafetyContextFactory = _ => new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true }
        };

        var orchestrator = new SlowOrchestrator();
        var provider = new SingleTargetProvider(target);
        var scheduler = new ReconciliationScheduler(orchestrator, provider);

        var first = scheduler.RequestReconciliationAsync(ReconciliationTrigger.Periodic);
        await Task.Delay(20); // let the first cycle acquire the gate
        var second = await scheduler.RequestReconciliationAsync(ReconciliationTrigger.EventTriggered);

        await first;

        Assert.Empty(second); // coalesced — second request saw a cycle already running
        Assert.Equal(1, orchestrator.CallCount);
    }

    private sealed class SingleTargetProvider(ReconciliationTarget target) : IReconciliationTargetProvider
    {
        public IReadOnlyList<ReconciliationTarget> GetCurrentTargets() => [target];
    }

    // ---- D9/D10: Full orchestrator lifecycle — events + history + idempotency ----

    private sealed class FakeDiscoveryEngine(Func<DesiredState, ObservedState> factory) : IObservedStateDiscoveryEngine
    {
        public Task<ObservedState> DiscoverAsync(DesiredState desiredState, CancellationToken cancellationToken = default)
            => Task.FromResult(factory(desiredState));
    }

    [Fact]
    public async Task Orchestrator_FullLifecycle_EmitsEventsInOrder_AndPersistsHistory()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var contract = new InfrastructureContract { ServiceId = "gateway", SchemaVersion = ContractSchemaVersions.Current };
        var manifest = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "gateway",
            DisplayName = "gateway",
            Version = "1.0.0",
            ImageReference = "aovias/gateway:1.0.0",
            ContainerName = "gateway",
            EntryPoint = "App.dll",
            HealthEndpoint = "/health"
        }).Manifest;

        var discovery = new FakeDiscoveryEngine(desired => new ObservedState
        {
            EnvironmentId = desired.EnvironmentId,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Services = [new ObservedServiceState { ServiceId = "gateway", Running = true, DiscoveryStatus = DiscoveryStatus.Observed }],
            Containers = [new ObservedContainerState { ContainerName = "gateway", Exists = false, DiscoveryStatus = DiscoveryStatus.Observed }]
        });

        var executor = new RecordingExecutor();
        var registry = new RepairExecutorRegistry([executor]);
        var selfHealing = new SelfHealingEngine(new RepairPolicyEvaluator(), registry, new InMemoryOperatorApprovalStore());
        var historyStore = new InMemoryReconciliationHistoryStore();

        var orchestrator = new ReconciliationOrchestrator(
            new DesiredStateProvider(),
            discovery,
            new DriftDetectionEngine(),
            new ReconciliationPlanner(),
            selfHealing,
            historyStore);

        var policySet = new RepairPolicySet
        {
            Environment = EnvironmentClass.Demo,
            Rules = [new RepairPolicyRule { ActionType = RepairActionType.RestartContainer, PolicyType = RepairPolicyType.AlwaysRepair }]
        };

        var result = await orchestrator.RunAsync(plan, contract, manifest, policySet, _ => new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true }, "corr-full-1");

        Assert.True(result.DriftReport.HasDrift);
        var eventTypes = result.Events.Select(e => e.Type).ToList();
        Assert.Equal(ReconciliationEventType.ReconciliationStarted, eventTypes.First());
        Assert.Equal(ReconciliationEventType.ReconciliationCompleted, eventTypes.Last());
        Assert.Contains(ReconciliationEventType.DriftDetected, eventTypes);
        Assert.Contains(ReconciliationEventType.RepairStarted, eventTypes);
        Assert.Contains(ReconciliationEventType.RepairCompleted, eventTypes);
        Assert.Contains(ReconciliationEventType.DriftResolved, eventTypes);

        var history = await historyStore.GetHistoryAsync("gateway");
        var record = Assert.Single(history);
        Assert.Equal("corr-full-1", record.CorrelationId);
        Assert.Equal(1, record.DriftFindingCount);
    }

    [Fact]
    public async Task Orchestrator_IdempotentReconciliation_SecondCycleWithNoDrift_ProducesNoRepairTasks()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var contract = new InfrastructureContract { ServiceId = "gateway", SchemaVersion = ContractSchemaVersions.Current };
        var manifest = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "gateway",
            DisplayName = "gateway",
            Version = "1.0.0",
            ImageReference = "aovias/gateway:1.0.0",
            ContainerName = "gateway",
            EntryPoint = "App.dll",
            HealthEndpoint = "/health"
        }).Manifest;

        // Second cycle: the container now exists and is healthy — no drift should be found, and
        // no repair task should be produced or executed (idempotent: reconciling an
        // already-healthy environment is a safe no-op).
        var discovery = new FakeDiscoveryEngine(desired => new ObservedState
        {
            EnvironmentId = desired.EnvironmentId,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Services = [new ObservedServiceState { ServiceId = "gateway", Running = true, DiscoveryStatus = DiscoveryStatus.Observed }],
            Containers = [new ObservedContainerState { ContainerName = "gateway", Exists = true, State = "running", Health = "healthy", ImageReference = "aovias/gateway:1.0.0", DiscoveryStatus = DiscoveryStatus.Observed }]
        });

        var executor = new RecordingExecutor();
        var registry = new RepairExecutorRegistry([executor]);
        var selfHealing = new SelfHealingEngine(new RepairPolicyEvaluator(), registry, new InMemoryOperatorApprovalStore());

        var orchestrator = new ReconciliationOrchestrator(
            new DesiredStateProvider(),
            discovery,
            new DriftDetectionEngine(),
            new ReconciliationPlanner(),
            selfHealing,
            new InMemoryReconciliationHistoryStore());

        var policySet = new RepairPolicySet { Environment = EnvironmentClass.Demo };

        var result = await orchestrator.RunAsync(plan, contract, manifest, policySet, _ => new SafetyContext { CapabilityAvailable = true, DeploymentPlanIsCurrent = true, PackageIntegrityVerified = true }, "corr-idem-1");

        Assert.False(result.DriftReport.HasDrift);
        Assert.Empty(result.Plan.RepairTasks);
        Assert.Empty(result.Healing.Outcomes);
        Assert.Equal(0, executor.InvocationCount);
    }
}
