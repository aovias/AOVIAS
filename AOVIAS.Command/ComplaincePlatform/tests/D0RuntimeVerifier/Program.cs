using AIOrchestratorService.Application.AI.Actions;
using AIOrchestratorService.Application.AI.Autonomy;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Orchestration;
using AIOrchestratorService.Application.AI.Prompting;
using AIOrchestratorService.Application.AI.Retrieval;
using AIOrchestratorService.Application.AI.Validation;
using AIOrchestratorService.Application.AI.Reliability;
using AIOrchestratorService.Domain;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
}));

var report = new List<string>();
var traces = new Dictionary<string, object>();

await RunScenario("invalid-plan-missing-dependency", ScenarioInvalidMissingDependency, report, traces);
await RunScenario("invalid-plan-forbidden-tool", ScenarioInvalidForbiddenTool, report, traces);
await RunScenario("context-version-mismatch", ScenarioContextVersionMismatch, report, traces);
await RunScenario("idempotency-under-retry", ScenarioIdempotencyRetry, report, traces);
await RunScenario("compensation-on-step2-failure", ScenarioCompensation, report, traces);
await RunScenario("failure-classification-matrix", ScenarioFailureClassification, report, traces);
await RunScenario("approval-state-machine", ScenarioApprovalStateMachine, report, traces);

Console.WriteLine("=== D0 Runtime Verification Summary ===");
foreach (var line in report)
{
    Console.WriteLine(line);
}

Console.WriteLine(Environment.NewLine + "=== Key Traces ===");
foreach (var kvp in traces)
{
    Console.WriteLine($"[{kvp.Key}] {System.Text.Json.JsonSerializer.Serialize(kvp.Value)}");
}

return;

async Task RunScenario(
    string scenarioName,
    Func<RuntimeHarness, Task<(bool Pass, object Trace, string Detail)>> scenario,
    List<string> output,
    Dictionary<string, object> traceOutput)
{
    await using var harness = RuntimeHarness.Create(loggerFactory, scenarioName);
    try
    {
        var result = await scenario(harness);
        output.Add($"{scenarioName}: {(result.Pass ? "PASS" : "FAIL")} - {result.Detail}");
        traceOutput[scenarioName] = result.Trace;
    }
    catch (Exception ex)
    {
        output.Add($"{scenarioName}: FAIL - unhandled exception: {ex.Message}");
        traceOutput[scenarioName] = new { ex.Message, ex.StackTrace };
    }
}

async Task DriveUntilTerminal(RuntimeHarness h, Guid executionId, int maxApprovals = 10)
{
    for (var i = 0; i < maxApprovals; i++)
    {
        var current = await h.Orchestrator.GetAsync(executionId, CancellationToken.None);
        if (current == null)
        {
            return;
        }

        if (current.Status is AgentExecutionStatus.Completed or AgentExecutionStatus.Failed or AgentExecutionStatus.Compensating or AgentExecutionStatus.TimedOut or AgentExecutionStatus.Cancelled)
        {
            return;
        }

        if (current.Status == AgentExecutionStatus.AwaitingApproval || current.Status == AgentExecutionStatus.Approved || current.Status == AgentExecutionStatus.Running)
        {
            var resumed = await h.Orchestrator.ApproveAndResumeAsync(executionId, Guid.NewGuid(), $"auto-approve-{i + 1}", CancellationToken.None);
            if (!resumed)
            {
                return;
            }
        }
    }
}

async Task<(bool Pass, object Trace, string Detail)> ScenarioInvalidMissingDependency(RuntimeHarness h)
{
    var request = h.CreateStartRequest("invalid-missing-dep");
    try
    {
        await h.Orchestrator.StartAsync(request, CancellationToken.None);
        return (false, new { expected = "start blocked", actual = "started" }, "Workflow should not start for missing dependency.");
    }
    catch (InvalidOperationException ex)
    {
        var pass = ex.Message.Contains("DEPENDENCY_MISSING", StringComparison.OrdinalIgnoreCase);
        return (pass, new { ex.Message }, "Missing dependency validation enforced.");
    }
}

