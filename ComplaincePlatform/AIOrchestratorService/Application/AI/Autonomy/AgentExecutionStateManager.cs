using System.Text.Json;
using AIOrchestratorService.Domain;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIOrchestratorService.Application.AI.Autonomy;

public interface IAgentExecutionStateManager
{
    Task<AgentExecution> CreateAsync(Guid tenantId, string goal, WorkflowTemplate template, string contextVersionId, CancellationToken cancellationToken);
    Task<AgentExecution?> GetAsync(Guid executionId, CancellationToken cancellationToken);
    Task<List<AgentExecution>> ListAsync(AgentExecutionStatus? status, CancellationToken cancellationToken);
    Task UpdateStatusAsync(Guid executionId, AgentExecutionStatus status, AgentFailureType failureType, string? reason, CancellationToken cancellationToken);
    Task ApproveAsync(Guid executionId, Guid approvedBy, string reason, CancellationToken cancellationToken);
    Task RejectAsync(Guid executionId, Guid rejectedBy, string reason, CancellationToken cancellationToken);
    Task MarkStepAwaitingApprovalAsync(Guid executionId, string stepId, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task<bool> RejectTimedOutApprovalsAsync(Guid executionId, CancellationToken cancellationToken);
    Task AddLogAsync(Guid executionId, string eventType, string message, object? metadata, CancellationToken cancellationToken);
    Task<List<AgentExecutionStep>> GetStepsAsync(Guid executionId, CancellationToken cancellationToken);
    Task UpdateStepAsync(AgentExecutionStep step, CancellationToken cancellationToken);
}

public sealed class AgentExecutionStateManager : IAgentExecutionStateManager
{
    private readonly AIOrchestratorDbContext _dbContext;

