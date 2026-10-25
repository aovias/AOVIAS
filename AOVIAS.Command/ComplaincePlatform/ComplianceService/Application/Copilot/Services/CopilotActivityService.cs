using ComplianceService.Application.Copilot.Contracts;
using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Services;

public sealed class CopilotActivityService : ICopilotActivityService
{
    private readonly ICopilotActivityRepository _repository;

    public CopilotActivityService(ICopilotActivityRepository repository) =>
        _repository = repository;

    public async Task RecordAsync(
        Guid organizationId,
        string eventType,
        string? persona,
        Guid? actorId = null,
        string? actorEmail = null,
        string? entityType = null,
        Guid? entityId = null,
        string? notes = null,
        CancellationToken ct = default)
    {
        var activity = CopilotActivityEvent.Record(
            organizationId,
            eventType,
            persona,
            actorId?.ToString(),
            actorEmail,
            entityType,
            entityId,
            notes);

        await _repository.RecordAsync(activity, ct);
        await _repository.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CopilotActivityEventDto>> GetFeedAsync(
        Guid organizationId,
        int limit = 50,
        CancellationToken ct = default)
    {
        var events = await _repository.GetRecentAsync(organizationId, limit, ct);
        return events.Select(e => new CopilotActivityEventDto
        {
            Id = e.Id,
            EventType = e.EventType,
            Persona = e.Persona,
            ActorId = e.ActorId,
            ActorEmail = e.ActorEmail,
            EntityType = e.EntityType,
            EntityId = e.EntityId,
            Notes = e.Notes,
            OccurredAtUtc = e.OccurredAtUtc,
        }).ToList();
    }

    public async Task<CopilotUsageMetricsDto> GetMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var events = await _repository.GetRecentAsync(organizationId, 500, ct);

        var personaCounts = events
            .Where(e => !string.IsNullOrEmpty(e.Persona))
            .GroupBy(e => e.Persona!)
            .ToDictionary(g => g.Key, g => g.Count());

        return new CopilotUsageMetricsDto
        {
            PersonaUsageCounts = personaCounts,
            DraftsGenerated = events.Count(e => e.EventType == CopilotActivityEvent.EventTypes.DraftCreated),
            ProposalsApproved = events.Count(e => e.EventType == CopilotActivityEvent.EventTypes.ProposalApproved),
            ProposalsExecuted = events.Count(e => e.EventType == CopilotActivityEvent.EventTypes.ProposalExecuted),
            RecommendationsEngaged = events.Count(e =>
                e.EventType == CopilotActivityEvent.EventTypes.RecommendationViewed ||
                e.EventType == CopilotActivityEvent.EventTypes.RecommendationGenerated),
            WorkspaceViews = events.Count(e => e.EventType == CopilotActivityEvent.EventTypes.WorkspaceViewed),
            PersonaChanges = events.Count(e => e.EventType == CopilotActivityEvent.EventTypes.PersonaChanged),
            MetricsAsOfUtc = DateTime.UtcNow,
        };
    }
}
