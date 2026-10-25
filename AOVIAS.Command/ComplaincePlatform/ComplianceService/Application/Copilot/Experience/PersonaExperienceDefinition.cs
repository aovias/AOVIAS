using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Experience;

/// <summary>
/// Sprint 13E — static persona experience catalog entry.
/// Describes how each persona mode surfaces existing recommendations, drafts, and proposals.
/// </summary>
public sealed class PersonaExperienceDefinition
{
    public required CopilotPersona Persona { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string Icon { get; init; }
    public required string Theme { get; init; }
    public IReadOnlyList<string> PrimaryObjectives { get; init; } = [];
    public IReadOnlyList<string> RecommendedActions { get; init; } = [];
    public IReadOnlyList<string> DefaultQuestions { get; init; } = [];
    public IReadOnlyList<RecommendationCategory> PriorityCategories { get; init; } = [];
    public IReadOnlyList<DraftType> SupportedDrafts { get; init; } = [];
    public IReadOnlyList<ProposalType> SupportedProposals { get; init; } = [];
}
