using AIOrchestratorService.Domain;
using AIOrchestratorService.Application.AI.Configuration;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Autonomy;

public interface IFailureClassifier
{
    AgentFailureType Classify(Exception exception);
}

public sealed class FailureClassifier : IFailureClassifier
{
    public AgentFailureType Classify(Exception exception)
    {
        return exception switch
        {
            TimeoutException => AgentFailureType.Transient,
            TaskCanceledException => AgentFailureType.Transient,
            UnauthorizedAccessException => AgentFailureType.PolicyViolation,
            ArgumentException => AgentFailureType.Validation,
            InvalidOperationException => AgentFailureType.DependencyFailure,
            _ => AgentFailureType.SystemError
        };
    }
}

public interface IExecutionSupervisor
{
    void EnsureWorkflowLimits(AgentExecution execution, int stepCount);
    void EnsureStepSla(DateTime stepStartedAtUtc);
    bool IsStuck(AgentExecution execution);
    bool IsApprovalTimedOut(AgentExecutionStep step);
    SupervisorDecision DecideFailureAction(AgentFailureType failureType, int retryCount);
}

public enum SupervisorDecision
{
    Retry = 1,
    Fail = 2,
    Compensate = 3,
    Escalate = 4
}

public sealed class ExecutionSupervisor : IExecutionSupervisor
{
    private readonly IOptionsMonitor<AutonomyControlOptions> _options;

    public ExecutionSupervisor(IOptionsMonitor<AutonomyControlOptions> options)
    {
        _options = options;
    }

    public void EnsureWorkflowLimits(AgentExecution execution, int stepCount)
    {
        var options = _options.CurrentValue;
        if (stepCount > options.MaxStepsPerWorkflow)
        {
            throw new InvalidOperationException($"Workflow exceeds max steps limit ({options.MaxStepsPerWorkflow}).");
        }

        if (DateTime.UtcNow - execution.StartedAtUtc > TimeSpan.FromSeconds(options.WorkflowTimeoutSeconds))
        {
            throw new TimeoutException("Workflow SLA exceeded.");
        }
    }

    public void EnsureStepSla(DateTime stepStartedAtUtc)
    {
        var options = _options.CurrentValue;
        if (DateTime.UtcNow - stepStartedAtUtc > TimeSpan.FromSeconds(options.StepTimeoutSeconds))
        {
            throw new TimeoutException("Step SLA exceeded.");
        }
    }

    public bool IsStuck(AgentExecution execution)
    {
        var options = _options.CurrentValue;
        return execution.Status == AgentExecutionStatus.Running &&
               (DateTime.UtcNow - execution.UpdatedAtUtc) > TimeSpan.FromSeconds(options.StuckWorkflowThresholdSeconds);
    }

    public bool IsApprovalTimedOut(AgentExecutionStep step)
    {
        return step.Status == AgentExecutionStatus.AwaitingApproval &&
               step.ApprovalExpiresAtUtc.HasValue &&
               DateTime.UtcNow >= step.ApprovalExpiresAtUtc.Value;
    }

    public SupervisorDecision DecideFailureAction(AgentFailureType failureType, int retryCount)
    {
        var options = _options.CurrentValue;
        return failureType switch
        {
            AgentFailureType.Transient when retryCount <= options.RetryBudgetPerStep => SupervisorDecision.Retry,
            AgentFailureType.PolicyViolation => SupervisorDecision.Compensate,
            AgentFailureType.DependencyFailure => SupervisorDecision.Fail,
            AgentFailureType.Validation => SupervisorDecision.Fail,
            AgentFailureType.SystemError when retryCount > options.RetryBudgetPerStep => SupervisorDecision.Escalate,
            AgentFailureType.SystemError => SupervisorDecision.Retry,
            _ => SupervisorDecision.Fail
        };
    }
}
