namespace AIOrchestratorService.Application.AI.Configuration;

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

public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-10-21";
    public string DefaultDeployment { get; set; } = string.Empty;
    public Dictionary<string, string> TenantDeploymentOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SarvamOptions
{
    public const string SectionName = "Sarvam";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.sarvam.ai";
    public string Model { get; set; } = "sarvam-30b";
    public string AuthMode { get; set; } = "Bearer";
    public int TimeoutSeconds { get; set; } = 20;
    public int MaxTokensPerRequest { get; set; } = 1024;
    public int RetryCount { get; set; } = 1;
    public bool EnableFallbackToAzure { get; set; } = true;
}

public sealed class AIProviderRoutingOptions
{
    public const string SectionName = "AIProviderRouting";

    /// <summary>
    /// Active chat provider. Supported: <c>AzureOpenAI</c> (default), <c>Sarvam</c>.
    /// Stub adapters (deterministic fallback only): <c>Anthropic</c>, <c>Ollama</c>.
    /// Also readable from top-level config key <c>AIProvider</c>.
    /// </summary>
    public string AIProvider { get; set; } = "AzureOpenAI";

    /// <summary>Fallback when primary fails (typically AzureOpenAI).</summary>
    public string FallbackProvider { get; set; } = "AzureOpenAI";

    public List<string> D1AllowedUseCases { get; set; } = new()
    {
        "ExplainCompliance",
        "QueryCompliance",
        "SuggestMapping",
        "ValidateEvidence",
        "TaskRecommendation"
    };
}

public sealed class AIRetrievalPolicyOptions
{
    public const string SectionName = "AIRetrievalPolicy";

    public int MaxFindings { get; set; } = 20;
    public int MaxEvidence { get; set; } = 20;
    public int MaxTasks { get; set; } = 20;
    public int MaxKBNodes { get; set; } = 20;
    public int MaxObjectiveElements { get; set; } = 5;
    public int MaxEvidenceItems { get; set; } = 3;
    public int MaxFindingsItems { get; set; } = 3;
    public int MaxContextTokens { get; set; } = 1200;
}

public sealed class AIConfidencePolicyOptions
{
    public const string SectionName = "AIConfidencePolicy";

    public decimal SuggestOnlyThreshold { get; set; } = 0.6m;
    public decimal WarnThreshold { get; set; } = 0.8m;
}

public sealed class AIPerformanceOptions
{
    public const string SectionName = "AIPerformance";

    public int RequestTimeoutMs { get; set; } = 5000;
    public int CacheTtlSeconds { get; set; } = 60;
    public int EventDebounceSeconds { get; set; } = 20;
    public int MaxInvocationsPerMinutePerTenant { get; set; } = 50;
}

public sealed class AIUsagePolicyOptions
{
    public const string SectionName = "AIUsagePolicy";

    public int MaxRequestsPerSession { get; set; } = 50;
    public int SessionWindowMinutes { get; set; } = 60;
    public int PerTenantRateLimitPerMinute { get; set; } = 60;
}

public sealed class AIEventProcessingOptions
{
    public const string SectionName = "AIEventProcessing";

    public bool Enabled { get; set; }
    public string ServiceBusConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "compliance-events";
    public string SubscriptionName { get; set; } = "ai-orchestrator-suggestions";
}

public sealed class AutonomyControlOptions
{
    public const string SectionName = "AutonomyControl";

    public bool EnableAutonomousWorkflows { get; set; }
    public bool EnableEventDrivenAutonomy { get; set; }
    public bool EnableAutoExecution { get; set; }
    public bool ForceApprovalForAllActions { get; set; }
    public string RolloutStage { get; set; } = "D0";
    public int MaxStepsPerWorkflow { get; set; } = 25;
    public int MaxActionsPerSession { get; set; } = 50;
    public int WorkflowTimeoutSeconds { get; set; } = 600;
    public int StepTimeoutSeconds { get; set; } = 90;
    public int ApprovalTimeoutSeconds { get; set; } = 1800;
    public int RetryBudgetPerStep { get; set; } = 2;
    public int StuckWorkflowThresholdSeconds { get; set; } = 180;
    public int MaxEventTriggersPerEntityPerHour { get; set; } = 5;
}
