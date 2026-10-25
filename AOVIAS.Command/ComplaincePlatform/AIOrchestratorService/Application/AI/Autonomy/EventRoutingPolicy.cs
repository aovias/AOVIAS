using AIOrchestratorService.Application.AI.Configuration;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Autonomy;

public enum EventRoutingDecision
{
    Trigger = 1,
    Skip = 2,
    Delay = 3
}

public sealed class EventRoutingInput
{
    public Guid TenantId { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int RecentTriggerCount { get; set; }
    public bool HasInFlightIntent { get; set; }
    public int FrequencyScore { get; set; }
}

public interface IEventRoutingPolicy
{
    EventRoutingDecision Decide(EventRoutingInput input);
}

public sealed class EventRoutingPolicy : IEventRoutingPolicy
{
    private readonly IOptionsMonitor<AutonomyControlOptions> _options;

    public EventRoutingPolicy(IOptionsMonitor<AutonomyControlOptions> options)
    {
        _options = options;
    }

    public EventRoutingDecision Decide(EventRoutingInput input)
    {
        var options = _options.CurrentValue;
        if (!options.EnableEventDrivenAutonomy)
        {
            return EventRoutingDecision.Skip;
        }

        if (input.HasInFlightIntent)
        {
            return EventRoutingDecision.Skip;
        }

        if (input.RecentTriggerCount >= options.MaxEventTriggersPerEntityPerHour)
        {
            return EventRoutingDecision.Delay;
        }

        if (input.FrequencyScore > options.MaxEventTriggersPerEntityPerHour)
        {
            return EventRoutingDecision.Delay;
        }

        if (string.Equals(input.Severity, "high", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(input.Severity, "critical", StringComparison.OrdinalIgnoreCase))
        {
            return EventRoutingDecision.Trigger;
        }

        return EventRoutingDecision.Delay;
    }
}
