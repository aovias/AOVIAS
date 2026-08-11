using Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace Gateway.Services;

public sealed class AIFeaturePolicy : IAIFeaturePolicy
{
    private readonly IOptionsMonitor<AIFeatureFlagsOptions> _options;

    public AIFeaturePolicy(IOptionsMonitor<AIFeatureFlagsOptions> options)
    {
        _options = options;
    }

    public bool CanUseRead() => _options.CurrentValue.EnableAIRead;

    public bool CanUseAssist() => _options.CurrentValue.EnableAIAssist;

    public bool CanUseAction() => _options.CurrentValue.EnableAIAction;

    public bool CanUseProactive() => _options.CurrentValue.EnableAIProactive;

    public bool CanUseAutonomousWorkflows() => _options.CurrentValue.EnableAutonomousWorkflows;

    public bool CanUseEventDrivenAutonomy() => _options.CurrentValue.EnableEventDrivenAutonomy;

    public bool CanUseAutoExecution() => _options.CurrentValue.EnableAutoExecution;
}
