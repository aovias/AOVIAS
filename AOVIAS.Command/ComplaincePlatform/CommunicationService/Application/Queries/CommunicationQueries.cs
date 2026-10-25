using CommunicationService.Application.Contracts;
using MediatR;

namespace CommunicationService.Application.Queries;

public sealed record ListNotificationsQuery(Guid PersonId, int Page, int PageSize) : IRequest<NotificationListResponse>;
public sealed record GetUnreadCountQuery(Guid PersonId) : IRequest<int>;
public sealed record ListPreferencesQuery(Guid PersonId) : IRequest<IReadOnlyList<PreferenceDto>>;
public sealed record ListTemplatesQuery : IRequest<IReadOnlyList<TemplateDto>>;
public sealed record GetTimelineQuery(Guid? PersonId, int Take) : IRequest<IReadOnlyList<TimelineDto>>;
public sealed record GetDeliveryStatusQuery(Guid DeliveryId) : IRequest<DeliveryStatusDto?>;
public sealed record GetCommunicationDashboardQuery : IRequest<DashboardDto>;
public sealed record ListReminderRulesQuery : IRequest<IReadOnlyList<ReminderRuleDto>>;
public sealed record ListCampaignsQuery : IRequest<IReadOnlyList<CampaignDto>>;
public sealed record GetAvailabilityQuery(Guid PersonId) : IRequest<AvailabilityDto?>;
public sealed record ListChannelsQuery : IRequest<IReadOnlyList<ChannelDto>>;
public sealed record ListProvidersQuery : IRequest<IReadOnlyList<ProviderDto>>;
public sealed record ListSubscriptionsQuery : IRequest<IReadOnlyList<SubscriptionDto>>;
public sealed record ListDeliveryHistoryQuery(int Take) : IRequest<IReadOnlyList<DeliveryHistoryDto>>;
public sealed record GetQueueStatusQuery : IRequest<QueueStatusDto>;
