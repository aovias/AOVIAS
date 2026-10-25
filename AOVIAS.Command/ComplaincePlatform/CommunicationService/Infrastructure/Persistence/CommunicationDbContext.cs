using CommunicationService.Domain.Entities;
using CommunicationService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CommunicationService.Infrastructure.Persistence;

public class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : DbContext(options)
{
    public DbSet<CommunicationProfile> CommunicationProfiles => Set<CommunicationProfile>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<CommunicationTemplate> CommunicationTemplates => Set<CommunicationTemplate>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<ChannelConfiguration> ChannelConfigurations => Set<ChannelConfiguration>();
    public DbSet<OutboundMessage> OutboundMessages => Set<OutboundMessage>();
    public DbSet<DeliveryRecord> DeliveryRecords => Set<DeliveryRecord>();
    public DbSet<InAppNotification> InAppNotifications => Set<InAppNotification>();
    public DbSet<ReminderRule> ReminderRules => Set<ReminderRule>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
    public DbSet<CommunicationCampaign> CommunicationCampaigns => Set<CommunicationCampaign>();
    public DbSet<AvailabilityProfile> AvailabilityProfiles => Set<AvailabilityProfile>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<VacationEntry> VacationEntries => Set<VacationEntry>();
    public DbSet<CommunicationTimelineEntry> CommunicationTimeline => Set<CommunicationTimelineEntry>();
    public DbSet<EventSubscriptionRule> EventSubscriptionRules => Set<EventSubscriptionRule>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<CommunicationProfile>(e => { e.ToTable("CommunicationProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.PersonId).IsUnique(); });
        builder.Entity<NotificationPreference>(e => { e.ToTable("NotificationPreferences"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.PersonId, x.ModuleCode, x.Channel }); });
        builder.Entity<CommunicationTemplate>(e => { e.ToTable("CommunicationTemplates"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        builder.Entity<TemplateVersion>(e => { e.ToTable("TemplateVersions"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.TemplateId, x.Version, x.Locale }); });
        builder.Entity<ChannelConfiguration>(e => { e.ToTable("ChannelConfigurations"); e.HasKey(x => x.Id); e.HasIndex(x => x.Channel).IsUnique(); });
        builder.Entity<OutboundMessage>(e => { e.ToTable("OutboundMessages"); e.HasKey(x => x.Id); e.HasIndex(x => x.DedupeKey).IsUnique().HasFilter("[DedupeKey] IS NOT NULL"); e.HasIndex(x => new { x.Status, x.ScheduledFor }); });
        builder.Entity<DeliveryRecord>(e => { e.ToTable("DeliveryRecords"); e.HasKey(x => x.Id); e.HasIndex(x => x.OutboundMessageId); });
        builder.Entity<InAppNotification>(e => { e.ToTable("InAppNotifications"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.PersonId, x.CreatedAt }); });
        builder.Entity<ReminderRule>(e => { e.ToTable("ReminderRules"); e.HasKey(x => x.Id); });
        builder.Entity<EscalationRule>(e => { e.ToTable("EscalationRules"); e.HasKey(x => x.Id); e.HasIndex(x => x.SourceEventType); });
        builder.Entity<CommunicationCampaign>(e => { e.ToTable("CommunicationCampaigns"); e.HasKey(x => x.Id); });
        builder.Entity<AvailabilityProfile>(e => { e.ToTable("AvailabilityProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.PersonId).IsUnique(); });
        builder.Entity<WorkingHours>(e => { e.ToTable("WorkingHours"); e.HasKey(x => x.Id); });
        builder.Entity<VacationEntry>(e => { e.ToTable("VacationEntries"); e.HasKey(x => x.Id); });
        builder.Entity<CommunicationTimelineEntry>(e => { e.ToTable("CommunicationTimeline"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.PersonId, x.OccurredAt }); });
        builder.Entity<EventSubscriptionRule>(e => { e.ToTable("EventSubscriptionRules"); e.HasKey(x => x.Id); e.HasIndex(x => x.SourceEventType); });
        builder.Entity<OutboxMessage>(e => { e.ToTable("OutboxMessages"); e.HasKey(x => x.Id); e.HasIndex(x => x.ProcessedOn); });
    }
}