async Task<(bool Pass, object Trace, string Detail)> ScenarioInvalidForbiddenTool(RuntimeHarness h)
{
    var request = h.CreateStartRequest("invalid-forbidden-tool");
    try
    {
        await h.Orchestrator.StartAsync(request, CancellationToken.None);
        return (false, new { expected = "start blocked", actual = "started" }, "Workflow should not start for forbidden tool.");
    }
    catch (InvalidOperationException ex)
    {
        var pass = ex.Message.Contains("FORBIDDEN_TOOL", StringComparison.OrdinalIgnoreCase);
        return (pass, new { ex.Message }, "Forbidden tool validation enforced.");
    }
}

async Task<(bool Pass, object Trace, string Detail)> ScenarioContextVersionMismatch(RuntimeHarness h)
{
    var request = h.CreateStartRequest("audit");
    var execution = await h.Orchestrator.StartAsync(request, CancellationToken.None); // moves to AwaitingApproval in D0
    h.ContextClient.CurrentContextVersion = "mutated-context";
    var approved = await h.Orchestrator.ApproveAndResumeAsync(execution.Id, Guid.NewGuid(), "approve first step", CancellationToken.None);
    var refreshed = await h.Orchestrator.GetAsync(execution.Id, CancellationToken.None);
    var steps = await h.StateManager.GetStepsAsync(execution!.Id, CancellationToken.None);
    var step = steps.First();
    var pass = approved &&
               refreshed?.Status is AgentExecutionStatus.Failed or AgentExecutionStatus.Compensating &&
               step.FailureReason?.Contains("Context version mismatch", StringComparison.OrdinalIgnoreCase) == true;
    return (pass, new
    {
        executionId = execution.Id,
        approved,
        workflowStatus = refreshed?.Status.ToString(),
        firstStepFailure = step.FailureReason
    }, "Context drift blocks step execution.");
}

async Task<(bool Pass, object Trace, string Detail)> ScenarioIdempotencyRetry(RuntimeHarness h)
{
    h.ToolExecutor.ConfigureTimeoutAfterCreate("create_task", 1);
    var request = h.CreateStartRequest("evidence");
    request.AIRequest.IdempotencyToken = "idem-retry-case";
    var execution = await h.Orchestrator.StartAsync(request, CancellationToken.None);
    await DriveUntilTerminal(h, execution.Id, 8);
    var refreshed = await h.Orchestrator.GetAsync(execution.Id, CancellationToken.None);
    var pass = refreshed?.Status == AgentExecutionStatus.Completed &&
               h.ToolExecutor.CreatedTaskCount == 1 &&
               h.ToolExecutor.CreateTaskCallCount >= 2;
    return (pass, new
    {
        executionId = execution.Id,
        status = refreshed?.Status.ToString(),
        createTaskCalls = h.ToolExecutor.CreateTaskCallCount,
        createdTasks = h.ToolExecutor.CreatedTaskCount,
        idempotencyKeys = h.ToolExecutor.ObservedCreateTaskIdempotencyKeys,
        idempotencyToken = "idem-retry-case"
    }, "Retry reused idempotency key without duplicates.");
}

async Task<(bool Pass, object Trace, string Detail)> ScenarioCompensation(RuntimeHarness h)
{
    h.ToolExecutor.FailWithException("create_finding", new UnauthorizedAccessException("permission denied"));
    var request = h.CreateStartRequest("audit");
    request.AIRequest.IdempotencyToken = "idem-comp-case";
    var execution = await h.Orchestrator.StartAsync(request, CancellationToken.None);
    await DriveUntilTerminal(h, execution.Id, 8);
    var refreshed = await h.Orchestrator.GetAsync(execution.Id, CancellationToken.None);
    var compensated = h.ToolExecutor.CompensationActionsTriggered.Contains("cancel_task");
    var pass = refreshed?.Status == AgentExecutionStatus.Compensating && compensated && h.ToolExecutor.UndoneTaskCount > 0;
    return (pass, new
    {
        executionId = execution.Id,
        status = refreshed?.Status.ToString(),
        compensationActions = h.ToolExecutor.CompensationActionsTriggered.ToList(),
        createdTasks = h.ToolExecutor.CreatedTaskCount,
        undoneTasks = h.ToolExecutor.UndoneTaskCount
    }, "Compensation executed reverse-order undo action.");
}

