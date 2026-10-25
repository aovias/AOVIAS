using ComplianceService.Application.Analytics;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.AI;

/// <summary>Summarized facts and sources contributed by a domain context provider (Sprint 12C).</summary>
public sealed class AIContextContribution
{
    public Dictionary<string, string> Facts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<AIContextSourceDto> Sources { get; init; } = new();
}

/// <summary>Resolved scope and request passed to each AI context provider.</summary>
public sealed class AIContextBuildContext
{
    public required AIContextBundleRequestDto Request { get; init; }
    public required AnalyticsScope Scope { get; init; }
}
