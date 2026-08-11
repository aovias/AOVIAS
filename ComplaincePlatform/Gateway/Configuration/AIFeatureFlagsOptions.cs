namespace Gateway.Configuration;

public sealed class AIFeatureFlagsOptions
{
    public const string SectionName = "AIFeatureFlags";

    public bool EnableAIRead { get; set; }
    public bool EnableAIAssist { get; set; }
    public bool EnableAIAction { get; set; }
    public bool EnableAIProactive { get; set; }
    public bool EnableAutonomousWorkflows { get; set; }
    public bool EnableEventDrivenAutonomy { get; set; }
    public bool EnableAutoExecution { get; set; }
}