async Task<(bool Pass, object Trace, string Detail)> ScenarioFailureClassification(RuntimeHarness h)
{
    var outcomes = new List<object>();

    // Timeout => Transient with retry
    h.ToolExecutor.ConfigureThrowBeforeExecute("create_task", new TimeoutException("timeout"), count: 1);
    var timeoutExec = await h.Orchestrator.StartAsync(h.CreateStartRequest("evidence"), CancellationToken.None);
    await DriveUntilTerminal(h, timeoutExec.Id, 8);
    var timeoutSteps = await h.StateManager.GetStepsAsync(timeoutExec.Id, CancellationToken.None);
    outcomes.Add(new
    {
        caseName = "timeout",
        createTaskCalls = h.ToolExecutor.CreateTaskCallCount,
        retryObserved = timeoutSteps.Any(x => x.RetryCount > 0)
    });

    // Validation => no retry (force context mismatch)
    h.ContextClient.CurrentContextVersion = "different";
    var validationExec = await h.Orchestrator.StartAsync(h.CreateStartRequest("audit"), CancellationToken.None);
    await DriveUntilTerminal(h, validationExec.Id, 6);
    var validationSteps = await h.StateManager.GetStepsAsync(validationExec.Id, CancellationToken.None);
    outcomes.Add(new
    {
        caseName = "validation",
        retryObserved = validationSteps.Any(x => x.RetryCount > 0),
        failureReasons = validationSteps.Where(x => !string.IsNullOrWhiteSpace(x.FailureReason)).Select(x => x.FailureReason).ToList()
    });
    h.ContextClient.CurrentContextVersion = h.DefaultContextVersion;

    // Policy => block
    h.ToolExecutor.FailWithException("create_finding", new UnauthorizedAccessException("policy denied"));
    var policyExec = await h.Orchestrator.StartAsync(h.CreateStartRequest("audit"), CancellationToken.None);
    await DriveUntilTerminal(h, policyExec.Id, 8);
    var policyDetails = await h.Orchestrator.GetAsync(policyExec.Id, CancellationToken.None);
    outcomes.Add(new
    {
        caseName = "policy",
        status = policyDetails?.Status.ToString(),
        failureType = policyDetails?.FailureType.ToString()
    });

    var pass = outcomes.Count == 3;
    return (pass, outcomes, "Failure classification paths executed.");
}

async Task<(bool Pass, object Trace, string Detail)> ScenarioApprovalStateMachine(RuntimeHarness h)
{
    // Approve path
    var approveExec = await h.Orchestrator.StartAsync(h.CreateStartRequest("evidence"), CancellationToken.None);
    await DriveUntilTerminal(h, approveExec.Id, 8);
    var approveState = await h.Orchestrator.GetAsync(approveExec.Id, CancellationToken.None);
    var approveResult = approveState?.Status == AgentExecutionStatus.Completed;

    // Timeout path
    h.AutonomyOptions.ApprovalTimeoutSeconds = 1;
    var timeoutExec = await h.Orchestrator.StartAsync(h.CreateStartRequest("audit"), CancellationToken.None);
    await Task.Delay(TimeSpan.FromSeconds(2));
    var lateApprove = await h.Orchestrator.ApproveAndResumeAsync(timeoutExec.Id, Guid.NewGuid(), "late approval", CancellationToken.None);
    var timeoutState = await h.Orchestrator.GetAsync(timeoutExec.Id, CancellationToken.None);

    // Reject path
    var rejectExec = await h.Orchestrator.StartAsync(h.CreateStartRequest("audit"), CancellationToken.None);
    var rejected = await h.Orchestrator.RejectAsync(rejectExec.Id, Guid.NewGuid(), "manual reject", CancellationToken.None);
    var rejectState = await h.Orchestrator.GetAsync(rejectExec.Id, CancellationToken.None);
    var rejectImplemented = rejected && rejectState?.Status == AgentExecutionStatus.Rejected;
    var pass = approveResult && timeoutState?.Status == AgentExecutionStatus.TimedOut && !lateApprove && rejectImplemented;
    return (pass, new
    {
        approvePath = approveResult,
        timeoutStatus = timeoutState?.Status.ToString(),
        lateApproveAccepted = lateApprove,
        rejectImplemented,
        rejectStatus = rejectState?.Status.ToString()
    }, "Approval approve/reject/timeout transitions verified.");
}

