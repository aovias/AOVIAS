using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Execution;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Planning;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M2.5 D12 regression suite: phase execution, task execution, resume, retry,
/// idempotency, progress reporting, event generation, failure recovery, health gates, and
/// cancellation.
/// </summary>
public sealed class DeploymentExecutionRuntimeTests
{
    private static ServiceDeploymentBundle BuildBundle(string serviceId, int port, IReadOnlyList<string> dependencies)
        => ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = serviceId,
            Version = "1.0.0",
            ImageReference = $"aovias/{serviceId}:1.0.0",
            ContainerName = serviceId,
            EntryPoint = "App.dll",
            HealthEndpoint = "/health",
            Ports = port > 0 ? [port] : [],
            Dependencies = dependencies
        });

    private static DeploymentPlan BuildApprovedPlan(string serviceId, int port)
    {
        var bundle = BuildBundle(serviceId, port, []);
        var contract = new InfrastructureContract
        {
            ServiceId = serviceId,
            SchemaVersion = ContractSchemaVersions.Current,
            Requirements = []
        };
        var admission = new AdmissionResult { Decision = AdmissionDecision.Approved, CorrelationId = "corr-exec-1" };

        return new DeploymentPlanBuilder().CreatePlan(new DeploymentPlanRequest
        {
            Bundle = bundle,
            Contract = contract,
            Admission = admission,
            EnvironmentId = "local",
            AvailableCapabilities = [],
            GeneratedUtc = DateTimeOffset.UtcNow
        });
    }

    private static DeploymentExecutionContext BuildContext(DeploymentPlan plan)
        => new()
        {
            EnvironmentId = "local",
            Plan = plan,
            RuntimeAgentVersion = "1.0.0"
        };

    /// <summary>A fake provisioner that handles every task, records invocation counts, and can be configured to fail a specific task a fixed number of times before succeeding.</summary>
    private sealed class CountingTaskProvisioner : IInfrastructureTaskProvisioner
    {
        public Dictionary<string, int> InvocationCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? FailTaskId { get; set; }
        public int FailTimes { get; set; }
        public FailureClassification FailClassification { get; set; } = FailureClassification.Retryable;

        public bool CanHandle(DeploymentTask task) => true;

        public Task<TaskExecutionOutcome> ExecuteTaskAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken)
        {
            InvocationCounts[task.TaskId] = InvocationCounts.GetValueOrDefault(task.TaskId) + 1;

            if (string.Equals(task.TaskId, FailTaskId, StringComparison.OrdinalIgnoreCase) && InvocationCounts[task.TaskId] <= FailTimes)
            {
                return Task.FromResult(TaskExecutionOutcome.Failed($"Simulated failure #{InvocationCounts[task.TaskId]} for '{task.TaskId}'.", FailClassification));
            }

            return Task.FromResult(TaskExecutionOutcome.Completed($"Completed '{task.TaskId}'."));
        }

        public Task<bool> ValidateResultAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.FromResult(true);

        public bool CanResume(DeploymentTask task, TaskExecutionRecord? priorRecord) => true;

        public bool CanRetry(DeploymentTask task, TaskExecutionRecord priorRecord) => true;

        public Task CleanupAsync(DeploymentTask task, DeploymentExecutionContext context, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    // ---- Phase execution ----

    [Fact]
    public async Task Runtime_ExecutesAllEightPhasesInCanonicalOrder()
    {
        var plan = BuildApprovedPlan("identity-service", 5062);
        var context = BuildContext(plan);
        var registry = new InfrastructureTaskProvisionerRegistry([new CountingTaskProvisioner()]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        var phaseStartedOrder = context.History.Events
            .Where(e => e.Type == ExecutionEventType.PhaseStarted)
            .Select(e => e.Phase!.Value)
            .ToList();
        Assert.Equal(
        [
            DeploymentExecutionPhaseName.Preflight,
            DeploymentExecutionPhaseName.Infrastructure,
            DeploymentExecutionPhaseName.Upload,
            DeploymentExecutionPhaseName.Configuration,
            DeploymentExecutionPhaseName.Deployment,
            DeploymentExecutionPhaseName.Health,
            DeploymentExecutionPhaseName.Certification,
            DeploymentExecutionPhaseName.Completion
        ], phaseStartedOrder);
    }

    // ---- Task execution / events ----

    [Fact]
    public async Task Runtime_EmitsTaskStartedAndCompletedForEveryTask()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var context = BuildContext(plan);
        var registry = new InfrastructureTaskProvisionerRegistry([new CountingTaskProvisioner()]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.True(result.Success);
        var totalTasks = plan.ExecutionPhases.Sum(p => p.Tasks.Count);
        var startedCount = context.History.Events.Count(e => e.Type == ExecutionEventType.TaskStarted);
        var completedCount = context.History.Events.Count(e => e.Type == ExecutionEventType.TaskCompleted);
        Assert.Equal(totalTasks, startedCount);
        Assert.Equal(totalTasks, completedCount);
    }

    [Fact]
    public async Task Runtime_EmitsHealthVerifiedAndCertificationCompleted()
    {
        var plan = BuildApprovedPlan("identity-service", 5062);
        var context = BuildContext(plan);
        var gateCheck = new AlwaysPassGateCheck();
        var registry = new InfrastructureTaskProvisionerRegistry(
        [
            new HealthGateTaskProvisioner(gateCheck),
            new CertificationTaskProvisioner(gateCheck),
            new CountingTaskProvisioner()
        ]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Contains(context.History.Events, e => e.Type == ExecutionEventType.HealthVerified);
        Assert.Contains(context.History.Events, e => e.Type == ExecutionEventType.CertificationCompleted);
    }

    // ---- Retry ----

    [Fact]
    public async Task Runtime_RetriesRetryableFailure_AndEventuallySucceeds()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var context = BuildContext(plan);
        var failingTaskId = plan.ExecutionPhases.First(p => p.Name == DeploymentExecutionPhaseName.Preflight).Tasks[0].TaskId;
        var provisioner = new CountingTaskProvisioner { FailTaskId = failingTaskId, FailTimes = 2, FailClassification = FailureClassification.Retryable };
        var registry = new InfrastructureTaskProvisionerRegistry([provisioner]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Equal(3, provisioner.InvocationCounts[failingTaskId]);
        Assert.Equal(2, result.Progress.RetryCount);
        Assert.Contains(context.History.Events, e => e.Type == ExecutionEventType.RetryStarted);
    }

    [Fact]
    public async Task Runtime_StopsAfterMaxRetries_WhenStillFailing()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var context = BuildContext(plan);
        var failingTaskId = plan.ExecutionPhases.First(p => p.Name == DeploymentExecutionPhaseName.Preflight).Tasks[0].TaskId;
        var provisioner = new CountingTaskProvisioner { FailTaskId = failingTaskId, FailTimes = 99, FailClassification = FailureClassification.Retryable };
        var registry = new InfrastructureTaskProvisionerRegistry([provisioner]);
        var runtime = new DeploymentExecutionRuntime(registry, maxRetries: 3);

        var result = await runtime.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(3, provisioner.InvocationCounts[failingTaskId]);
        Assert.True(result.CanResume); // Retryable-exhausted still defaults to a non-Fatal, resumable stop in this scenario's classification
    }

    // ---- Failure recovery / classification ----

    [Fact]
    public async Task Runtime_FatalFailure_ReportsCanResumeFalse()
    {
        var plan = BuildApprovedPlan("identity-service", 5062);
        var context = BuildContext(plan);
        // No InfrastructureProvisioningContext seeded -> ExistingInfrastructurePipelineTaskProvisioner fails Fatal on the first Infrastructure task.
        var registry = new InfrastructureTaskProvisionerRegistry(
        [
            new ExistingInfrastructurePipelineTaskProvisioner(),
            new CountingTaskProvisioner()
        ]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.False(result.CanResume);
        Assert.Contains(result.Errors, e => e.Contains("No InfrastructureProvisioningContext was supplied"));
    }

    [Fact]
    public async Task Runtime_RecoverableFailure_ReportsCanResumeTrue()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var context = BuildContext(plan);
        var failingTaskId = plan.ExecutionPhases.First(p => p.Name == DeploymentExecutionPhaseName.Preflight).Tasks[0].TaskId;
        var provisioner = new CountingTaskProvisioner { FailTaskId = failingTaskId, FailTimes = 99, FailClassification = FailureClassification.Recoverable };
        var registry = new InfrastructureTaskProvisionerRegistry([provisioner]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.True(result.CanResume);
        // Recoverable is never auto-retried by the runtime itself.
        Assert.Equal(1, provisioner.InvocationCounts[failingTaskId]);
    }

    // ---- Health gates ----

    [Fact]
    public async Task Runtime_HealthGateFailure_StopsBeforeCertification()
    {
        var plan = BuildApprovedPlan("identity-service", 5062);
        var context = BuildContext(plan);
        var failingGateCheck = new FakeGateCheck(passes: false);
        var registry = new InfrastructureTaskProvisionerRegistry(
        [
            new HealthGateTaskProvisioner(failingGateCheck),
            new CertificationTaskProvisioner(failingGateCheck),
            new CountingTaskProvisioner()
        ]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.DoesNotContain(context.History.Events, e => e.Type == ExecutionEventType.CertificationCompleted);
        Assert.DoesNotContain(context.History.Events, e => e.Type == ExecutionEventType.PhaseCompleted && e.Phase == DeploymentExecutionPhaseName.Health);
    }

    private sealed class FakeGateCheck(bool passes) : IExecutionGateCheck
    {
        public Task<GateCheckResult> CheckAsync(string gateId, DeploymentExecutionContext context, CancellationToken cancellationToken)
            => Task.FromResult(passes ? GateCheckResult.Pass() : GateCheckResult.Fail("simulated health failure"));
    }

    // ---- Resume / idempotency ----

    [Fact]
    public async Task Runtime_Resume_DoesNotReExecuteAlreadyCompletedTasks()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var firstContext = BuildContext(plan);
        var failingTaskId = plan.ExecutionPhases.First(p => p.Name == DeploymentExecutionPhaseName.Configuration).Tasks[0].TaskId;
        var provisioner = new CountingTaskProvisioner { FailTaskId = failingTaskId, FailTimes = 99, FailClassification = FailureClassification.Recoverable };
        var registry = new InfrastructureTaskProvisionerRegistry([provisioner]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var firstResult = await runtime.ExecuteAsync(firstContext);
        Assert.False(firstResult.Success);
        Assert.True(firstResult.CanResume);

        var preflightTaskId = plan.ExecutionPhases.First(p => p.Name == DeploymentExecutionPhaseName.Preflight).Tasks[0].TaskId;
        var invocationsBeforeResume = provisioner.InvocationCounts[preflightTaskId];

        // Fix the simulated problem, then resume with a fresh context sharing the same plan.
        provisioner.FailTaskId = null;
        var secondContext = BuildContext(plan);
        var secondResult = await runtime.ResumeAsync(secondContext, firstResult.History);

        Assert.True(secondResult.Success, string.Join("; ", secondResult.Errors));
        // The Preflight task, already Completed before the failure, must not have been invoked again.
        Assert.Equal(invocationsBeforeResume, provisioner.InvocationCounts[preflightTaskId]);
        Assert.Contains(secondContext.History.Events, e => e.Type == ExecutionEventType.ResumeStarted);
    }

    [Fact]
    public async Task Runtime_ExecutingSamePlanTwice_IsIdempotent()
    {
        var plan = BuildApprovedPlan("gateway", 5212);
        var provisioner = new CountingTaskProvisioner();
        var registry = new InfrastructureTaskProvisionerRegistry([provisioner]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var context = BuildContext(plan);
        var firstResult = await runtime.ExecuteAsync(context);
        Assert.True(firstResult.Success);

        // Re-running against the SAME context/history (simulating a duplicate trigger of the
        // same already-completed deployment) must not re-invoke any provisioner.
        var invocationsAfterFirstRun = provisioner.InvocationCounts.ToDictionary(pair => pair.Key, pair => pair.Value);
        var secondResult = await runtime.ExecuteAsync(context);

        Assert.True(secondResult.Success);
        foreach (var (taskId, count) in invocationsAfterFirstRun)
        {
            Assert.Equal(count, provisioner.InvocationCounts[taskId]);
        }
    }

    // ---- Progress reporting ----

    [Fact]
    public async Task Runtime_ReportsFullProgressOnSuccess()
    {
        var plan = BuildApprovedPlan("identity-service", 5062);
        var context = BuildContext(plan);
        var registry = new InfrastructureTaskProvisionerRegistry([new CountingTaskProvisioner()]);
        var runtime = new DeploymentExecutionRuntime(registry);

        var result = await runtime.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(100d, result.Progress.PercentComplete);
        Assert.Empty(result.Progress.Errors);
        Assert.True(result.Progress.Duration >= TimeSpan.Zero);
    }

    // ---- Cancellation ----

    [Fact]
    public async Task Runtime_Cancellation_ThrowsOperationCanceled()
    {
        var plan = BuildApprovedPlan("identity-service", 5062);
        var context = BuildContext(plan);
        var registry = new InfrastructureTaskProvisionerRegistry([new CountingTaskProvisioner()]);
        var runtime = new DeploymentExecutionRuntime(registry);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runtime.ExecuteAsync(context, cts.Token));
    }
}
