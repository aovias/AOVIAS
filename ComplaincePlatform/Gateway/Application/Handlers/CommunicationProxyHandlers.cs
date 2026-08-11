using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Handlers;

public class CommunicationProxyHandlers
{
    public class ListNotificationsHandler(IProxyService proxy) : IRequestHandler<ListNotificationsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListNotificationsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/notifications?page={request.Page}&pageSize={request.PageSize}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class GetUnreadCountHandler(IProxyService proxy) : IRequestHandler<GetUnreadCountQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/notifications/unread-count", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class MarkNotificationReadHandler(IProxyService proxy) : IRequestHandler<MarkNotificationReadCommand, IActionResult>
    {
        public Task<IActionResult> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/notifications/{request.NotificationId}/read", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class MarkAllNotificationsReadHandler(IProxyService proxy) : IRequestHandler<MarkAllNotificationsReadCommand, IActionResult>
    {
        public Task<IActionResult> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/notifications/read-all", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class SendNotificationHandler(IProxyService proxy) : IRequestHandler<SendNotificationCommand, IActionResult>
    {
        public Task<IActionResult> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/notifications/send", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class ListPreferencesHandler(IProxyService proxy) : IRequestHandler<ListPreferencesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListPreferencesQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/preferences", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class UpdatePreferencesHandler(IProxyService proxy) : IRequestHandler<UpdatePreferencesCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdatePreferencesCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/preferences", HttpMethod.Put, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class ListTemplatesHandler(IProxyService proxy) : IRequestHandler<ListCommunicationTemplatesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListCommunicationTemplatesQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/templates", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class UpsertTemplateHandler(IProxyService proxy) : IRequestHandler<UpsertCommunicationTemplateCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpsertCommunicationTemplateCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/templates", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class GetCommunicationHistoryHandler(IProxyService proxy) : IRequestHandler<GetCommunicationHistoryQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetCommunicationHistoryQuery request, CancellationToken cancellationToken)
        {
            var qs = $"?take={request.Take}";
            if (request.PersonId.HasValue) qs += $"&personId={request.PersonId}";
            return proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/communication/history{qs}", HttpMethod.Get, cancellationToken: cancellationToken);
        }
    }

    public class GetDeliveryStatusHandler(IProxyService proxy) : IRequestHandler<GetDeliveryStatusQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetDeliveryStatusQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/delivery-status/{request.DeliveryId}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class GetCommunicationDashboardHandler(IProxyService proxy) : IRequestHandler<GetCommunicationDashboardQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetCommunicationDashboardQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/communication/dashboard", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class ListChannelsHandler(IProxyService proxy) : IRequestHandler<ListChannelsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListChannelsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/communication/channels", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class ListProvidersHandler(IProxyService proxy) : IRequestHandler<ListProvidersQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListProvidersQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/communication/providers", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class ListSubscriptionsHandler(IProxyService proxy) : IRequestHandler<ListSubscriptionsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListSubscriptionsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/communication/subscriptions", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class ListDeliveryHistoryHandler(IProxyService proxy) : IRequestHandler<ListDeliveryHistoryQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListDeliveryHistoryQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/communication/delivery-history?take={request.Take}", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class GetQueueStatusHandler(IProxyService proxy) : IRequestHandler<GetQueueStatusQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetQueueStatusQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/communication/queues", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class ListRemindersHandler(IProxyService proxy) : IRequestHandler<ListReminderRulesQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListReminderRulesQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/reminders", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class CreateReminderHandler(IProxyService proxy) : IRequestHandler<CreateReminderRuleCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateReminderRuleCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/reminders", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class ListCampaignsHandler(IProxyService proxy) : IRequestHandler<ListCampaignsQuery, IActionResult>
    {
        public Task<IActionResult> Handle(ListCampaignsQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/campaigns", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class CreateCampaignHandler(IProxyService proxy) : IRequestHandler<CreateCampaignCommand, IActionResult>
    {
        public Task<IActionResult> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/campaigns", HttpMethod.Post, request.RequestBody, cancellationToken: cancellationToken);
    }

    public class LaunchCampaignHandler(IProxyService proxy) : IRequestHandler<LaunchCampaignCommand, IActionResult>
    {
        public Task<IActionResult> Handle(LaunchCampaignCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/campaigns/{request.CampaignId}/launch", HttpMethod.Post, cancellationToken: cancellationToken);
    }

    public class GetAvailabilityHandler(IProxyService proxy) : IRequestHandler<GetAvailabilityQuery, IActionResult>
    {
        public Task<IActionResult> Handle(GetAvailabilityQuery request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/availability", HttpMethod.Get, cancellationToken: cancellationToken);
    }

    public class UpdateAvailabilityHandler(IProxyService proxy) : IRequestHandler<UpdateAvailabilityCommand, IActionResult>
    {
        public Task<IActionResult> Handle(UpdateAvailabilityCommand request, CancellationToken cancellationToken)
            => proxy.ProxyToCommunicationServiceAsync($"/api/v{request.ApiVersion}/availability", HttpMethod.Put, request.RequestBody, cancellationToken: cancellationToken);
    }
}
