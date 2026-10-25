namespace ComplianceService.Application.Copilot.DTOs;

/// <summary>Describes a single Copilot persona for catalog and active-persona API responses.</summary>
public sealed class PersonaResponseDto
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string[] PageContexts { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; }
    public Guid? AiProfileId { get; init; }
}
