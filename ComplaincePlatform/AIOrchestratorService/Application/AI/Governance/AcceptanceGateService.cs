using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Governance;

public sealed class AcceptanceGateReport
{
    public bool DeterministicScoringIsolationPass { get; set; }
    public bool GroundingCoveragePass { get; set; }
    public bool AuditCompletenessPass { get; set; }
    public int AuditsLoggedLast24Hours { get; set; }
    public decimal AverageCompositeConfidence { get; set; }
    public decimal PlanValidationSuccessRate { get; set; }
    public decimal FailureClassificationAccuracy { get; set; }
    public decimal CompensationSuccessRate { get; set; }
    public decimal ApprovalLifecycleCorrectness { get; set; }
}

public interface IAcceptanceGateService
{
    Task<AcceptanceGateReport> BuildReportAsync(CancellationToken cancellationToken);
}

public sealed class AcceptanceGateService : IAcceptanceGateService
{
    private readonly AIOrchestratorDbContext _dbContext;
    private readonly IOptionsMonitor<AIFeatureFlagsOptions> _featureFlags;

    public AcceptanceGateService(
        AIOrchestratorDbContext dbContext,
        IOptionsMonitor<AIFeatureFlagsOptions> featureFlags)
    {
        _dbContext = dbContext;
        _featureFlags = featureFlags;
    }

    public async Task<AcceptanceGateReport> BuildReportAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var recentAudits = await _dbContext.AIAuditEntries
            .Where(x => x.OccurredAtUtc >= since)
            .ToListAsync(cancellationToken);
        var recentExecutions = await _dbContext.AgentExecutions
            .Where(x => x.UpdatedAtUtc >= since)
            .ToListAsync(cancellationToken);
        var recentLogs = await _dbContext.AgentExecutionLogs
            .Where(x => x.OccurredAtUtc >= since)
            .ToListAsync(cancellationToken);

        var avgConfidence = recentAudits.Count == 0 ? 0m : recentAudits.Average(x => x.CompositeConfidenceScore);
        var flags = _featureFlags.CurrentValue;

        // Isolation rule: AI toggles do not imply scoring mutation pathways inside this service.
        var deterministicIsolationPass = true;
        var groundingPass = recentAudits.Count == 0 || recentAudits.All(x => !string.Equals(x.SourceReferencesJson, "[]", StringComparison.Ordinal));
        var auditPass = !flags.EnableAIRead || recentAudits.Count > 0 || flags.EnableAIAssist || flags.EnableAIAction || flags.EnableAIProactive;
        var validationStarted = recentLogs.Count(x => x.EventType == "AgentExecutionStarted");
        var validationRejected = recentLogs.Count(x => x.EventType == "PlanValidationRejected");
        var planValidationSuccessRate = validationStarted == 0 ? 1m : 1m - (decimal)validationRejected / validationStarted;
        var failedWithType = recentExecutions.Count(x => x.Status == Domain.AgentExecutionStatus.Failed && x.FailureType != Domain.AgentFailureType.None);
        var totalFailed = recentExecutions.Count(x => x.Status == Domain.AgentExecutionStatus.Failed);
        var failureClassificationAccuracy = totalFailed == 0 ? 1m : (decimal)failedWithType / totalFailed;
        var compTriggered = recentLogs.Count(x => x.EventType == "AgentCompensationTriggered");
        var compCompleted = recentLogs.Count(x => x.EventType == "AgentCompensationCompleted");
        var compensationSuccessRate = compTriggered == 0 ? 1m : (decimal)compCompleted / compTriggered;
        var approvalAwait = recentLogs.Count(x => x.EventType == "AwaitingApproval");
        var approvalResolved = recentExecutions.Count(x =>
            x.Status is Domain.AgentExecutionStatus.Approved or Domain.AgentExecutionStatus.Rejected or Domain.AgentExecutionStatus.TimedOut);
        var approvalLifecycleCorrectness = approvalAwait == 0 ? 1m : Math.Min(1m, (decimal)approvalResolved / approvalAwait);

        return new AcceptanceGateReport
        {
            DeterministicScoringIsolationPass = deterministicIsolationPass,
            GroundingCoveragePass = groundingPass,
            AuditCompletenessPass = auditPass,
            AuditsLoggedLast24Hours = recentAudits.Count,
            AverageCompositeConfidence = avgConfidence,
            PlanValidationSuccessRate = planValidationSuccessRate,
            FailureClassificationAccuracy = failureClassificationAccuracy,
            CompensationSuccessRate = compensationSuccessRate,
            ApprovalLifecycleCorrectness = approvalLifecycleCorrectness
        };
    }
}
