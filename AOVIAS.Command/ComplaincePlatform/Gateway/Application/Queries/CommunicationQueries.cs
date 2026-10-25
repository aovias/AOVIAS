using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public sealed record ListNotificationsQuery(string ApiVersion, int Page, int PageSize) : IRequest<IActionResult>;
public sealed record GetUnreadCountQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListPreferencesQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListCommunicationTemplatesQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetCommunicationHistoryQuery(string ApiVersion, Guid? PersonId, int Take) : IRequest<IActionResult>;
public sealed record GetDeliveryStatusQuery(string ApiVersion, Guid DeliveryId) : IRequest<IActionResult>;
public sealed record GetCommunicationDashboardQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListReminderRulesQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListCampaignsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record GetAvailabilityQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListChannelsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListProvidersQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListSubscriptionsQuery(string ApiVersion) : IRequest<IActionResult>;
public sealed record ListDeliveryHistoryQuery(string ApiVersion, int Take) : IRequest<IActionResult>;
public sealed record GetQueueStatusQuery(string ApiVersion) : IRequest<IActionResult>;