    public AgentExecutionStateManager(AIOrchestratorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AgentExecution> CreateAsync(Guid tenantId, string goal, WorkflowTemplate template, string contextVersionId, CancellationToken cancellationToken)
    {
        var sortedSteps = template.Steps.OrderBy(x => x.Sequence).ToList();
        var execution = new AgentExecution
        {
            TenantId = tenantId,
            Goal = goal,
            WorkflowTemplateId = template.TemplateId,
            WorkflowTemplateVersion = template.Version,
            ContextVersionId = contextVersionId,
            CurrentStepId = sortedSteps.FirstOrDefault()?.StepId ?? string.Empty,
            PendingStepIdsJson = JsonSerializer.Serialize(sortedSteps.Select(x => x.StepId).ToList()),
            CompletedStepIdsJson = "[]",
            Status = AgentExecutionStatus.Running
        };

        _dbContext.Set<AgentExecution>().Add(execution);
        foreach (var step in sortedSteps)
        {
            _dbContext.Set<AgentExecutionStep>().Add(new AgentExecutionStep
            {
                ExecutionId = execution.Id,
                StepId = step.StepId,
                Sequence = step.Sequence,
                ToolName = step.ToolName,
                DependsOnJson = JsonSerializer.Serialize(step.DependsOn),
                IsParallelizable = step.IsParallelizable,
                Status = AgentExecutionStatus.Running
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return execution;
    }

    public Task<AgentExecution?> GetAsync(Guid executionId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<AgentExecution>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
    }

    public Task<List<AgentExecution>> ListAsync(AgentExecutionStatus? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<AgentExecution>().AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }
        return query.OrderByDescending(x => x.UpdatedAtUtc).Take(200).ToListAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid executionId, AgentExecutionStatus status, AgentFailureType failureType, string? reason, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.Set<AgentExecution>().FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
        if (execution == null)
        {
            return;
        }

        execution.Status = status;
        execution.FailureType = failureType;
        execution.FailureReason = reason;
        execution.UpdatedAtUtc = DateTime.UtcNow;
        if (status is AgentExecutionStatus.Completed or AgentExecutionStatus.Failed or AgentExecutionStatus.Rejected or AgentExecutionStatus.Cancelled)
        {
            execution.CompletedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(Guid executionId, Guid approvedBy, string reason, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.Set<AgentExecution>().FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
        if (execution == null)
        {
            return;
        }

        execution.Status = AgentExecutionStatus.Approved;
        execution.ApprovedBy = approvedBy;
        execution.ApprovedAtUtc = DateTime.UtcNow;
        execution.ApprovalReason = reason;
        execution.UpdatedAtUtc = DateTime.UtcNow;

        var awaitingStep = await _dbContext.Set<AgentExecutionStep>()
            .Where(x => x.ExecutionId == executionId && x.Status == AgentExecutionStatus.AwaitingApproval)
            .OrderBy(x => x.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (awaitingStep != null)
        {
            if (awaitingStep.ApprovalExpiresAtUtc.HasValue && awaitingStep.ApprovalExpiresAtUtc.Value <= DateTime.UtcNow)
            {
                awaitingStep.Status = AgentExecutionStatus.TimedOut;
                awaitingStep.FailureType = AgentFailureType.Validation;
                awaitingStep.FailureReason = "Approval timeout reached before approval.";
                execution.Status = AgentExecutionStatus.TimedOut;
                execution.FailureType = AgentFailureType.Validation;
                execution.FailureReason = "Approval timeout reached before approval.";
                execution.CompletedAtUtc = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            awaitingStep.Status = AgentExecutionStatus.Running;
            awaitingStep.ApprovedBy = approvedBy;
            awaitingStep.ApprovedAtUtc = DateTime.UtcNow;
            awaitingStep.ApprovalReason = reason;
            awaitingStep.FailureReason = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid executionId, Guid rejectedBy, string reason, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.Set<AgentExecution>().FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
        if (execution == null)
        {
            return;
        }

        var awaitingStep = await _dbContext.Set<AgentExecutionStep>()
            .Where(x => x.ExecutionId == executionId && x.Status == AgentExecutionStatus.AwaitingApproval)
            .OrderBy(x => x.Sequence)
            .FirstOrDefaultAsync(cancellationToken);
        if (awaitingStep != null)
        {
            awaitingStep.Status = AgentExecutionStatus.Rejected;
            awaitingStep.ApprovedBy = rejectedBy;
            awaitingStep.ApprovedAtUtc = DateTime.UtcNow;
            awaitingStep.ApprovalReason = reason;
            awaitingStep.FailureType = AgentFailureType.PolicyViolation;
            awaitingStep.FailureReason = "Step rejected by approver.";
        }

        execution.Status = AgentExecutionStatus.Rejected;
        execution.FailureType = AgentFailureType.PolicyViolation;
        execution.FailureReason = reason;
        execution.ApprovedBy = rejectedBy;
        execution.ApprovedAtUtc = DateTime.UtcNow;
        execution.ApprovalReason = reason;
        execution.CompletedAtUtc = DateTime.UtcNow;
        execution.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkStepAwaitingApprovalAsync(Guid executionId, string stepId, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        var step = await _dbContext.Set<AgentExecutionStep>()
            .FirstOrDefaultAsync(x => x.ExecutionId == executionId && x.StepId == stepId, cancellationToken);
        if (step == null)
        {
            return;
        }

        step.Status = AgentExecutionStatus.AwaitingApproval;
        step.ApprovalRequestedAtUtc = DateTime.UtcNow;
        step.ApprovalExpiresAtUtc = expiresAtUtc;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RejectTimedOutApprovalsAsync(Guid executionId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var timedOutSteps = await _dbContext.Set<AgentExecutionStep>()
            .Where(x => x.ExecutionId == executionId &&
                        x.Status == AgentExecutionStatus.AwaitingApproval &&
                        x.ApprovalExpiresAtUtc.HasValue &&
                        x.ApprovalExpiresAtUtc.Value <= now)
            .ToListAsync(cancellationToken);
        if (timedOutSteps.Count == 0)
        {
            return false;
        }

        foreach (var step in timedOutSteps)
        {
            step.Status = AgentExecutionStatus.TimedOut;
            step.FailureType = AgentFailureType.Validation;
            step.FailureReason = "Approval timeout reached.";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task AddLogAsync(Guid executionId, string eventType, string message, object? metadata, CancellationToken cancellationToken)
    {
        _dbContext.Set<AgentExecutionLog>().Add(new AgentExecutionLog
        {
            ExecutionId = executionId,
            EventType = eventType,
            Message = message,
            MetadataJson = metadata == null ? "{}" : JsonSerializer.Serialize(metadata)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<AgentExecutionStep>> GetStepsAsync(Guid executionId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<AgentExecutionStep>()
            .Where(x => x.ExecutionId == executionId)
            .OrderBy(x => x.Sequence)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateStepAsync(AgentExecutionStep step, CancellationToken cancellationToken)
    {
        _dbContext.Set<AgentExecutionStep>().Update(step);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
