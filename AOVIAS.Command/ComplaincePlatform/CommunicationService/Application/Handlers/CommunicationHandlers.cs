using CommunicationService.Application.Commands;
using CommunicationService.Application.Contracts;
using CommunicationService.Application.Queries;
using MediatR;

namespace CommunicationService.Application.Handlers;

public sealed class SendCommunicationHandler(ICommunicationRequestService svc) : IRequestHandler<SendCommunicationCommand, SendCommunicationResult>
{
    public Task<SendCommunicationResult> Handle(SendCommunicationCommand request, CancellationToken ct) => svc.SendAsync(request.Request, ct);
}

public sealed class ListNotificationsHandler(INotificationInboxService svc) : IRequestHandler<ListNotificationsQuery, NotificationListResponse>
{
    public Task<NotificationListResponse> Handle(ListNotificationsQuery request, CancellationToken ct) => svc.ListAsync(request.PersonId, request.Page, request.PageSize, ct);
}

public sealed class GetUnreadCountHandler(INotificationInboxService svc) : IRequestHandler<GetUnreadCountQuery, int>
{
    public Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct) => svc.UnreadCountAsync(request.PersonId, ct);
}

public sealed class MarkNotificationReadHandler(INotificationInboxService svc) : IRequestHandler<MarkNotificationReadCommand>
{
    public Task Handle(MarkNotificationReadCommand request, CancellationToken ct) => svc.MarkReadAsync(request.PersonId, request.NotificationId, ct);
}

public sealed class MarkAllNotificationsReadHandler(INotificationInboxService svc) : IRequestHandler<MarkAllNotificationsReadCommand>
{
    public Task Handle(MarkAllNotificationsReadCommand request, CancellationToken ct) => svc.MarkAllReadAsync(request.PersonId, ct);
}

public sealed class ListPreferencesHandler(IPreferenceService svc) : IRequestHandler<ListPreferencesQuery, IReadOnlyList<PreferenceDto>>
{
    public Task<IReadOnlyList<PreferenceDto>> Handle(ListPreferencesQuery request, CancellationToken ct) => svc.GetAsync(request.PersonId, ct);
}

public sealed class UpdatePreferenceHandler(IPreferenceService svc) : IRequestHandler<UpdatePreferenceCommand, PreferenceDto>
{
    public Task<PreferenceDto> Handle(UpdatePreferenceCommand request, CancellationToken ct) => svc.UpsertAsync(request.PersonId, request.Request, ct);
}

