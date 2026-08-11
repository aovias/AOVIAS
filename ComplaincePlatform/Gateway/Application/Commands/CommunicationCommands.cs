using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Commands;

public sealed record MarkNotificationReadCommand(string ApiVersion, Guid NotificationId) : IRequest<IActionResult>;
public sealed record MarkAllNotificationsReadCommand(string ApiVersion) : IRequest<IActionResult>;
public sealed record SendNotificationCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public sealed record UpdatePreferencesCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public sealed record UpsertCommunicationTemplateCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public sealed record CreateReminderRuleCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public sealed record CreateCampaignCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
public sealed record LaunchCampaignCommand(string ApiVersion, Guid CampaignId) : IRequest<IActionResult>;
public sealed record UpdateAvailabilityCommand(string ApiVersion, object RequestBody) : IRequest<IActionResult>;
