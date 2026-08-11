using System.Text.Json;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Domain.Entities;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIOrchestratorService.Application.AI.Ace;

public sealed class AceInsightDto
{
    public Guid Id { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? FrameworkId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = "Open";
    public List<AISourceReference> Citations { get; set; } = new();
    public List<string> SuggestedActions { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
}

public interface IAceInsightStore
{
    Task<AceInsightDto> AddAsync(AceInsight entity, CancellationToken cancellationToken);
    Task<IReadOnlyList<AceInsightDto>> ListAsync(Guid hospitalId, string? status, int take, CancellationToken cancellationToken);
    Task<AceInsightDto?> AcknowledgeAsync(Guid insightId, Guid? userId, CancellationToken cancellationToken);
    Task<bool> ExistsOpenAsync(Guid hospitalId, string title, CancellationToken cancellationToken);
}

public sealed class AceInsightStore : IAceInsightStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly AIOrchestratorDbContext _db;

    public AceInsightStore(AIOrchestratorDbContext db)
    {
        _db = db;
    }

    public async Task<AceInsightDto> AddAsync(AceInsight entity, CancellationToken cancellationToken)
    {
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        if (entity.CreatedAtUtc == default) entity.CreatedAtUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(entity.Status)) entity.Status = "Open";
        if (string.IsNullOrWhiteSpace(entity.CitationsJson)) entity.CitationsJson = "[]";
        if (string.IsNullOrWhiteSpace(entity.SuggestedActionsJson)) entity.SuggestedActionsJson = "[]";

        _db.AceInsights.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<AceInsightDto>> ListAsync(Guid hospitalId, string? status, int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);
        var query = _db.AceInsights.AsNoTracking().Where(x => x.HospitalId == hospitalId);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var rows = await query
            .OrderBy(x => x.Priority)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDto).ToList();
    }

    public async Task<AceInsightDto?> AcknowledgeAsync(Guid insightId, Guid? userId, CancellationToken cancellationToken)
    {
        var entity = await _db.AceInsights.FirstOrDefaultAsync(x => x.Id == insightId, cancellationToken);
        if (entity == null) return null;

        entity.Status = "Acknowledged";
        entity.AcknowledgedAtUtc = DateTime.UtcNow;
        entity.AcknowledgedByUserId = userId;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public Task<bool> ExistsOpenAsync(Guid hospitalId, string title, CancellationToken cancellationToken)
    {
        return _db.AceInsights.AsNoTracking().AnyAsync(
            x => x.HospitalId == hospitalId &&
                 x.Status == "Open" &&
                 x.Title == title,
            cancellationToken);
    }

    public static AceInsightDto ToDto(AceInsight entity)
    {
        return new AceInsightDto
        {
            Id = entity.Id,
            HospitalId = entity.HospitalId,
            FrameworkId = entity.FrameworkId,
            Category = entity.Category,
            Title = entity.Title,
            Summary = entity.Summary,
            Reasoning = entity.Reasoning,
            Confidence = entity.Confidence,
            Priority = entity.Priority,
            Status = entity.Status,
            Citations = DeserializeCitations(entity.CitationsJson),
            SuggestedActions = DeserializeActions(entity.SuggestedActionsJson),
            CreatedAtUtc = entity.CreatedAtUtc,
            AcknowledgedAtUtc = entity.AcknowledgedAtUtc,
            AcknowledgedByUserId = entity.AcknowledgedByUserId
        };
    }

    public static string SerializeCitations(IEnumerable<AISourceReference> citations)
        => JsonSerializer.Serialize(citations.ToList(), JsonOptions);

    public static string SerializeActions(IEnumerable<string> actions)
        => JsonSerializer.Serialize(actions.ToList(), JsonOptions);

    private static List<AISourceReference> DeserializeCitations(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<AISourceReference>>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static List<string> DeserializeActions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