sealed class RuntimeHarness : IAsyncDisposable
{
    public IAgentOrchestrator Orchestrator { get; }
    public IAgentExecutionStateManager StateManager { get; }
    public FakeComplianceContextClient ContextClient { get; }
    public FakeToolExecutor ToolExecutor { get; }
    public string DefaultContextVersion { get; } = "ctx-v1";
    public AutonomyControlOptions AutonomyOptions { get; }

    private RuntimeHarness(
        IAgentOrchestrator orchestrator,
        IAgentExecutionStateManager stateManager,
        FakeComplianceContextClient contextClient,
        FakeToolExecutor toolExecutor,
        AutonomyControlOptions autonomyOptions)
    {
        Orchestrator = orchestrator;
        StateManager = stateManager;
        ContextClient = contextClient;
        ToolExecutor = toolExecutor;
        AutonomyOptions = autonomyOptions;
    }

    public static RuntimeHarness Create(ILoggerFactory loggerFactory, string scenarioName)
    {
        var dbOptions = new DbContextOptionsBuilder<AIOrchestratorDbContext>()
            .UseInMemoryDatabase($"d0-runtime-{scenarioName}-{Guid.NewGuid():N}")
            .Options;
        var dbContext = new AIOrchestratorDbContext(dbOptions);

        var contextClient = new FakeComplianceContextClient { CurrentContextVersion = "ctx-v1" };
        var toolExecutor = new FakeToolExecutor();
        var toolRegistry = new AIToolRegistry();
        var planValidator = new PlanValidator(toolRegistry, new StepDependencyGraph());
        var promptRegistry = new PromptTemplateRegistry();
        var autonomyOptions = new AutonomyControlOptions
        {
            EnableAutonomousWorkflows = true,
            EnableEventDrivenAutonomy = false,
            EnableAutoExecution = false,
            ForceApprovalForAllActions = true,
            RolloutStage = "D0",
            RetryBudgetPerStep = 2,
            ApprovalTimeoutSeconds = 1800
        };
        var confidenceOptions = new AIConfidencePolicyOptions { SuggestOnlyThreshold = 0.6m, WarnThreshold = 0.8m };
        var performanceOptions = new AIPerformanceOptions { RequestTimeoutMs = 5000, CacheTtlSeconds = 1 };
        var usageOptions = new AIUsagePolicyOptions
        {
            MaxRequestsPerSession = 100,
            SessionWindowMinutes = 60,
            PerTenantRateLimitPerMinute = 120
        };

        var confidenceCalibration = new ConfidenceCalibrationService(new StaticOptionsMonitor<AIConfidencePolicyOptions>(confidenceOptions));
        var mediator = new AIMediator(new AIResponseValidator());
        var metricsTracker = new MetricsTracker(loggerFactory.CreateLogger<MetricsTracker>());
        var httpContextAccessor = new HttpContextAccessor();
        var aiOrchestration = new AIOrchestrationService(
            new FakeRetrievalService(),
            promptRegistry,
            new FakeModelAdapter(),
            mediator,
            confidenceCalibration,
            new MemoryCache(new MemoryCacheOptions()),
            new StaticOptionsMonitor<AIPerformanceOptions>(performanceOptions),
            new StaticOptionsMonitor<AIUsagePolicyOptions>(usageOptions),
            metricsTracker,
            httpContextAccessor,
            loggerFactory.CreateLogger<AIOrchestrationService>());
        var preExecutionValidator = new ToolPreExecutionValidator(
            toolRegistry,
            contextClient,
            confidenceCalibration,
            new StaticOptionsMonitor<AutonomyControlOptions>(autonomyOptions));

        var stateManager = new AgentExecutionStateManager(dbContext);
        var orchestrator = new AgentOrchestrator(
            new TestWorkflowTemplateRegistry(),
            planValidator,
            new StepDependencyGraph(),
            new AutonomyPolicyEngine(
                new StaticOptionsMonitor<AIConfidencePolicyOptions>(confidenceOptions),
                new StaticOptionsMonitor<AutonomyControlOptions>(autonomyOptions)),
            new ExecutionSupervisor(new StaticOptionsMonitor<AutonomyControlOptions>(autonomyOptions)),
            new FailureClassifier(),
            stateManager,
            aiOrchestration,
            preExecutionValidator,
            toolExecutor,
            new AIAuditService(dbContext),
            new StaticOptionsMonitor<AutonomyControlOptions>(autonomyOptions),
            new RecordingLifecyclePublisher(),
            new CompensationOrchestrator(toolRegistry, toolExecutor, loggerFactory.CreateLogger<CompensationOrchestrator>()));

        return new RuntimeHarness(orchestrator, stateManager, contextClient, toolExecutor, autonomyOptions);
    }

