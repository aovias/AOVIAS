using AIOrchestratorService.Application.AI.Configuration;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Governance;

public interface IAIFeaturePolicy
{
    bool IsReadEnabled();
    bool IsAssistEnabled();
    bool IsActionEnabled();
    bool IsProactiveEnabled();
    bool IsAutonomyEnabled();
    bool IsEventDrivenAutonomyEnabled();
    bool IsAutoExecutionEnabled();
}

public sealed class AIFeaturePolicy : IAIFeaturePolicy
{
    private readonly IOptionsMonitor<AIFeatureFlagsOptions> _flags;

    public AIFeaturePolicy(IOptionsMonitor<AIFeatureFlagsOptions> flags)
    {
        _flags = flags;
    }

    public bool IsReadEnabled() => _flags.CurrentValue.EnableAIRead;
    public bool IsAssistEnabled() => _flags.CurrentValue.EnableAIAssist;
    public bool IsActionEnabled() => _flags.CurrentValue.EnableAIAction;
    public bool IsProactiveEnabled() => _flags.CurrentValue.EnableAIProactive;
    public bool IsAutonomyEnabled() => _flags.CurrentValue.EnableAutonomousWorkflows;
    public bool IsEventDrivenAutonomyEnabled() => _flags.CurrentValue.EnableEventDrivenAutonomy;
    public bool IsAutoExecutionEnabled() => _flags.CurrentValue.EnableAutoExecution;
}
