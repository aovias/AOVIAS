using System.Text;
using System.Text.RegularExpressions;
using AIOrchestratorService.Domain.Entities;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIOrchestratorService.Application.AI.Memory;

public interface IConversationMemoryService
{
    /// <summary>
    /// Loads the last N messages for the user+hospital conversation and returns a compact summary.
    /// </summary>
    Task<string?> LoadSummaryAsync(Guid hospitalId, Guid? userId, int maxMessages = 12, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a user/assistant turn (PII-trimmed) to durable conversation memory.
    /// </summary>
    Task SaveTurnAsync(
        Guid hospitalId,
        Guid? userId,
        Guid? profileId,
        string userPrompt,
        string assistantReply,
        CancellationToken cancellationToken = default);
}

public sealed class ConversationMemoryService : IConversationMemoryService
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneRegex = new(
        @"(?:\+?\d{1,3}[\s\-]?)?(?:\(?\d{2,4}\)?[\s\-]?)?\d{3,4}[\s\-]?\d{3,4}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly AIOrchestratorDbContext _db;
    private readonly ILogger<ConversationMemoryService> _logger;

    public ConversationMemoryService(AIOrchestratorDbContext db, ILogger<ConversationMemoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string?> LoadSummaryAsync(
        Guid hospitalId,
        Guid? userId,
        int maxMessages = 12,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty)
        {
            return null;
        }

        var conversation = await FindConversationAsync(hospitalId, userId, cancellationToken);
        if (conversation == null)
        {
            return null;
        }

        var take = Math.Clamp(maxMessages, 2, 40);
        var messages = await _db.AIConversationMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.Sequence)
            .Take(take)
            .OrderBy(m => m.Sequence)
            .Select(m => new { m.Role, m.Content })
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return null;
        }

        var sb = new StringBuilder();
        foreach (var message in messages)
        {
            var role = string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "Assistant" : "User";
            var content = TrimPii(message.Content);
            if (content.Length > 280)
            {
                content = content[..277] + "...";
            }

            sb.Append(role).Append(": ").Append(content).Append(" | ");
        }

        return sb.ToString().Trim(' ', '|');
    }

    public async Task SaveTurnAsync(
        Guid hospitalId,
        Guid? userId,
        Guid? profileId,
        string userPrompt,
        string assistantReply,
        CancellationToken cancellationToken = default)
    {
        if (hospitalId == Guid.Empty || string.IsNullOrWhiteSpace(userPrompt))
        {
            return;
        }

        try
        {
            var conversation = await FindConversationAsync(hospitalId, userId, cancellationToken);
            var now = DateTime.UtcNow;
            if (conversation == null)
            {
                conversation = new AIConversationContext
                {
                    Id = Guid.NewGuid(),
                    ProfileId = profileId,
                    UserId = userId,
                    ModuleCode = "assistant",
                    OrganizationId = hospitalId,
                    RetentionDays = 90,
                    CreatedAt = now,
                    LastActivityAt = now
                };
                _db.AIConversationContexts.Add(conversation);
            }
            else
            {
                conversation.LastActivityAt = now;
                if (profileId.HasValue && conversation.ProfileId != profileId)
                {
                    conversation.ProfileId = profileId;
                }
            }

            var nextSequence = await _db.AIConversationMessages
                .Where(m => m.ConversationId == conversation.Id)
                .Select(m => (int?)m.Sequence)
                .MaxAsync(cancellationToken) ?? 0;

            _db.AIConversationMessages.Add(new AIConversationMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Sequence = nextSequence + 1,
                Role = "user",
                Content = TrimPii(userPrompt),
                CreatedAt = now
            });

            if (!string.IsNullOrWhiteSpace(assistantReply))
            {
                _db.AIConversationMessages.Add(new AIConversationMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    Sequence = nextSequence + 2,
                    Role = "assistant",
                    Content = TrimPii(assistantReply),
                    CreatedAt = now
                });
            }

            // Keep memory bounded: drop older than last 40 messages.
            var staleIds = await _db.AIConversationMessages
                .Where(m => m.ConversationId == conversation.Id)
                .OrderByDescending(m => m.Sequence)
                .Skip(40)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);
            if (staleIds.Count > 0)
            {
                await _db.AIConversationMessages
                    .Where(m => staleIds.Contains(m.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist AI conversation memory for hospital {HospitalId}", hospitalId);
        }
    }

    private async Task<AIConversationContext?> FindConversationAsync(
        Guid hospitalId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        var query = _db.AIConversationContexts
            .Where(c => c.OrganizationId == hospitalId && c.ModuleCode == "assistant");

        query = userId.HasValue
            ? query.Where(c => c.UserId == userId)
            : query.Where(c => c.UserId == null);

        return await query
            .OrderByDescending(c => c.LastActivityAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal static string TrimPii(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var scrubbed = EmailRegex.Replace(text, "[email]");
        scrubbed = PhoneRegex.Replace(scrubbed, "[phone]");
        return scrubbed.Trim();
    }
}
