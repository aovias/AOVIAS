using CommunicationService.Domain.Enums;

namespace CommunicationService.Application.Contracts;

public sealed class DeliveryContext
{
    public required Guid OutboundMessageId { get; init; }
    public required Guid PersonId { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public string ModuleCode { get; init; } = "Communication";
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public sealed class DeliveryAttemptResult
{
    public bool Success { get; init; }
    public string? ProviderResponse { get; init; }
    public int LatencyMs { get; init; }
}

public interface ICommunicationChannelProvider
{
    CommunicationChannelType Channel { get; }
    Task<DeliveryAttemptResult> SendAsync(DeliveryContext context, CancellationToken ct = default);
}

public sealed class SendCommunicationRequest
{
    public required string ModuleCode { get; init; }
    public required string EventType { get; init; }
    public Guid? PersonId { get; init; }
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
    public string? TemplateCode { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public bool Critical { get; init; }
    public string? DedupeKey { get; init; }
    public IReadOnlyList<string>? PreferredChannels { get; init; }
}

public sealed class SendCommunicationResult
{
    public Guid OutboundMessageId { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class NotificationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string ModuleCode { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class NotificationListResponse
{
    public IReadOnlyList<NotificationDto> Items { get; init; } = Array.Empty<NotificationDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class PreferenceDto
{
    public Guid Id { get; init; }
    public string? ModuleCode { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public bool EscalationOverride { get; init; }
    public bool NoMarketing { get; init; }
    public string? QuietHoursStart { get; init; }
    public string? QuietHoursEnd { get; init; }
}

public sealed class UpdatePreferenceRequest
{
    public string? ModuleCode { get; init; }
    public string Channel { get; init; } = "InApp";
    public string Frequency { get; init; } = "Immediate";
    public bool Enabled { get; init; } = true;
    public bool EscalationOverride { get; init; } = true;
    public bool NoMarketing { get; init; }
    public string? QuietHoursStart { get; init; }
    public string? QuietHoursEnd { get; init; }
}

public sealed class TemplateDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string ModuleCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int CurrentVersion { get; init; }
}

public sealed class UpsertTemplateRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Channel { get; init; }
    public required string ModuleCode { get; init; }
    public required string Subject { get; init; }
    public required string Body { get; init; }
    public string Locale { get; init; } = "en-IN";
    public bool Publish { get; init; }
}

public sealed class TimelineDto
{
    public Guid Id { get; init; }
    public string Source { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
}

public sealed class DeliveryStatusDto
{
    public Guid Id { get; init; }
    public Guid OutboundMessageId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public int? LatencyMs { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class DashboardDto
{
    public int PendingMessages { get; init; }
    public int DeliveredToday { get; init; }
    public int FailedToday { get; init; }
    public int UnreadInApp { get; init; }
}

public sealed class ReminderRuleDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RuleType { get; init; } = string.Empty;
    public string ModuleCode { get; init; } = string.Empty;
    public string? TemplateCode { get; init; }
    public bool Enabled { get; init; }
}

public sealed class CreateReminderRuleRequest
{
    public required string Name { get; init; }
    public required string RuleType { get; init; }
    public required string ModuleCode { get; init; }
    public string? TemplateCode { get; init; }
    public string? CronExpression { get; init; }
    public string? EventType { get; init; }
}

public sealed class CampaignDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? LaunchedAt { get; init; }
}

public sealed class CreateCampaignRequest
{
    public required string Name { get; init; }
    public required Guid TemplateId { get; init; }
    public string AudienceFilterJson { get; init; } = "{}";
}

public sealed class AvailabilityDto
{
    public Guid PersonId { get; init; }
    public string TimeZone { get; init; } = "UTC";
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<WorkingHoursDto> WorkingHours { get; init; } = Array.Empty<WorkingHoursDto>();
    public IReadOnlyList<VacationDto> Vacations { get; init; } = Array.Empty<VacationDto>();
}

public sealed class WorkingHoursDto
{
    public DayOfWeek DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
}

public sealed class VacationDto
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string? Reason { get; init; }
}

public sealed class UpdateAvailabilityRequest
{
    public string TimeZone { get; init; } = "UTC";
    public string Status { get; init; } = "Available";
    public List<WorkingHoursDto>? WorkingHours { get; init; }
    public List<VacationDto>? Vacations { get; init; }
}

public sealed class ChannelDto
{
    public string Channel { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public bool Implemented { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class ProviderDto
{
    public Guid Id { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

public sealed class SubscriptionDto
{
    public Guid Id { get; init; }
    public string SourceEventType { get; init; } = string.Empty;
    public string ModuleCode { get; init; } = string.Empty;
    public string TemplateCode { get; init; } = string.Empty;
    public string Channels { get; init; } = string.Empty;
    public bool Enabled { get; init; }
}

public sealed class DeliveryHistoryDto
{
    public Guid Id { get; init; }
    public Guid OutboundMessageId { get; init; }
    public Guid? RecipientPersonId { get; init; }
    public string Channel { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public DateTime? SentAt { get; init; }
    public string? ProviderType { get; init; }
    public string? FailureReason { get; init; }
    public string? CorrelationId { get; init; }
    public string? EventType { get; init; }
}

public sealed class QueueStatusDto
{
    public int Pending { get; init; }
    public int Processing { get; init; }
    public int Failed { get; init; }
    public int Retrying { get; init; }
    public int Completed { get; init; }
}

public interface ICommunicationRequestService
{
    Task<SendCommunicationResult> SendAsync(SendCommunicationRequest request, CancellationToken ct = default);
    Task ProcessPendingAsync(CancellationToken ct = default);
    Task ProcessDigestAsync(CancellationToken ct = default);
}

public sealed class EnqueueComplianceEventRequest
{
    public required string EventType { get; init; }
    public string ModuleCode { get; init; } = "Compliance";
    public Guid PersonId { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public bool Critical { get; init; }
}

public interface INotificationInboxService
{
    Task<NotificationListResponse> ListAsync(Guid personId, int page, int pageSize, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Guid personId, CancellationToken ct = default);
    Task MarkReadAsync(Guid personId, Guid notificationId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid personId, CancellationToken ct = default);
}

public interface IPreferenceService
{
    Task<IReadOnlyList<PreferenceDto>> GetAsync(Guid personId, CancellationToken ct = default);
    Task<PreferenceDto> UpsertAsync(Guid personId, UpdatePreferenceRequest request, CancellationToken ct = default);
}

public interface ITemplateService
{
    Task<IReadOnlyList<TemplateDto>> ListAsync(CancellationToken ct = default);
    Task<TemplateDto> UpsertAsync(UpsertTemplateRequest request, CancellationToken ct = default);
}

public interface ICommunicationTimelineService
{
    Task<IReadOnlyList<TimelineDto>> GetAsync(Guid? personId, int take, CancellationToken ct = default);
}

public interface IDeliveryQueryService
{
    Task<DeliveryStatusDto?> GetAsync(Guid deliveryId, CancellationToken ct = default);
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct = default);
}

public interface IReminderRuleService
{
    Task<IReadOnlyList<ReminderRuleDto>> ListAsync(CancellationToken ct = default);
    Task<ReminderRuleDto> CreateAsync(CreateReminderRuleRequest request, CancellationToken ct = default);
}

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignDto>> ListAsync(CancellationToken ct = default);
    Task<CampaignDto> CreateAsync(CreateCampaignRequest request, CancellationToken ct = default);
    Task LaunchAsync(Guid campaignId, CancellationToken ct = default);
}

public interface IAvailabilityService
{
    Task<AvailabilityDto?> GetAsync(Guid personId, CancellationToken ct = default);
    Task<AvailabilityDto> UpsertAsync(Guid personId, UpdateAvailabilityRequest request, CancellationToken ct = default);
}

public interface IEventSubscriptionProcessor
{
    Task ProcessEventAsync(string sourceEventType, string moduleCode, Guid personId, Dictionary<string, string>? variables, bool critical, CancellationToken ct = default);
}

public interface INotificationAdminService
{
    Task<IReadOnlyList<ChannelDto>> ListChannelsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProviderDto>> ListProvidersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionDto>> ListSubscriptionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryHistoryDto>> ListDeliveryHistoryAsync(int take, CancellationToken ct = default);
    Task<QueueStatusDto> GetQueueStatusAsync(CancellationToken ct = default);
}
