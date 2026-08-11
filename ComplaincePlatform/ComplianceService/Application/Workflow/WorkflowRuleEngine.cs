using System.Text.Json;
using ComplianceService.Application.Events;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Workflow;

public sealed class WorkflowRuleEngine : IWorkflowRuleEngine
{
    private readonly IEnumerable<IWorkflowRule> _rules;
    private readonly ILogger<WorkflowRuleEngine> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WorkflowRuleEngine(IEnumerable<IWorkflowRule> rules, ILogger<WorkflowRuleEngine> logger)
    {
        _rules = rules;
        _logger = logger;
    }

    public async Task ExecuteAsync(ComplianceEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var matching = _rules.Where(r => r.EventType == envelope.EventType).ToList();
        if (matching.Count == 0)
        {
            return;
        }

        foreach (var rule in matching)
        {
            try
            {
                await rule.ExecuteAsync(envelope, cancellationToken);
            }
            catch (Exception ex)
            {
                // Workflow automation must not fail the originating domain transaction.
                _logger.LogWarning(
                    ex,
                    "Workflow rule {Rule} failed for event {EventType} entity {EntityId}",
                    rule.GetType().Name,
                    envelope.EventType,
                    envelope.EntityId);
            }
        }
    }

    public async Task ExecuteForOutboxAsync(string outboxType, string payloadJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outboxType)
            || !outboxType.StartsWith("ComplianceEvent.", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ComplianceEventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ComplianceEventEnvelope>(payloadJson, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize ComplianceEvent envelope from outbox type {Type}", outboxType);
            return;
        }

        if (envelope == null)
        {
            return;
        }

        await ExecuteAsync(envelope, cancellationToken);
    }
}
