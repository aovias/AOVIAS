namespace Gateway.Configuration;

/// <summary>CRP Phase 5 — Gateway-side trial lifecycle enforcement (mirrors Org TrialLifecycle).</summary>
public sealed class TrialLifecycleGatewayOptions
{
    public const string SectionName = "TrialLifecycle";

    public bool EnforceReadOnly { get; set; } = true;
}
