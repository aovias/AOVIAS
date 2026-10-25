namespace ComplianceService.Application.DTOs;

/// <summary>Request for entity-scoped explainability (Sprint 12C Phase 8).</summary>
public sealed class AIExplainContextRequestDto
{
    public Guid HospitalId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

public sealed class AIExplainContextResponseDto
{
    public string Topic { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<string> KeyPoints { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Blockers { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> RecommendedActions { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Facts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AIReadOnlyToolRequestDto
{
    public Guid HospitalId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AIReadOnlyToolResponseDto
{
    public string ToolName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, string> Facts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
