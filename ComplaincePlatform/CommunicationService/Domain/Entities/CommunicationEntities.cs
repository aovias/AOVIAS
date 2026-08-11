using CommunicationService.Domain.Enums;

namespace CommunicationService.Domain.Entities;

public class CommunicationProfile
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string Language { get; set; } = "en-IN";
    public string TimeZone { get; set; } = "UTC";
    public NotificationFrequency DigestMode { get; set; } = NotificationFrequency.Immediate;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class NotificationPreference
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string? ModuleCode { get; set; }
    public CommunicationChannelType Channel { get; set; }
    public NotificationFrequency Frequency { get; set; } = NotificationFrequency.Immediate;
    public bool Enabled { get; set; } = true;
    public bool EscalationOverride { get; set; } = true;
    public bool NoMarketing { get; set; }
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CommunicationTemplate
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CommunicationChannelType Channel { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public TemplateStatus Status { get; set; } = TemplateStatus.Draft;
    public int CurrentVersion { get; set; } = 1;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TemplateVersion
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public int Version { get; set; }
    public string Locale { get; set; } = "en-IN";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string VariablesSchemaJson { get; set; } = "[]";
    public DateTime PublishedAt { get; set; }
}

public class ChannelConfiguration
{
    public Guid Id { get; set; }
    public CommunicationChannelType Channel { get; set; }
    public string ProviderType { get; set; } = string.Empty;
    public string SettingsJson { get; set; } = "{}";
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class OutboundMessage
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public bool Critical { get; set; }
    public int Priority { get; set; }
    public string? DedupeKey { get; set; }
    public OutboundMessageStatus Status { get; set; } = OutboundMessageStatus.Pending;
    public DateTime? ScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DeliveryRecord
{
    public Guid Id { get; set; }
    public Guid OutboundMessageId { get; set; }
    public CommunicationChannelType Channel { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Queued;
    public string? ProviderResponse { get; set; }
    public int? LatencyMs { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class InAppNotification
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid? OutboundMessageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReminderRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ReminderRuleType RuleType { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public string? TemplateCode { get; set; }
    public string? CronExpression { get; set; }
    public string? EventType { get; set; }
    public bool Enabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EscalationRule
{
    public Guid Id { get; set; }
    public string SourceEventType { get; set; } = string.Empty;
    public int? EscalationLevel { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public CommunicationChannelType? ChannelOverride { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class CommunicationCampaign
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public string AudienceFilterJson { get; set; } = "{}";
    public string Status { get; set; } = "Draft";
    public DateTime? LaunchedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AvailabilityProfile
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public AvailabilityStatus Status { get; set; } = AvailabilityStatus.Available;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WorkingHours
{
    public Guid Id { get; set; }
    public Guid AvailabilityProfileId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}

public class VacationEntry
{
    public Guid Id { get; set; }
    public Guid AvailabilityProfileId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

public class CommunicationTimelineEntry
{
    public Guid Id { get; set; }
    public Guid? PersonId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class EventSubscriptionRule
{
    public Guid Id { get; set; }
    public string SourceEventType { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string ChannelsJson { get; set; } = "[\"InApp\",\"Email\"]";
    public bool Enabled { get; set; } = true;
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
