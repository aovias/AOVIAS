using CommunicationService.Application.Contracts;
using CommunicationService.Domain.Enums;
using CommunicationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunicationService.Application.Services;

public sealed class NotificationAdminService(CommunicationDbContext db) : INotificationAdminService
{
    private static readonly (CommunicationChannelType Channel, bool Implemented, string Description)[] ChannelCatalog =
    [
        (CommunicationChannelType.InApp, true, "In-application notification center"),
        (CommunicationChannelType.Email, true, "SMTP and transactional email"),
        (CommunicationChannelType.Sms, false, "SMS via Twilio or Azure Communication Services"),
        (CommunicationChannelType.Push, false, "Mobile push via Firebase"),
        (CommunicationChannelType.Teams, false, "Microsoft Teams webhook delivery"),
        (CommunicationChannelType.Slack, false, "Slack workspace delivery"),
        (CommunicationChannelType.WhatsApp, false, "WhatsApp Business API (future)"),
        (CommunicationChannelType.Webhook, false, "Custom HTTP webhook callbacks (future)"),
    ];

    public async Task<IReadOnlyList<ChannelDto>> ListChannelsAsync(CancellationToken ct = default)
    {
        var configs = await db.ChannelConfigurations.AsNoTracking().ToListAsync(ct);
        return ChannelCatalog.Select(c =>
        {
            var config = configs.FirstOrDefault(x => x.Channel == c.Channel);
            return new ChannelDto
            {
                Channel = c.Channel.ToString(),
                Enabled = config?.Enabled ?? c.Implemented,
                Implemented = c.Implemented,
                Description = c.Description,
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<ProviderDto>> ListProvidersAsync(CancellationToken ct = default)
        => await db.ChannelConfigurations.AsNoTracking()
            .OrderBy(c => c.Channel)
            .Select(c => new ProviderDto
            {
                Id = c.Id,
                Channel = c.Channel.ToString(),
                ProviderType = c.ProviderType,
                Enabled = c.Enabled,
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubscriptionDto>> ListSubscriptionsAsync(CancellationToken ct = default)
        => await db.EventSubscriptionRules.AsNoTracking()
            .OrderBy(s => s.ModuleCode).ThenBy(s => s.SourceEventType)
            .Select(s => new SubscriptionDto
            {
                Id = s.Id,
                SourceEventType = s.SourceEventType,
                ModuleCode = s.ModuleCode,
                TemplateCode = s.TemplateCode,
                Channels = s.ChannelsJson,
                Enabled = s.Enabled,
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DeliveryHistoryDto>> ListDeliveryHistoryAsync(int take, CancellationToken ct = default)
        => await (
            from d in db.DeliveryRecords.AsNoTracking()
            join m in db.OutboundMessages.AsNoTracking() on d.OutboundMessageId equals m.Id
            orderby d.CreatedAt descending
            select new DeliveryHistoryDto
            {
                Id = d.Id,
                OutboundMessageId = d.OutboundMessageId,
                RecipientPersonId = m.PersonId == Guid.Empty ? null : m.PersonId,
                Channel = d.Channel.ToString(),
                Status = d.Status.ToString(),
                RetryCount = d.RetryCount,
                SentAt = d.CompletedAt ?? d.CreatedAt,
                ProviderType = d.Channel.ToString(),
                FailureReason = d.Status == DeliveryStatus.Failed ? d.ProviderResponse : null,
                CorrelationId = m.DedupeKey,
                EventType = m.EventType,
            })
            .Take(take)
            .ToListAsync(ct);

    public async Task<QueueStatusDto> GetQueueStatusAsync(CancellationToken ct = default)
    {
        var pending = await db.OutboundMessages.CountAsync(m => m.Status == OutboundMessageStatus.Pending || m.Status == OutboundMessageStatus.Deferred, ct);
        var processing = await db.OutboundMessages.CountAsync(m => m.Status == OutboundMessageStatus.Processing, ct);
        var failed = await db.OutboundMessages.CountAsync(m => m.Status == OutboundMessageStatus.Failed, ct)
            + await db.DeliveryRecords.CountAsync(d => d.Status == DeliveryStatus.Failed, ct);
        var retrying = await db.DeliveryRecords.CountAsync(d => d.Status == DeliveryStatus.Retried, ct);
        var completed = await db.OutboundMessages.CountAsync(m => m.Status == OutboundMessageStatus.Completed, ct);
        return new QueueStatusDto
        {
            Pending = pending,
            Processing = processing,
            Failed = failed,
            Retrying = retrying,
            Completed = completed,
        };
    }
}
