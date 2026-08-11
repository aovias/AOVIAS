using System.Collections.Concurrent;
using System.Text.Json;
using AIOrchestratorService.Application.AI.Autonomy;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Orchestration;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Proactive;

public sealed class ProactiveSuggestionConsumer : BackgroundService
{
    private readonly IAIFeaturePolicy _featurePolicy;
    private readonly IEventRoutingPolicy _eventRoutingPolicy;
    private readonly IProactiveSuggestionStore _store;
    private readonly IOptionsMonitor<AIEventProcessingOptions> _eventOptions;
    private readonly IOptionsMonitor<AIPerformanceOptions> _performanceOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProactiveSuggestionConsumer> _logger;

    private readonly ConcurrentDictionary<string, DateTime> _debounceMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _rateMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _inFlightIntentMap = new(StringComparer.OrdinalIgnoreCase);

    public ProactiveSuggestionConsumer(
        IAIFeaturePolicy featurePolicy,
        IEventRoutingPolicy eventRoutingPolicy,
        IProactiveSuggestionStore store,
        IOptionsMonitor<AIEventProcessingOptions> eventOptions,
        IOptionsMonitor<AIPerformanceOptions> performanceOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<ProactiveSuggestionConsumer> logger)
    {
        _featurePolicy = featurePolicy;
        _eventRoutingPolicy = eventRoutingPolicy;
        _store = store;
        _eventOptions = eventOptions;
        _performanceOptions = performanceOptions;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _eventOptions.CurrentValue;
        if (!options.Enabled || !_featurePolicy.IsProactiveEnabled() || !_featurePolicy.IsEventDrivenAutonomyEnabled())
        {
            _logger.LogInformation("Proactive suggestion consumer disabled by feature flags/options.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.ServiceBusConnectionString))
        {
            _logger.LogWarning("AIEventProcessing enabled without ServiceBusConnectionString.");
            return;
        }

        await using var client = new ServiceBusClient(options.ServiceBusConnectionString);
        await using var processor = client.CreateProcessor(options.TopicName, options.SubscriptionName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Proactive suggestion consumer error.");
            return Task.CompletedTask;
        };

        processor.ProcessMessageAsync += async args =>
        {
            try
            {
                var body = args.Message.Body.ToString();
                var evt = JsonSerializer.Deserialize<ComplianceEventEnvelopeLite>(body);
                if (evt == null || string.IsNullOrWhiteSpace(evt.EventType))
                {
                    await args.CompleteMessageAsync(args.Message, stoppingToken);
                    return;
                }

                var debounceKey = $"{evt.TenantId}:{evt.EntityType}:{evt.EntityId}:{evt.EventType}";
                if (IsDebounced(debounceKey) || IsRateLimited(evt.TenantId))
                {
                    await args.CompleteMessageAsync(args.Message, stoppingToken);
                    return;
                }

                if (!ShouldHandle(evt.EventType))
                {
                    await args.CompleteMessageAsync(args.Message, stoppingToken);
                    return;
                }

                var routeDecision = _eventRoutingPolicy.Decide(new EventRoutingInput
                {
                    TenantId = evt.TenantId,
                    Department = evt.Department ?? string.Empty,
                    Severity = evt.Severity ?? "medium",
                    RecentTriggerCount = evt.RecentTriggerCount,
                    FrequencyScore = evt.RecentTriggerCount,
                    HasInFlightIntent = HasInFlightIntent(evt)
                });
                if (routeDecision == EventRoutingDecision.Skip)
                {
                    await args.CompleteMessageAsync(args.Message, stoppingToken);
                    return;
                }
                if (routeDecision == EventRoutingDecision.Delay)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _performanceOptions.CurrentValue.EventDebounceSeconds)), stoppingToken);
                }

                var request = new AIRequest
                {
                    Prompt = $"Generate proactive suggestion for {evt.EventType}",
                    UseCase = AIUseCase.TaskRecommendation,
                    HospitalId = evt.TenantId,
                    ContextVersionId = evt.ContextVersionId ?? $"{evt.TenantId:N}:{DateTime.UtcNow:yyyyMMddHHmm}",
                    PromptTemplateId = "TaskRecommendation",
                    PromptTemplateVersion = "1.0"
                };

                using var scope = _scopeFactory.CreateScope();
                var orchestrationService = scope.ServiceProvider.GetRequiredService<IAIOrchestrationService>();
                var agentOrchestrator = scope.ServiceProvider.GetRequiredService<IAgentOrchestrator>();

                var response = await orchestrationService.RunAssistiveAsync(request, stoppingToken);
                _store.Add(new ProactiveSuggestionItem
                {
                    EntityType = evt.EntityType,
                    EntityId = evt.EntityId.ToString(),
                    EventType = evt.EventType,
                    Suggestion = response,
                    CreatedAtUtc = DateTime.UtcNow
                });

                var intentKey = $"{evt.TenantId:N}:{evt.EntityType}:{evt.EntityId:N}:{evt.EventType}";
                _inFlightIntentMap[intentKey] = DateTime.UtcNow;
                await agentOrchestrator.StartAsync(new StartWorkflowRequest
                {
                    Goal = $"Auto remediation from {evt.EventType}",
                    AIRequest = request
                }, stoppingToken);
                _inFlightIntentMap.TryRemove(intentKey, out _);

                await args.CompleteMessageAsync(args.Message, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process proactive suggestion event.");
                await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
            }
        };

        await processor.StartProcessingAsync(stoppingToken);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        finally
        {
            await processor.StopProcessingAsync(stoppingToken);
        }
    }

    private bool ShouldHandle(string eventType)
    {
        return string.Equals(eventType, "EvidenceUploaded", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(eventType, "FindingCreated", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(eventType, "ComplianceEvaluated", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(eventType, "RiskUpdated", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDebounced(string key)
    {
        var now = DateTime.UtcNow;
        var debounceWindow = TimeSpan.FromSeconds(Math.Max(1, _performanceOptions.CurrentValue.EventDebounceSeconds));
        if (_debounceMap.TryGetValue(key, out var lastSeen) && now - lastSeen < debounceWindow)
        {
            return true;
        }

        _debounceMap[key] = now;
        return false;
    }

    private bool IsRateLimited(Guid tenantId)
    {
        var now = DateTime.UtcNow;
        var maxPerMinute = Math.Max(1, _performanceOptions.CurrentValue.MaxInvocationsPerMinutePerTenant);
        var queue = _rateMap.GetOrAdd(tenantId.ToString("N"), _ => new Queue<DateTime>());
        lock (queue)
        {
            while (queue.Count > 0 && now - queue.Peek() > TimeSpan.FromMinutes(1))
            {
                queue.Dequeue();
            }

            if (queue.Count >= maxPerMinute)
            {
                return true;
            }

            queue.Enqueue(now);
            return false;
        }
    }

    private bool HasInFlightIntent(ComplianceEventEnvelopeLite evt)
    {
        var now = DateTime.UtcNow;
        var key = $"{evt.TenantId:N}:{evt.EntityType}:{evt.EntityId:N}:{evt.EventType}";
        if (!_inFlightIntentMap.TryGetValue(key, out var startedAtUtc))
        {
            return false;
        }

        if (now - startedAtUtc > TimeSpan.FromMinutes(30))
        {
            _inFlightIntentMap.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    private sealed class ComplianceEventEnvelopeLite
    {
        public string EventType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string? ContextVersionId { get; set; }
        public string? Department { get; set; }
        public string? Severity { get; set; }
        public int RecentTriggerCount { get; set; }
    }
}
