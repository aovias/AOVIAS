using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Application service for Copilot recommendations.
/// Sprint 13B: wires the rule engine generation pipeline.
/// </summary>
public sealed class CopilotRecommendationService : ICopilotRecommendationService
{
    private readonly ICopilotRecommendationRepository _repository;
    private readonly IRecommendationEngine _engine;

    public CopilotRecommendationService(
        ICopilotRecommendationRepository repository,
        IRecommendationEngine engine)
    {
        _repository = repository;
        _engine     = engine;
    }

    public async Task<IReadOnlyList<CopilotRecommendationDto>> GetRecommendationsAsync(
        Guid organizationId,
        CopilotContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.SearchAsync(organizationId, cancellationToken: cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<CopilotRecommendationDto>> GetByPersonaAsync(
        Guid organizationId,
        string persona,
        int maxCount = 5,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetByPersonaAsync(
            organizationId, persona,
            status: Domain.Copilot.Enums.RecommendationStatus.Proposed,
            maxCount: maxCount,
            cancellationToken: cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<CopilotRecommendationDto?> GetRecommendationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetAsync(id, cancellationToken);
        return item is null ? null : MapToDto(item);
    }

    public async Task<IReadOnlyList<CopilotRecommendationDto>> RefreshAsync(
        Guid organizationId,
        Guid hospitalId,
        RefreshRecommendationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var recommendations = await _engine.GenerateAsync(
            organizationId,
            hospitalId,
            request.FrameworkId,
            request,
            cancellationToken);

        return recommendations.Select(MapToDto).ToList();
    }

    public async Task AcceptAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _repository.AcceptAsync(id, cancellationToken);

    public async Task DismissAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _repository.DismissAsync(id, cancellationToken);

    private static CopilotRecommendationDto MapToDto(CopilotRecommendation r) =>
        new()
        {
            Id                 = r.Id,
            OrganizationId     = r.OrganizationId,
            RecommendationType = r.RecommendationType,
            Category           = r.Category.ToString(),
            Severity           = r.Severity.ToString(),
            Confidence         = r.Confidence,
            Persona            = r.Persona,
            Priority           = r.Priority,
            Title              = r.Title,
            Description        = r.Description,
            Rationale          = r.Rationale,
            ExpectedOutcome    = r.ExpectedOutcome,
            RiskIfIgnored      = r.RiskIfIgnored,
            SourceReferences   = r.SourceReferences,
            SourceEntityType   = r.SourceEntityType,
            SourceEntityId     = r.SourceEntityId,
            DeepLink           = r.DeepLink,
            RationaleJson      = r.RationaleJson,
            ImpactJson         = r.ImpactJson,
            SuggestedDraftAction = RecommendationToDraftMapper.GetActionLabel(
                RecommendationToDraftMapper.MapToDraftType(r.Category)),
            Status             = r.Status.ToString(),
            GeneratedAtUtc     = r.GeneratedAtUtc,
            ExpiresAtUtc       = r.ExpiresAtUtc,
            AcceptedAtUtc      = r.AcceptedAtUtc,
            DismissedAtUtc     = r.DismissedAtUtc,
        };
}
