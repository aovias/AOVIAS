using ComplianceService.Application.Events;
using ComplianceService.Application.Workflow;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class WorkflowRuleEngineTests
{
    [Fact]
    public async Task ExecuteAsync_invokes_matching_rule_only()
    {
        var matched = new RecordingRule(ComplianceEventType.EvidenceApproved);
        var other = new RecordingRule(ComplianceEventType.TaskOverdue);
        var engine = new WorkflowRuleEngine(
            [matched, other],
            NullLogger<WorkflowRuleEngine>.Instance);

        var envelope = new ComplianceEventEnvelope
        {
            EventType = ComplianceEventType.EvidenceApproved,
            EntityId = Guid.NewGuid(),
            EntityType = "Evidence",
            TenantId = Guid.NewGuid(),
            Payload = "{}"
        };

        await engine.ExecuteAsync(envelope);

        Assert.Equal(1, matched.CallCount);
        Assert.Equal(0, other.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_swallows_rule_failures()
    {
        var failing = new FailingRule(ComplianceEventType.AuditSessionClosed);
        var engine = new WorkflowRuleEngine([failing], NullLogger<WorkflowRuleEngine>.Instance);

        var envelope = new ComplianceEventEnvelope
        {
            EventType = ComplianceEventType.AuditSessionClosed,
            EntityId = Guid.NewGuid(),
            EntityType = "AuditSession",
            TenantId = Guid.NewGuid(),
            Payload = "{}"
        };

        await engine.ExecuteAsync(envelope);
        Assert.Equal(1, failing.CallCount);
    }

    [Fact]
    public async Task ExecuteForOutboxAsync_deserializes_ComplianceEvent_envelope()
    {
        var matched = new RecordingRule(ComplianceEventType.EvidenceApproved);
        var engine = new WorkflowRuleEngine([matched], NullLogger<WorkflowRuleEngine>.Instance);
        var entityId = Guid.NewGuid();
        var envelope = new ComplianceEventEnvelope
        {
            EventType = ComplianceEventType.EvidenceApproved,
            EntityId = entityId,
            EntityType = "Evidence",
            TenantId = Guid.NewGuid(),
            Payload = "{\"evidenceId\":\"" + entityId + "\"}"
        };
        var json = System.Text.Json.JsonSerializer.Serialize(envelope);

        await engine.ExecuteForOutboxAsync("ComplianceEvent.EvidenceApproved", json);

        Assert.Equal(1, matched.CallCount);
        Assert.Equal(entityId, matched.LastEntityId);
    }

    private sealed class RecordingRule : IWorkflowRule
    {
        public RecordingRule(ComplianceEventType eventType) => EventType = eventType;
        public ComplianceEventType EventType { get; }
        public int CallCount { get; private set; }
        public Guid LastEntityId { get; private set; }

        public Task ExecuteAsync(ComplianceEventEnvelope envelope, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastEntityId = envelope.EntityId;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingRule : IWorkflowRule
    {
        public FailingRule(ComplianceEventType eventType) => EventType = eventType;
        public ComplianceEventType EventType { get; }
        public int CallCount { get; private set; }

        public Task ExecuteAsync(ComplianceEventEnvelope envelope, CancellationToken cancellationToken = default)
        {
            CallCount++;
            throw new InvalidOperationException("rule boom");
        }
    }
}
