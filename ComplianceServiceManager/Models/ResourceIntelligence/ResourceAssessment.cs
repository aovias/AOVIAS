using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Models.ResourceIntelligence;

public sealed class RuntimeProfileResourceEstimate
{
    public string ProfileId { get; init; } = string.Empty;

    public string ProfileName { get; init; } = string.Empty;

    public int ExpectedProcessCount { get; init; }

    public RuntimeProfileResourceClass MemoryClass { get; init; } = RuntimeProfileResourceClass.Unknown;

    public RuntimeProfileResourceClass CpuClass { get; init; } = RuntimeProfileResourceClass.Unknown;

    public string MemoryClassDisplay => OperatorLabels.ResourceLevel(MemoryClass);

    public string Explanation { get; init; } = string.Empty;
}

public sealed class ResourceRecommendation
{
    public string Id { get; init; } = string.Empty;

    public ResourceRecommendationKind Kind { get; init; }

    public ResourceRecommendationSeverity Severity { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Explanation { get; init; } = string.Empty;

    public string? SuggestedAction { get; init; }

    public string? RelatedProfileId { get; init; }

    public string? RelatedProfileName { get; init; }

    public string? RelatedSessionId { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class SessionResourceContribution
{
    public string SessionId { get; init; } = string.Empty;

    public string AgentDisplay { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string RuntimeProfileDisplay { get; init; } = string.Empty;

    public string StatusDisplay { get; init; } = string.Empty;
}

public sealed class ResourceAssessment
{
    public DateTimeOffset Timestamp { get; init; }

    public double? CpuPercent { get; init; }

    public double? CpuWindowAveragePercent { get; init; }

    public double? MemoryPercent { get; init; }

    public long MemoryAvailableBytes { get; init; }

    public long MemoryUsedBytes { get; init; }

    public long MemoryTotalBytes { get; init; }

    public double? DiskPercent { get; init; }

    public ResourcePressureLevel CpuPressure { get; init; } = ResourcePressureLevel.Unknown;

    public ResourcePressureLevel MemoryPressure { get; init; } = ResourcePressureLevel.Unknown;

    public ResourcePressureLevel DiskPressure { get; init; } = ResourcePressureLevel.Unknown;

    public ResourcePressureLevel OverallPressure { get; init; } = ResourcePressureLevel.Unknown;

    public int ActiveServiceCount { get; init; }

    public int ActiveSessionCount { get; init; }

    public int ActiveProfileCount { get; init; }

    public IReadOnlyList<SessionResourceContribution> ActiveSessions { get; init; } = [];

    public IReadOnlyList<RuntimeProfileResourceEstimate> ProfileEstimates { get; init; } = [];

    public IReadOnlyList<ResourceRecommendation> Recommendations { get; init; } = [];

    public string Summary { get; init; } = string.Empty;
}
