using System.Text.Json;
using AIOrchestratorService.Application.AI.Actions;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Orchestration;
using AIOrchestratorService.Domain;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Autonomy;

public sealed class StartWorkflowRequest
{
    public string Goal { get; set; } = string.Empty;
    public AIRequest AIRequest { get; set; } = new();
}

public interface IAgentOrchestrator
{
    Task<AgentExecution> StartAsync(StartWorkflowRequest request, CancellationToken cancellationToken);
    Task<AgentExecution?> GetAsync(Guid executionId, CancellationToken cancellationToken);
    Task<List<AgentExecution>> ListAsync(AgentExecutionStatus? status, CancellationToken cancellationToken);
    Task<bool> ApproveAndResumeAsync(Guid executionId, Guid approvedBy, string reason, CancellationToken cancellationToken);
    Task<bool> RejectAsync(Guid executionId, Guid rejectedBy, string reason, CancellationToken cancellationToken);
    Task<bool> CancelAsync(Guid executionId, string reason, CancellationToken cancellationToken);
}

public sealed class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IWorkflowTemplateRegistry _templateRegistry;
    private readonly IPlanValidator _planValidator;
    private readonly IStepDependencyGraph _dependencyGraph;
    private readonly IAutonomyPolicyEngine _policyEngine;
    private readonly IExecutionSupervisor _supervisor;
    private readonly IFailureClassifier _failureClassifier;
    private readonly IAgentExecutionStateManager _stateManager;
    private readonly IAIOrchestrationService _aiOrchestration;
    private readonly IToolPreExecutionValidator _toolPrecheck;
    private readonly IAIToolExecutor _toolExecutor;
    private readonly IAIAuditService _auditService;
    private readonly IOptionsMonitor<AutonomyControlOptions> _controlOptions;
    private readonly IAgentLifecycleEventPublisher _lifecycleEventPublisher;
    private readonly ICompensationOrchestrator _compensationOrchestrator;

    public AgentOrchestrator(
        IWorkflowTemplateRegistry templateRegistry,
        IPlanValidator planValidator,
        IStepDependencyGraph dependencyGraph,
        IAutonomyPolicyEngine policyEngine,
        IExecutionSupervisor supervisor,
        IFailureClassifier failureClassifier,
        IAgentExecutionStateManager stateManager,
        IAIOrchestrationService aiOrchestration,
        IToolPreExecutionValidator toolPrecheck,
        IAIToolExecutor toolExecutor,
        IAIAuditService auditService,
        IOptionsMonitor<AutonomyControlOptions> controlOptions,
        IAgentLifecycleEventPublisher lifecycleEventPublisher,
        ICompensationOrchestrator compensationOrchestrator)
    {
        _templateRegistry = templateRegistry;
        _planValidator = planValidator;
        _dependencyGraph = dependencyGraph;
        _policyEngine = policyEngine;
        _supervisor = supervisor;
        _failureClassifier = failureClassifier;
        _stateManager = stateManager;
        _aiOrchestration = aiOrchestration;
        _toolPrecheck = toolPrecheck;
        _toolExecutor = toolExecutor;
        _auditService = auditService;
        _controlOptions = controlOptions;
        _lifecycleEventPublisher = lifecycleEventPublisher;
        _compensationOrchestrator = compensationOrchestrator;
    }

    public async Task<AgentExecution> StartAsync(StartWorkflowRequest request, CancellationToken cancellationToken)
    {
        var template = _templateRegistry.ResolveForGoal(request.Goal);
        var validation = _planValidator.Validate(template);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Workflow plan invalid: {JsonSerializer.Serialize(validation.Issues)}");
        }

        if (template.Steps.Count > _controlOptions.CurrentValue.MaxStepsPerWorkflow)
        {
            throw new InvalidOperationException("Workflow exceeds max configured step count.");
        }

        var execution = await _stateManager.CreateAsync(
            tenantId: request.AIRequest.HospitalId,
            goal: request.Goal,
            template: template,
            contextVersionId: request.AIRequest.ContextVersionId,
            cancellationToken);

        await _stateManager.AddLogAsync(execution.Id, "AgentExecutionStarted", "Workflow started.", new
        {
            goal = request.Goal,
            contextVersionId = request.AIRequest.ContextVersionId,
            template.TemplateId,
            template.Version,
            plan = template.Steps.Select(x => new
            {
                x.StepId,
                x.Sequence,
                x.ToolName,
                x.ToolVersion,
                x.ToolSchemaVersion,
                x.DependsOn,
                x.IsParallelizable,
                x.RequiresApproval
            }).ToList()
        }, cancellationToken);
        await _lifecycleEventPublisher.PublishAsync("AgentExecutionStarted", execution.Id, execution.TenantId, new
        {
            template.TemplateId,
            template.Version
        }, correlationId: execution.Id.ToString("N"), idempotencyKey: $"AgentExecutionStarted:{execution.Id:N}", cancellationToken);

        await ContinueExecutionAsync(execution.Id, request.AIRequest, cancellationToken);
        return (await _stateManager.GetAsync(execution.Id, cancellationToken))!;
    }

    public Task<AgentExecution?> GetAsync(Guid executionId, CancellationToken cancellationToken)
    {
        return _stateManager.GetAsync(executionId, cancellationToken);
    }

    public Task<List<AgentExecution>> ListAsync(AgentExecutionStatus? status, CancellationToken cancellationToken)
    {
        return _stateManager.ListAsync(status, cancellationToken);
    }

    public async Task<bool> ApproveAndResumeAsync(Guid executionId, Guid approvedBy, string reason, CancellationToken cancellationToken)
    {
        var execution = await _stateManager.GetAsync(executionId, cancellationToken);
        if (execution == null)
        {
            return false;
        }

        await _stateManager.ApproveAsync(executionId, approvedBy, reason, cancellationToken);
        execution = await _stateManager.GetAsync(executionId, cancellationToken);
        if (execution == null || execution.Status == AgentExecutionStatus.TimedOut)
        {
            return false;
        }
        var request = new AIRequest
        {
            HospitalId = execution.TenantId,
            Prompt = execution.Goal,
            ContextVersionId = execution.ContextVersionId,
            PromptTemplateId = "TaskRecommendation",
            PromptTemplateVersion = "1.0",
            UseCase = AIUseCase.TaskRecommendation
        };
        await ContinueExecutionAsync(executionId, request, cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(Guid executionId, string reason, CancellationToken cancellationToken)
    {
        var execution = await _stateManager.GetAsync(executionId, cancellationToken);
        if (execution == null)
        {
            return false;
        }

        await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.Compensating, AgentFailureType.None, "Cancellation initiated.", cancellationToken);
        await CompensateAsync(executionId, cancellationToken);
        await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.Cancelled, AgentFailureType.None, reason, cancellationToken);
        return true;
    }

    public async Task<bool> RejectAsync(Guid executionId, Guid rejectedBy, string reason, CancellationToken cancellationToken)
    {
        var execution = await _stateManager.GetAsync(executionId, cancellationToken);
        if (execution == null)
        {
            return false;
        }

        await _stateManager.RejectAsync(executionId, rejectedBy, reason, cancellationToken);
        await _stateManager.AddLogAsync(executionId, "ApprovalRejected", "Execution rejected by approver.", new
        {
            rejectedBy,
            reason
        }, cancellationToken);
        await _lifecycleEventPublisher.PublishAsync(
            "AgentExecutionRejected",
            executionId,
            execution.TenantId,
            new { rejectedBy, reason },
            correlationId: executionId.ToString("N"),
            idempotencyKey: $"AgentExecutionRejected:{executionId:N}",
            cancellationToken);
        return true;
    }

    private async Task ContinueExecutionAsync(Guid executionId, AIRequest request, CancellationToken cancellationToken)
    {
        var execution = await _stateManager.GetAsync(executionId, cancellationToken);
        if (execution == null)
        {
            return;
        }

        var steps = await _stateManager.GetStepsAsync(executionId, cancellationToken);
        _supervisor.EnsureWorkflowLimits(execution, steps.Count);
        if (await _stateManager.RejectTimedOutApprovalsAsync(executionId, cancellationToken))
        {
            await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.TimedOut, AgentFailureType.Validation, "Approval timeout reached.", cancellationToken);
            await _stateManager.AddLogAsync(executionId, "ApprovalTimedOut", "Approval window expired for one or more steps.", null, cancellationToken);
            await CompensateAsync(executionId, cancellationToken);
            return;
        }

        var completed = steps
            .Where(x => x.Status == AgentExecutionStatus.Completed)
            .Select(x => x.StepId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var template = _templateRegistry.GetById(execution.WorkflowTemplateId, execution.WorkflowTemplateVersion) ??
                       _templateRegistry.ResolveForGoal(execution.Goal);
        var stepTemplateMap = template.Steps.ToDictionary(x => x.StepId, StringComparer.OrdinalIgnoreCase);
        var runningTemplates = template.Steps
            .Where(x => steps.Any(s => s.StepId.Equals(x.StepId, StringComparison.OrdinalIgnoreCase) &&
                                       s.Status is AgentExecutionStatus.Running or AgentExecutionStatus.Rejected))
            .ToList();
        var readyTemplates = _dependencyGraph.GetReadySteps(runningTemplates, completed);
        var parallelBatchIds = readyTemplates
            .Where(x => x.IsParallelizable)
            .Select(x => x.StepId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var executionOrder = steps
            .Where(x => readyTemplates.Any(t => t.StepId.Equals(x.StepId, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Sequence)
            .ThenBy(x => parallelBatchIds.Contains(x.StepId) ? 0 : 1)
            .ToList();

        var progressMade = false;
        foreach (var step in executionOrder)
        {
            if (!stepTemplateMap.TryGetValue(step.StepId, out var stepTemplate))
            {
                continue;
            }
            
            execution = await _stateManager.GetAsync(executionId, cancellationToken);
            if (execution == null)
            {
                return;
            }

            if (_supervisor.IsStuck(execution))
            {
                await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.TimedOut, AgentFailureType.Transient, "Workflow detected as stuck.", cancellationToken);
                await CompensateAsync(executionId, cancellationToken);
                return;
            }

            try
            {
                await _stateManager.AddLogAsync(executionId, "AgentStepInput", $"Step '{step.StepId}' execution input captured.", new
                {
                    executionId,
                    stepId = step.StepId,
                    tool = step.ToolName,
                    request.Prompt,
                    request.ContextVersionId,
                    request.IdempotencyToken
                }, cancellationToken);
                _supervisor.EnsureStepSla(step.StartedAtUtc);
                request.Prompt = $"{request.Prompt}. Execute step: {step.StepId}.";
                var aiResponse = string.Equals(_controlOptions.CurrentValue.RolloutStage, "D0", StringComparison.OrdinalIgnoreCase)
                    ? new AIResponse
                    {
                        SuggestedAction = new AISuggestedAction
                        {
                            ToolName = step.ToolName,
                            RiskClass = stepTemplate.RiskClass
                        },
                        ComplianceHint = "D0 deterministic execution path",
                        ConfidenceScore = 0.95m,
                        CompositeConfidenceScore = 0.95m,
                        ValidationOutcome = AIValidationOutcome.Accept,
                        ContextVersionId = request.ContextVersionId,
                        PromptTemplateId = request.PromptTemplateId,
                        PromptTemplateVersion = request.PromptTemplateVersion,
                        Reasoning = new List<AIReasoningStep>
                        {
                            new() { Code = "D0_BYPASS", Message = "Deterministic D0 bypass for transport/runtime verification." }
                        },
                        SourceReferences = new List<AISourceReference>
                        {
                            new() { SourceType = "runtime", SourceId = "d0-bypass", RelevanceScore = 1m }
                        }
                    }
                    : await _aiOrchestration.RunAssistiveAsync(request, cancellationToken);
                await _auditService.LogAsync(request, aiResponse, $"workflow.{execution.WorkflowTemplateId}.{step.StepId}", cancellationToken);
                await _stateManager.AddLogAsync(executionId, "AgentStepAIOutput", $"Step '{step.StepId}' AI response captured.", new
                {
                    executionId,
                    stepId = step.StepId,
                    confidence = aiResponse.ConfidenceScore,
                    compositeConfidence = aiResponse.CompositeConfidenceScore,
                    validationOutcome = aiResponse.ValidationOutcome.ToString(),
                    aiResponse.ContextVersionId
                }, cancellationToken);

                var action = aiResponse.SuggestedAction ?? new AISuggestedAction
                {
                    ToolName = step.ToolName,
                    RiskClass = AIActionRiskClass.Low
                };
                action.ToolName = step.ToolName;
                if (string.IsNullOrWhiteSpace(request.IdempotencyToken))
                {
                    request.IdempotencyToken = $"{executionId:N}:{step.StepId}";
                }

                var policy = _policyEngine.Evaluate(new WorkflowStepTemplate
                {
                    StepId = stepTemplate.StepId,
                    ToolName = stepTemplate.ToolName,
                    RequiresApproval = stepTemplate.RequiresApproval,
                    RiskClass = action.RiskClass,
                    IsParallelizable = stepTemplate.IsParallelizable
                }, aiResponse.CompositeConfidenceScore, request.HospitalId.ToString("N"));
                await _stateManager.AddLogAsync(executionId, "AgentPolicyDecision", $"Policy evaluated for step '{step.StepId}'.", new
                {
                    executionId,
                    stepId = step.StepId,
                    policy.Allowed,
                    policy.RequiresApproval,
                    policy.Reason,
                    confidence = aiResponse.CompositeConfidenceScore,
                    contextVersionId = request.ContextVersionId
                }, cancellationToken);
                var stepAlreadyApproved = step.ApprovedAtUtc.HasValue && step.Status == AgentExecutionStatus.Running;

                if (!policy.Allowed)
                {
                    step.Status = AgentExecutionStatus.Failed;
                    step.FailureType = AgentFailureType.PolicyViolation;
                    step.FailureReason = policy.Reason;
                    await _stateManager.UpdateStepAsync(step, cancellationToken);
                    await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.Failed, AgentFailureType.PolicyViolation, policy.Reason, cancellationToken);
                    await _lifecycleEventPublisher.PublishAsync(
                        "AgentExecutionFailed",
                        executionId,
                        execution.TenantId,
                        new { reason = policy.Reason },
                        correlationId: executionId.ToString("N"),
                        idempotencyKey: $"AgentExecutionFailed:{executionId:N}:{step.StepId}",
                        cancellationToken);
                    await CompensateAsync(executionId, cancellationToken);
                    return;
                }

                if (policy.RequiresApproval && !stepAlreadyApproved)
                {
                    var expiresAt = DateTime.UtcNow.AddSeconds(Math.Max(1, _controlOptions.CurrentValue.ApprovalTimeoutSeconds));
                    await _stateManager.MarkStepAwaitingApprovalAsync(executionId, step.StepId, expiresAt, cancellationToken);
                    await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.AwaitingApproval, AgentFailureType.None, "Awaiting approval.", cancellationToken);
                    await _stateManager.AddLogAsync(executionId, "AwaitingApproval", $"Step '{step.StepId}' waiting for approval.", new { expiresAtUtc = expiresAt }, cancellationToken);
                    await _lifecycleEventPublisher.PublishAsync(
                        "AgentExecutionAwaitingApproval",
                        executionId,
                        execution.TenantId,
                        new { step = step.StepId, expiresAtUtc = expiresAt },
                        correlationId: executionId.ToString("N"),
                        idempotencyKey: $"AgentExecutionAwaitingApproval:{executionId:N}:{step.StepId}",
                        cancellationToken);
                    return;
                }

                action.Parameters["__toolVersion"] = stepTemplate.ToolVersion;
                action.Parameters["__schemaVersion"] = stepTemplate.ToolSchemaVersion;
                var preview = await _toolPrecheck.ValidateAsync(request, action, approved: true, cancellationToken);
                await _stateManager.AddLogAsync(executionId, "AgentValidationResult", $"Validation completed for step '{step.StepId}'.", new
                {
                    executionId,
                    stepId = step.StepId,
                    preview.Allowed,
                    preview.Reason
                }, cancellationToken);
                if (!preview.Allowed)
                {
                    step.Status = AgentExecutionStatus.Failed;
                    step.FailureType = AgentFailureType.Validation;
                    step.FailureReason = preview.Reason;
                    await _stateManager.UpdateStepAsync(step, cancellationToken);
                    await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.Failed, AgentFailureType.Validation, preview.Reason, cancellationToken);
                    await CompensateAsync(executionId, cancellationToken);
                    return;
                }

                var output = await _toolExecutor.ExecuteAsync(request, action, cancellationToken);
                step.OutputJson = JsonSerializer.Serialize(output);
                step.Status = AgentExecutionStatus.Completed;
                step.CompletedAtUtc = DateTime.UtcNow;
                await _stateManager.UpdateStepAsync(step, cancellationToken);
                completed.Add(step.StepId);
                progressMade = true;
                await _stateManager.AddLogAsync(executionId, "AgentStepCompleted", $"Step '{step.StepId}' completed.", output, cancellationToken);
                await _lifecycleEventPublisher.PublishAsync(
                    "AgentStepCompleted",
                    executionId,
                    execution.TenantId,
                    new { step = step.StepId },
                    correlationId: executionId.ToString("N"),
                    idempotencyKey: $"AgentStepCompleted:{executionId:N}:{step.StepId}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                step.RetryCount += 1;
                step.FailureType = _failureClassifier.Classify(ex);
                step.FailureReason = ex.Message;
                var supervisorDecision = _supervisor.DecideFailureAction(step.FailureType, step.RetryCount);
                if (supervisorDecision == SupervisorDecision.Retry)
                {
                    await _stateManager.UpdateStepAsync(step, cancellationToken);
                    continue;
                }

                step.Status = AgentExecutionStatus.Failed;
                await _stateManager.UpdateStepAsync(step, cancellationToken);
                await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.Failed, step.FailureType, ex.Message, cancellationToken);
                await _stateManager.AddLogAsync(executionId, "AgentExecutionFailed", $"Step '{step.StepId}' failed.", new { ex.Message }, cancellationToken);
                await _lifecycleEventPublisher.PublishAsync(
                    "AgentExecutionFailed",
                    executionId,
                    execution.TenantId,
                    new { step = step.StepId, ex.Message },
                    correlationId: executionId.ToString("N"),
                    idempotencyKey: $"AgentExecutionFailed:{executionId:N}:{step.StepId}:{step.RetryCount}",
                    cancellationToken);
                if (supervisorDecision is SupervisorDecision.Compensate or SupervisorDecision.Escalate)
                {
                    await CompensateAsync(executionId, cancellationToken);
                }
                return;
            }
        }

        var allSteps = await _stateManager.GetStepsAsync(executionId, cancellationToken);
        if (allSteps.All(x => x.Status == AgentExecutionStatus.Completed))
        {
            await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.Completed, AgentFailureType.None, null, cancellationToken);
            await _stateManager.AddLogAsync(executionId, "AgentExecutionCompleted", "Workflow completed successfully.", null, cancellationToken);
            await _lifecycleEventPublisher.PublishAsync(
                "AgentExecutionCompleted",
                executionId,
                execution.TenantId,
                new { status = "completed" },
                correlationId: executionId.ToString("N"),
                idempotencyKey: $"AgentExecutionCompleted:{executionId:N}",
                cancellationToken);
        }
        else if (progressMade)
        {
            await ContinueExecutionAsync(executionId, request, cancellationToken);
        }
    }

    private async Task CompensateAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var execution = await _stateManager.GetAsync(executionId, cancellationToken);
        if (execution == null)
        {
            return;
        }

        await _stateManager.UpdateStatusAsync(executionId, AgentExecutionStatus.Compensating, execution.FailureType, "Compensation started.", cancellationToken);
        var steps = await _stateManager.GetStepsAsync(executionId, cancellationToken);
        var completedSteps = steps.Where(x => x.Status == AgentExecutionStatus.Completed).ToList();
        foreach (var step in completedSteps.OrderByDescending(x => x.Sequence))
        {
            await _stateManager.AddLogAsync(executionId, "AgentCompensationTriggered", $"Compensating step '{step.StepId}' in reverse order.", new { step.ToolName }, cancellationToken);
        }

        await _compensationOrchestrator.RunReverseOrderAsync(execution, completedSteps, cancellationToken);
        await _stateManager.AddLogAsync(executionId, "AgentCompensationCompleted", "Compensation flow completed.", null, cancellationToken);
        await _lifecycleEventPublisher.PublishAsync(
            "AgentCompensationTriggered",
            executionId,
            execution.TenantId,
            new { status = "compensating" },
            correlationId: executionId.ToString("N"),
            idempotencyKey: $"AgentCompensationTriggered:{executionId:N}",
            cancellationToken);
    }
}