public sealed class ListTemplatesHandler(ITemplateService svc) : IRequestHandler<ListTemplatesQuery, IReadOnlyList<TemplateDto>>
{
    public Task<IReadOnlyList<TemplateDto>> Handle(ListTemplatesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}

public sealed class UpsertTemplateHandler(ITemplateService svc) : IRequestHandler<UpsertTemplateCommand, TemplateDto>
{
    public Task<TemplateDto> Handle(UpsertTemplateCommand request, CancellationToken ct) => svc.UpsertAsync(request.Request, ct);
}

public sealed class GetTimelineHandler(ICommunicationTimelineService svc) : IRequestHandler<GetTimelineQuery, IReadOnlyList<TimelineDto>>
{
    public Task<IReadOnlyList<TimelineDto>> Handle(GetTimelineQuery request, CancellationToken ct) => svc.GetAsync(request.PersonId, request.Take, ct);
}

public sealed class GetDeliveryStatusHandler(IDeliveryQueryService svc) : IRequestHandler<GetDeliveryStatusQuery, DeliveryStatusDto?>
{
    public Task<DeliveryStatusDto?> Handle(GetDeliveryStatusQuery request, CancellationToken ct) => svc.GetAsync(request.DeliveryId, ct);
}

public sealed class GetCommunicationDashboardHandler(IDeliveryQueryService svc) : IRequestHandler<GetCommunicationDashboardQuery, DashboardDto>
{
    public Task<DashboardDto> Handle(GetCommunicationDashboardQuery request, CancellationToken ct) => svc.GetDashboardAsync(ct);
}

public sealed class ListReminderRulesHandler(IReminderRuleService svc) : IRequestHandler<ListReminderRulesQuery, IReadOnlyList<ReminderRuleDto>>
{
    public Task<IReadOnlyList<ReminderRuleDto>> Handle(ListReminderRulesQuery request, CancellationToken ct) => svc.ListAsync(ct);
}

public sealed class CreateReminderRuleHandler(IReminderRuleService svc) : IRequestHandler<CreateReminderRuleCommand, ReminderRuleDto>
{
    public Task<ReminderRuleDto> Handle(CreateReminderRuleCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, ct);
}

public sealed class ListCampaignsHandler(ICampaignService svc) : IRequestHandler<ListCampaignsQuery, IReadOnlyList<CampaignDto>>
{
    public Task<IReadOnlyList<CampaignDto>> Handle(ListCampaignsQuery request, CancellationToken ct) => svc.ListAsync(ct);
}

public sealed class CreateCampaignHandler(ICampaignService svc) : IRequestHandler<CreateCampaignCommand, CampaignDto>
{
    public Task<CampaignDto> Handle(CreateCampaignCommand request, CancellationToken ct) => svc.CreateAsync(request.Request, ct);
}

public sealed class LaunchCampaignHandler(ICampaignService svc) : IRequestHandler<LaunchCampaignCommand>
{
    public Task Handle(LaunchCampaignCommand request, CancellationToken ct) => svc.LaunchAsync(request.CampaignId, ct);
}

public sealed class GetAvailabilityHandler(IAvailabilityService svc) : IRequestHandler<GetAvailabilityQuery, AvailabilityDto?>
{
    public Task<AvailabilityDto?> Handle(GetAvailabilityQuery request, CancellationToken ct) => svc.GetAsync(request.PersonId, ct);
}

public sealed class UpdateAvailabilityHandler(IAvailabilityService svc) : IRequestHandler<UpdateAvailabilityCommand, AvailabilityDto>
{
    public Task<AvailabilityDto> Handle(UpdateAvailabilityCommand request, CancellationToken ct) => svc.UpsertAsync(request.PersonId, request.Request, ct);
}

public sealed class ListChannelsHandler(INotificationAdminService svc) : IRequestHandler<ListChannelsQuery, IReadOnlyList<ChannelDto>>
{
    public Task<IReadOnlyList<ChannelDto>> Handle(ListChannelsQuery request, CancellationToken ct) => svc.ListChannelsAsync(ct);
}

public sealed class ListProvidersHandler(INotificationAdminService svc) : IRequestHandler<ListProvidersQuery, IReadOnlyList<ProviderDto>>
{
    public Task<IReadOnlyList<ProviderDto>> Handle(ListProvidersQuery request, CancellationToken ct) => svc.ListProvidersAsync(ct);
}

public sealed class ListSubscriptionsHandler(INotificationAdminService svc) : IRequestHandler<ListSubscriptionsQuery, IReadOnlyList<SubscriptionDto>>
{
    public Task<IReadOnlyList<SubscriptionDto>> Handle(ListSubscriptionsQuery request, CancellationToken ct) => svc.ListSubscriptionsAsync(ct);
}

public sealed class ListDeliveryHistoryHandler(INotificationAdminService svc) : IRequestHandler<ListDeliveryHistoryQuery, IReadOnlyList<DeliveryHistoryDto>>
{
    public Task<IReadOnlyList<DeliveryHistoryDto>> Handle(ListDeliveryHistoryQuery request, CancellationToken ct) => svc.ListDeliveryHistoryAsync(request.Take, ct);
}

public sealed class GetQueueStatusHandler(INotificationAdminService svc) : IRequestHandler<GetQueueStatusQuery, QueueStatusDto>
{
    public Task<QueueStatusDto> Handle(GetQueueStatusQuery request, CancellationToken ct) => svc.GetQueueStatusAsync(ct);
}
