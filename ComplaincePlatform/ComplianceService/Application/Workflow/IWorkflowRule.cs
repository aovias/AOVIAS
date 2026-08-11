using ComplianceService.Application.Events;

namespace ComplianceService.Application.Workflow;

/// <summary>
/// Declarative workflow automation rule keyed to a <see cref="ComplianceEventType"/>.
/// </summary>
public interface IWorkflowRule
{
    ComplianceEventType EventType { get; }

    Task ExecuteAsync(ComplianceEventEnvelope envelope, CancellationToken cancellationToken = default);
}

public interface IWorkflowRuleEngine
{
    Task ExecuteAsync(ComplianceEventEnvelope envelope, CancellationToken cancellationToken = default);

    Task ExecuteForOutboxAsync(string outboxType, string payloadJson, CancellationToken cancellationToken = default);
}
