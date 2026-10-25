namespace CommunicationService.Domain.Enums;

public enum CommunicationChannelType { Email, Sms, WhatsApp, Push, Teams, Slack, Webhook, InApp }

public enum DeliveryStatus { Queued, Sent, Delivered, Read, Acknowledged, Failed, Retried, Expired, Deferred }

public enum NotificationFrequency { Immediate, Digest, DailySummary, WeeklySummary, CriticalOnly, NoMarketing }

public enum ReminderRuleType { TimeBased, EventBased, DueDate, Recurring }

public enum AvailabilityStatus { Available, Away, Vacation, DoNotDisturb }

public enum OutboundMessageStatus { Pending, Processing, Completed, Failed, Deferred }

public enum TemplateStatus { Draft, Published, Archived }