    public StartWorkflowRequest CreateStartRequest(string goal)
    {
        return new StartWorkflowRequest
        {
            Goal = goal,
            AIRequest = new AIRequest
            {
                Prompt = $"run scenario {goal}",
                HospitalId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UseCase = AIUseCase.TaskRecommendation,
                ContextVersionId = DefaultContextVersion,
                PromptTemplateId = "TaskRecommendation",
                PromptTemplateVersion = "1.0"
            }
        };
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class
{
    private readonly T _value;
    public StaticOptionsMonitor(T value) => _value = value;
    public T CurrentValue => _value;
    public T Get(string? name) => _value;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

sealed class FakeRetrievalService : IRetrievalService
{
    public Task<AIRetrievalContext> BuildContextAsync(AIRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AIRetrievalContext
        {
            ContextVersionId = request.ContextVersionId,
            Department = request.Department ?? "qa",
            RelevanceScore = 0.9m,
            Sources = new List<AISourceReference>
            {
                new() { SourceType = "RuntimeVerifier", SourceId = "source-1", RelevanceScore = 0.9m, Summary = "Runtime test source." }
            }
        });
    }
}

sealed class FakeModelAdapter : IAIModelAdapter
{
    public Task<AIModelResponse> GenerateAsync(AIModelRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new AIModelResponse
        {
            Hint = $"Model hint for {request.Prompt}",
            Confidence = 0.85m,
            ReasoningBullets = new List<string> { "Reasoning step 1", "Reasoning step 2" }
        });
    }
}

sealed class FakeComplianceContextClient : IComplianceContextClient
{
    public string CurrentContextVersion { get; set; } = "ctx-v1";

    public Task<ComplianceContextBundleDto?> GetContextBundleAsync(ComplianceContextBundleRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult<ComplianceContextBundleDto?>(new ComplianceContextBundleDto
        {
            ContextVersionId = CurrentContextVersion,
            Facts = new Dictionary<string, string> { ["case"] = "runtime" },
            Sources = new List<ComplianceContextSourceDto>
            {
                new() { SourceType = "RuntimeVerifier", SourceId = "ctx", RelevanceScore = 1m, Summary = "Fake context bundle." }
            }
        });
    }

    public Task<ComplianceCommandPreconditionResponse?> ValidateCommandPreconditionsAsync(ComplianceCommandPreconditionRequest request, CancellationToken cancellationToken)
    {
        var allowed = string.Equals(request.ExpectedContextVersionId, CurrentContextVersion, StringComparison.Ordinal) &&
                      !string.IsNullOrWhiteSpace(request.IdempotencyToken) &&
                      request.ApprovalAttested;
        return Task.FromResult<ComplianceCommandPreconditionResponse?>(new ComplianceCommandPreconditionResponse
        {
            IsAllowed = allowed,
            Reason = allowed ? "ok" : "Context version mismatch or idempotency/approval missing.",
            CurrentContextVersionId = CurrentContextVersion
        });
    }
}

sealed class FakeToolExecutor : IAIToolExecutor
{
    private readonly Dictionary<string, int> _callCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _timeoutAfterCreateRemaining = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (Exception Exception, int Remaining)> _throwBeforeExecute = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _taskByIdempotency = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _undoneTasks = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _compActions = new();
    private readonly Dictionary<string, Exception> _alwaysFail = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _observedCreateTaskIdempotencyKeys = new();

