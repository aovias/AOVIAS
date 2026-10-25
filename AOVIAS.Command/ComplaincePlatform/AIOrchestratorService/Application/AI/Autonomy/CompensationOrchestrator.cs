using AIOrchestratorService.Application.AI.Actions;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Domain;

namespace AIOrchestratorService.Application.AI.Autonomy;

public interface ICompensationOrchestrator
{
    Task RunReverseOrderAsync(
        AgentExecution execution,
        IReadOnlyList<AgentExecutionStep> completedSteps,
        CancellationToken cancellationToken);
}

public sealed class CompensationOrchestrator : ICompensationOrchestrator
{
    private readonly IAIToolRegistry _toolRegistry;
    private readonly IAIToolExecutor _toolExecutor;
    private readonly ILogger<CompensationOrchestrator> _logger;

    public CompensationOrchestrator(
        IAIToolRegistry toolRegistry,
        IAIToolExecutor toolExecutor,
        ILogger<CompensationOrchestrator> logger)
    {
        _toolRegistry = toolRegistry;
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async Task RunReverseOrderAsync(
        AgentExecution execution,
        IReadOnlyList<AgentExecutionStep> completedSteps,
        CancellationToken cancellationToken)
    {
        foreach (var step in completedSteps.OrderByDescending(x => x.Sequence))
        {
            var toolDefinition = _toolRegistry.Get(step.ToolName);
            var undoAction = toolDefinition?.CompensationStrategy.UndoAction;
            if (string.IsNullOrWhiteSpace(undoAction) ||
                string.Equals(undoAction, "none", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var request = new AIRequest
                {
                    HospitalId = execution.TenantId,
                    Prompt = $"Compensation for step {step.StepId}",
                    ContextVersionId = execution.ContextVersionId,
                    IdempotencyToken = $"{execution.Id:N}:{step.StepId}:comp"
                };
                var action = new AISuggestedAction
                {
                    ToolName = undoAction,
                    RiskClass = AIActionRiskClass.Low
                };

                var result = await _toolExecutor.ExecuteAsync(request, action, cancellationToken);
                _logger.LogInformation(
                    "Compensation for execution {ExecutionId} step {StepId} used undo action {UndoAction} with status {Status}.",
                    execution.Id,
                    step.StepId,
                    undoAction,
                    result.GetValueOrDefault("status", "unknown"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Compensation action failed for execution {ExecutionId}, step {StepId}.", execution.Id, step.StepId);
            }
        }
    }
}
