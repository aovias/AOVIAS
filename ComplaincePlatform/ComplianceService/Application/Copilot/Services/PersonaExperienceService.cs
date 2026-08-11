using ComplianceService.Application.Analytics;
using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Experience;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>Sprint 13E — persona workspace orchestration (no new AI rules).</summary>
public sealed class PersonaExperienceService : IPersonaExperienceService
{
    private readonly ICopilotRecommendationRepository _recommendations;
    private readonly IAiDraftRepository _drafts;
    private readonly ICopilotActionProposalRepository _proposals;
    private readonly PersonaLandingStateBuilder _landingBuilder;
    private readonly ICopilotActivityService _activity;

    public PersonaExperienceService(
        ICopilotRecommendationRepository recommendations,
        IAiDraftRepository drafts,
        ICopilotActionProposalRepository proposals,
        PersonaLandingStateBuilder landingBuilder,
        ICopilotActivityService activity)
    {
        _recommendations = recommendations;
        _drafts          = drafts;
        _proposals       = proposals;
        _landingBuilder  = landingBuilder;
        _activity        = activity;
    }

    public IReadOnlyList<PersonaExperienceDefinitionDto> GetExperienceCatalog() =>
        PersonaExperienceRegistry.GetAll().Select(MapDefinition).ToList();

    public async Task<PersonaWorkspaceDto> GetWorkspaceAsync(
        CopilotPersona persona,
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var definition = PersonaExperienceRegistry.Get(persona);
        var scope = AnalyticsScope.ForHospital(organizationId, hospitalId, frameworkId);
        var personaName = persona.ToString();

        var allRecs = await _recommendations.SearchAsync(organizationId, cancellationToken: ct);
        var proposed = allRecs
            .Where(r => r.Status == RecommendationStatus.Proposed)
            .Where(r => r.Persona == personaName || MatchesPriorityCategory(r, definition))
            .OrderByDescending(r => CategoryScore(r.Category, definition))
            .ThenBy(r => r.Priority)
            .Take(8)
            .ToList();

        var topRec = proposed.FirstOrDefault();
        var landingParts = await _landingBuilder.BuildAsync(persona, scope, topRec, ct);

        var allDrafts = await _drafts.SearchAsync(organizationId, cancellationToken: ct);
        var highlightedDrafts = allDrafts
            .Where(d => definition.SupportedDrafts.Contains(d.DraftType))
            .Where(d => d.Persona == personaName || definition.SupportedDrafts.Contains(d.DraftType))
            .OrderByDescending(d => d.GeneratedAtUtc)
            .Take(6)
            .Select(CopilotDraftServiceMap)
            .ToList();

        var allProposals = await _proposals.SearchAsync(organizationId, pageSize: 100, ct: ct);
        var highlightedProposals = allProposals
            .Where(p => definition.SupportedProposals.Contains(p.ProposalType))
            .OrderByDescending(p => p.ProposedAtUtc)
            .Take(6)
            .Select(ActionProposalGenerator.MapToDto)
            .ToList();

        await _activity.RecordAsync(
            organizationId,
            CopilotActivityEvent.EventTypes.WorkspaceViewed,
            personaName,
            ct: ct);

        return new PersonaWorkspaceDto
        {
            Definition = MapDefinition(definition),
            Landing = new PersonaLandingStateDto
            {
                Persona = personaName,
                Headline = landingParts.Headline,
                Subheadline = landingParts.Subheadline,
                FocusArea = landingParts.Focus,
                ReadinessScore = landingParts.Readiness,
                TopRecommendationTitle = topRec?.Title,
                KeyKpiLabel = landingParts.KpiLabel,
                KeyKpiValue = landingParts.KpiValue,
            },
            PrioritizedRecommendations = proposed.Select(CopilotRecommendationServiceMap).ToList(),
            HighlightedDrafts = highlightedDrafts,
            HighlightedProposals = highlightedProposals,
            SuggestedQuestions = definition.DefaultQuestions,
        };
    }

    private static bool MatchesPriorityCategory(CopilotRecommendation r, PersonaExperienceDefinition def) =>
        def.PriorityCategories.Contains(r.Category);

    private static int CategoryScore(RecommendationCategory category, PersonaExperienceDefinition def)
    {
        for (var i = 0; i < def.PriorityCategories.Count; i++)
        {
            if (def.PriorityCategories[i] == category)
                return def.PriorityCategories.Count - i;
        }
        return 0;
    }

    private static PersonaExperienceDefinitionDto MapDefinition(PersonaExperienceDefinition d) =>
        new()
        {
            Persona = d.Persona.ToString(),
            DisplayName = d.DisplayName,
            Description = d.Description,
            Icon = d.Icon,
            Theme = d.Theme,
            PrimaryObjectives = d.PrimaryObjectives,
            RecommendedActions = d.RecommendedActions,
            DefaultQuestions = d.DefaultQuestions,
            PriorityCategories = d.PriorityCategories.Select(c => c.ToString()).ToList(),
            SupportedDraftTypes = d.SupportedDrafts.Select(t => (int)t).ToList(),
            SupportedProposalTypes = d.SupportedProposals.Select(t => (int)t).ToList(),
        };

    // Local mappers — avoid widening access on other services
    private static CopilotRecommendationDto CopilotRecommendationServiceMap(CopilotRecommendation r) =>
        new()
        {
            Id = r.Id,
            OrganizationId = r.OrganizationId,
            RecommendationType = r.RecommendationType,
            Category = r.Category.ToString(),
            Severity = r.Severity.ToString(),
            Confidence = r.Confidence,
            Persona = r.Persona,
            Priority = r.Priority,
            Title = r.Title,
            Description = r.Description,
            Rationale = r.Rationale,
            ExpectedOutcome = r.ExpectedOutcome,
            RiskIfIgnored = r.RiskIfIgnored,
            Status = r.Status.ToString(),
            DeepLink = r.DeepLink,
            GeneratedAtUtc = r.GeneratedAtUtc,
            SuggestedDraftAction = RecommendationToDraftMapper.GetActionLabel(
                RecommendationToDraftMapper.MapToDraftType(r.Category)),
        };

    private static AiDraftDto CopilotDraftServiceMap(AiDraft d) =>
        new()
        {
            Id = d.Id,
            OrganizationId = d.OrganizationId,
            GroupId = d.GroupId,
            Title = d.Title,
            Content = d.Content,
            DraftType = d.DraftType.ToString(),
            DraftTypeEnum = (int)d.DraftType,
            Persona = d.Persona,
            Status = d.Status.ToString(),
            Version = d.Version,
            Summary = d.Summary,
            GeneratedBy = d.GeneratedBy,
            Confidence = d.Confidence,
            GeneratedAtUtc = d.GeneratedAtUtc,
            CreatedUtc = d.CreatedUtc,
            UpdatedUtc = d.UpdatedUtc,
        };
}
