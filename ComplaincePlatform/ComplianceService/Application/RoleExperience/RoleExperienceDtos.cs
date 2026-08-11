namespace ComplianceService.Application.RoleExperience;

public sealed class RoleExperienceHomeDto
{
    public required string PackId { get; init; }
    public required string Persona { get; init; }
    public required string DefaultHomeRoute { get; init; }
    public IReadOnlyList<RoleExperiencePriorityDto> Priorities { get; init; } = [];
    public RoleExperienceAttentionCountsDto AttentionCounts { get; init; } = new();
    public IReadOnlyList<RoleExperienceQuickActionDto> QuickActions { get; init; } = [];
    public IReadOnlyList<string> WidgetAllowList { get; init; } = [];
    public IReadOnlyList<string> NavSectionIds { get; init; } = [];
}

public sealed class RoleExperiencePriorityDto
{
    public required string Title { get; init; }
    public string? Rationale { get; init; }
    public string? Href { get; init; }
    public string Severity { get; init; } = "Medium";
}

public sealed class RoleExperienceAttentionCountsDto
{
    public int OverdueTasks { get; init; }
    public int PendingReviews { get; init; }
    public int CriticalCapas { get; init; }
    public int UpcomingAudits { get; init; }
}

public sealed class RoleExperienceQuickActionDto
{
    public required string Label { get; init; }
    public required string Href { get; init; }
}
