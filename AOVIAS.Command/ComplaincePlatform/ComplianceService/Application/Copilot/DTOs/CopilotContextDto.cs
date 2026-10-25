namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>
/// Request DTO carrying Copilot session context from the Gateway/frontend.
/// Maps to the CopilotContext domain model after validation.
/// </summary>
public sealed class CopilotContextDto
{
    public string PageContext { get; init; } = string.Empty;
    public Guid? FocusedEntityId { get; init; }
    public string? FocusedEntityType { get; init; }
    public string? ConversationSummary { get; init; }
    public IReadOnlyList<string>? RecentEntityRefs { get; init; }

    /// <summary>
    /// Optional manual persona override. Null means auto-route.
    /// Valid values: "AccreditationCoach", "QualityManagerAssistant", "EvidenceAdvisor",
    /// "CapaAdvisor", "ExecutiveAdvisor", "DepartmentHeadAssistant", "AuditorAssistant".
    /// </summary>
    public string? PersonaOverride { get; init; }

    public string? CurrentStage { get; init; }
    public decimal? ReadinessScore { get; init; }
    public string? AnalyticsSummary { get; init; }
}
