using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Services;

/// <summary>
/// Application service for Copilot drafts.
/// Sprint 13C: delegates generation to <see cref="IDraftGenerationService"/>,
/// exposes read + versioning + lifecycle operations.
/// </summary>
public sealed class CopilotDraftService : ICopilotDraftService
{
    private readonly IAiDraftRepository _repository;
    private readonly IDraftGenerationService _generationService;

    public CopilotDraftService(
        IAiDraftRepository repository,
        IDraftGenerationService generationService)
    {
        _repository        = repository;
        _generationService = generationService;
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AiDraftDto>> GetDraftsAsync(
        Guid organizationId,
        CopilotContext? context = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _repository.SearchAsync(
            organizationId, cancellationToken: cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<AiDraftDto?> GetDraftAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetAsync(id, cancellationToken);
        return item is null ? null : MapToDto(item);
    }

    public Task<IReadOnlyList<AiDraftDto>> GetVersionsAsync(
        Guid draftId,
        CancellationToken cancellationToken = default) =>
        _generationService.GetVersionsAsync(draftId, cancellationToken);

    // ── Generate ──────────────────────────────────────────────────────────────

    public Task<AiDraftDto> GenerateDraftAsync(
        Guid organizationId,
        GenerateDraftRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<DraftType>(request.DraftType.ToString(), out var draftType))
            throw new ArgumentException($"Invalid DraftType value: {request.DraftType}");

        if (!Enum.TryParse<DraftSourceType>(request.SourceType.ToString(), out var sourceType))
            throw new ArgumentException($"Invalid SourceType value: {request.SourceType}");

        var genRequest = new DraftGenerationRequest
        {
            DraftType              = draftType,
            SourceType             = sourceType,
            Persona                = request.Persona,
            OrganizationId         = organizationId,
            RecommendationId       = request.RecommendationId,
            SourceEntityId         = request.SourceEntityId,
            AdditionalInstructions = request.AdditionalInstructions,
        };

        return _generationService.GenerateAsync(genRequest, cancellationToken);
    }

    public Task<AiDraftDto> RegenerateDraftAsync(
        Guid draftId,
        Guid organizationId,
        CancellationToken cancellationToken = default) =>
        _generationService.RegenerateAsync(draftId, cancellationToken: cancellationToken);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public async Task UpdateDraftAsync(
        Guid id,
        string newContent,
        CancellationToken cancellationToken = default)
    {
        var draft = await _repository.GetAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Draft {id} not found.");

        draft.UpdateContent(newContent);
        await _repository.UpdateAsync(draft, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveDraftAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _repository.ArchiveAsync(id, cancellationToken);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static AiDraftDto MapToDto(AiDraft d) =>
        new()
        {
            Id               = d.Id,
            GroupId          = d.GroupId,
            OrganizationId   = d.OrganizationId,
            DraftType        = d.DraftType.ToString(),
            DraftTypeEnum    = (int)d.DraftType,
            SourceType       = d.SourceType.ToString(),
            SourceEntityId   = d.SourceEntityId,
            RecommendationId = d.RecommendationId,
            Persona          = d.Persona,
            Title            = d.Title,
            Summary          = d.Summary,
            Content          = d.Content,
            GeneratedBy      = d.GeneratedBy,
            Confidence       = d.Confidence,
            MetadataJson     = d.MetadataJson,
            Status           = d.Status.ToString(),
            Version          = d.Version,
            GeneratedAtUtc   = d.GeneratedAtUtc,
            CreatedUtc       = d.CreatedUtc,
            UpdatedUtc       = d.UpdatedUtc,
        };
}