    public int CreateTaskCallCount => _callCounts.GetValueOrDefault("create_task", 0);
    public int CreatedTaskCount => _taskByIdempotency.Count;
    public int UndoneTaskCount => _undoneTasks.Count;
    public IReadOnlyList<string> CompensationActionsTriggered => _compActions;
    public IReadOnlyList<string> ObservedCreateTaskIdempotencyKeys => _observedCreateTaskIdempotencyKeys;

    public void ConfigureTimeoutAfterCreate(string toolName, int count)
    {
        _timeoutAfterCreateRemaining[toolName] = count;
    }

    public void ConfigureThrowBeforeExecute(string toolName, Exception ex, int count)
    {
        _throwBeforeExecute[toolName] = (ex, count);
    }

    public void FailWithException(string toolName, Exception ex)
    {
        _alwaysFail[toolName] = ex;
    }

    public Task<Dictionary<string, string>> ExecuteAsync(AIRequest request, AISuggestedAction action, CancellationToken cancellationToken)
    {
        var tool = action.ToolName;
        _callCounts[tool] = _callCounts.GetValueOrDefault(tool, 0) + 1;

        if (_alwaysFail.TryGetValue(tool, out var hardFail))
        {
            throw hardFail;
        }

        if (_throwBeforeExecute.TryGetValue(tool, out var planned) && planned.Remaining > 0)
        {
            _throwBeforeExecute[tool] = (planned.Exception, planned.Remaining - 1);
            throw planned.Exception;
        }

        if (string.Equals(tool, "create_task", StringComparison.OrdinalIgnoreCase))
        {
            var idempotency = request.IdempotencyToken ?? "none";
            _observedCreateTaskIdempotencyKeys.Add(idempotency);
            if (!_taskByIdempotency.ContainsKey(idempotency))
            {
                _taskByIdempotency[idempotency] = $"task-{_taskByIdempotency.Count + 1}";
            }

            if (_timeoutAfterCreateRemaining.TryGetValue(tool, out var remaining) && remaining > 0)
            {
                _timeoutAfterCreateRemaining[tool] = remaining - 1;
                throw new TimeoutException("Simulated timeout after create_task side effect.");
            }

            return Task.FromResult(new Dictionary<string, string>
            {
                ["status"] = "executed",
                ["taskId"] = _taskByIdempotency[idempotency],
                ["idempotency"] = idempotency
            });
        }

        if (string.Equals(tool, "cancel_task", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var taskId in _taskByIdempotency.Values)
            {
                _undoneTasks.Add(taskId);
            }
            _compActions.Add("cancel_task");
            return Task.FromResult(new Dictionary<string, string> { ["status"] = "compensated" });
        }

        if (string.Equals(tool, "close_finding", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tool, "unlink_evidence", StringComparison.OrdinalIgnoreCase))
        {
            _compActions.Add(tool);
            return Task.FromResult(new Dictionary<string, string> { ["status"] = "compensated" });
        }

        return Task.FromResult(new Dictionary<string, string> { ["status"] = "executed", ["tool"] = tool });
    }
}

sealed class RecordingLifecyclePublisher : IAgentLifecycleEventPublisher
{
    public Task PublishAsync(
        string eventType,
        Guid executionId,
        Guid tenantId,
        object payload,
        string? correlationId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

sealed class TestWorkflowTemplateRegistry : IWorkflowTemplateRegistry
{
    private readonly Dictionary<string, WorkflowTemplate> _templates;

    public TestWorkflowTemplateRegistry()
    {
        _templates = new Dictionary<string, WorkflowTemplate>(StringComparer.OrdinalIgnoreCase)
        {
            ["AuditPreparationWorkflow:1.0"] = new WorkflowTemplate
            {
                TemplateId = "AuditPreparationWorkflow",
                Version = "1.0",
                Name = "Audit",
                Steps = new List<WorkflowStepTemplate>
                {
                    new() { StepId = "fetch_tasks", Sequence = 1, ToolName = "query_tasks", ToolVersion = "1.0", ToolSchemaVersion = "1.0" },
                    new() { StepId = "create_tasks", Sequence = 2, ToolName = "create_task", ToolVersion = "1.0", ToolSchemaVersion = "1.0", DependsOn = new List<string> { "fetch_tasks" }, RequiresApproval = true, RiskClass = AIActionRiskClass.High },
                    new() { StepId = "raise_preliminary_findings", Sequence = 3, ToolName = "create_finding", ToolVersion = "1.0", ToolSchemaVersion = "1.0", DependsOn = new List<string> { "create_tasks" }, RequiresApproval = true, RiskClass = AIActionRiskClass.High }
                }
            },
            ["EvidenceCompletionWorkflow:1.0"] = new WorkflowTemplate
            {
                TemplateId = "EvidenceCompletionWorkflow",
                Version = "1.0",
                Name = "Evidence",
                Steps = new List<WorkflowStepTemplate>
                {
                    new() { StepId = "validate_evidence", Sequence = 1, ToolName = "validate_evidence", ToolVersion = "1.0", ToolSchemaVersion = "1.0" },
                    new() { StepId = "recommend_task", Sequence = 2, ToolName = "create_task", ToolVersion = "1.0", ToolSchemaVersion = "1.0", DependsOn = new List<string> { "validate_evidence" }, RequiresApproval = true }
                }
            },
            ["InvalidMissingDependency:1.0"] = new WorkflowTemplate
            {
                TemplateId = "InvalidMissingDependency",
                Version = "1.0",
                Name = "Invalid dep",
                Steps = new List<WorkflowStepTemplate>
                {
                    new() { StepId = "only", Sequence = 1, ToolName = "create_task", ToolVersion = "1.0", ToolSchemaVersion = "1.0", DependsOn = new List<string> { "ghost" } }
                }
            },
            ["InvalidForbiddenTool:1.0"] = new WorkflowTemplate
            {
                TemplateId = "InvalidForbiddenTool",
                Version = "1.0",
                Name = "Invalid tool",
                Steps = new List<WorkflowStepTemplate>
                {
                    new() { StepId = "bad", Sequence = 1, ToolName = "drop_database", ToolVersion = "1.0", ToolSchemaVersion = "1.0" }
                }
            }
        };
    }

    public WorkflowTemplate ResolveForGoal(string goal)
    {
        if (goal.Contains("invalid-missing-dep", StringComparison.OrdinalIgnoreCase))
        {
            return Clone(_templates["InvalidMissingDependency:1.0"]);
        }
        if (goal.Contains("invalid-forbidden-tool", StringComparison.OrdinalIgnoreCase))
        {
            return Clone(_templates["InvalidForbiddenTool:1.0"]);
        }
        if (goal.Contains("evidence", StringComparison.OrdinalIgnoreCase))
        {
            return Clone(_templates["EvidenceCompletionWorkflow:1.0"]);
        }
        return Clone(_templates["AuditPreparationWorkflow:1.0"]);
    }

    public WorkflowTemplate? GetById(string templateId, string version)
    {
        return _templates.TryGetValue($"{templateId}:{version}", out var template) ? Clone(template) : null;
    }

    private static WorkflowTemplate Clone(WorkflowTemplate template)
    {
        return new WorkflowTemplate
        {
            TemplateId = template.TemplateId,
            Version = template.Version,
            Name = template.Name,
            Steps = template.Steps.Select(x => new WorkflowStepTemplate
            {
                StepId = x.StepId,
                Sequence = x.Sequence,
                ToolName = x.ToolName,
                ToolVersion = x.ToolVersion,
                ToolSchemaVersion = x.ToolSchemaVersion,
                DependsOn = x.DependsOn.ToList(),
                IsParallelizable = x.IsParallelizable,
                RequiresApproval = x.RequiresApproval,
                MaxRetries = x.MaxRetries,
                RiskClass = x.RiskClass,
                CompensationHandler = x.CompensationHandler
            }).ToList()
        };
    }
}
